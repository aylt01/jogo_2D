using System.Collections.Generic;
using UnityEngine;

// O herói: anda, pula, ataca com a espada, leva dano, usa o dash (depois de pegar o baú) e morre.
// Teclas: A/D ou setas = andar | Espaço/W = pular | J, Z ou mouse = atacar | X ou Shift = dash | Esc/P = pausa
public class Player : MonoBehaviour
{
    public static Player I;

    [Header("Movimento")]
    public float speed = 5f;
    public float jumpSpeed = 12.5f;
    public float gravidadeSubindo = 3.2f;
    public float gravidadeCaindo = 4.4f;
    public float quedaMaxima = 18f;

    [Header("Vida")]
    public int vidaMax = 5;
    public int vida;

    [Header("Dash (liberado ao pegar o baú)")]
    public bool temDash = false;
    public float dashSpeed = 18f;
    public float dashDuration = 0.15f;
    public float dashCooldown = 0.5f;

    private Rigidbody2D rb;
    private BoxCollider2D col;
    private SpriteRenderer sr;

    private int dir = 1;                  // 1 = olhando para a direita, -1 = esquerda
    private bool noChao, estavaNoChao;
    private float coyote, bufferPulo;
    private bool cortouPulo;

    private bool atacando, golpeFeito;
    private float tAtaque, atkCd;
    private HashSet<ILevaDano> acertados = new HashSet<ILevaDano>();

    private float dashT, dashCd;
    private float invul, stun, flash;
    private bool morto;
    private float tMorte;
    private bool avisouMorte;

    private float animT;
    private Vector3 ultimoSeguro;
    private float tSeguro;

    public bool Vivo { get { return !morto; } }
    public int Direcao { get { return dir; } }
    public float DashRecarga { get { return temDash ? Mathf.Clamp01(1f - dashCd / (dashCooldown + dashDuration)) : 0f; } }


    void Awake()
    {
        I = this;
        PixelArt.Iniciar();

        transform.localScale = Vector3.one;
        transform.rotation = Quaternion.identity;

        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = PixelArt.heroiParado[0];
        sr.color = Color.white;
        sr.flipX = false;

        rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.mass = 1f;
        rb.linearDamping = 0f;
        rb.gravityScale = gravidadeSubindo;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep;

        // O herói agora usa uma caixa (o triângulo antigo é removido)
        foreach (Collider2D antigo in GetComponents<Collider2D>())
        {
            Destroy(antigo);
        }

        col = gameObject.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.7f, 1.25f);
        col.offset = new Vector2(0f, 0.625f);
        PhysicsMaterial2D semAtrito = new PhysicsMaterial2D("SemAtrito");
        semAtrito.friction = 0f;
        semAtrito.bounciness = 0f;
        col.sharedMaterial = semAtrito;

        vida = vidaMax;
    }

    void Start()
    {
        ultimoSeguro = transform.position;
    }

    void OnDestroy()
    {
        if (I == this) I = null;
    }


    // ------------------------------------------------------------------ atualização
    void Update()
    {
        if (Time.timeScale == 0f) return;
        float dt = Time.deltaTime;

        if (morto)
        {
            AnimarMorte(dt);
            return;
        }

        invul -= dt; stun -= dt; flash -= dt; atkCd -= dt; dashCd -= dt;

        VerificarChao(dt);

        if (Entrada.Pular()) bufferPulo = 0.12f;
        bufferPulo -= dt;

        if (stun <= 0f) Controlar(dt);

        // gravidade mais forte na queda deixa o pulo mais "firme"
        Vector2 v = rb.linearVelocity;
        if (dashT <= 0f) rb.gravityScale = v.y < 0f ? gravidadeCaindo : gravidadeSubindo;
        if (v.y < -quedaMaxima) rb.linearVelocity = new Vector2(v.x, -quedaMaxima);

        Animar(dt);
        VerificarQueda();
    }

    void VerificarChao(float dt)
    {
        estavaNoChao = noChao;
        noChao = false;

        Bounds b = col.bounds;
        Collider2D[] tocando = Physics2D.OverlapBoxAll(new Vector2(b.center.x, b.min.y - 0.04f), new Vector2(b.size.x * 0.85f, 0.1f), 0f);
        for (int i = 0; i < tocando.Length; i++)
        {
            if (tocando[i] != col && !tocando[i].isTrigger && tocando[i].CompareTag("Ground"))
            {
                noChao = true;
                break;
            }
        }
        if (rb.linearVelocity.y > 0.5f) noChao = false;

        coyote = noChao ? 0.1f : coyote - dt;

        if (noChao && !estavaNoChao) Efeitos.Poeira(transform.position, 0);

        // guarda o último lugar seguro (para voltar se cair num buraco)
        if (noChao) tSeguro += dt; else tSeguro = 0f;
        if (tSeguro > 0.5f && !PertoDePerigo()) ultimoSeguro = transform.position;
    }

    bool PertoDePerigo()
    {
        Collider2D[] perto = Physics2D.OverlapCircleAll((Vector2)transform.position + Vector2.up * 0.6f, 1.2f);
        for (int i = 0; i < perto.Length; i++)
        {
            if (perto[i].CompareTag("Dano")) return true;
        }
        return false;
    }

    void Controlar(float dt)
    {
        float eixo = Entrada.Eixo();
        if (!atacando && Mathf.Abs(eixo) > 0.1f) dir = eixo > 0f ? 1 : -1;

        // dash
        if (temDash && Entrada.Dash() && dashCd <= 0f && dashT <= 0f)
        {
            dashT = dashDuration;
            dashCd = dashCooldown + dashDuration;
            invul = Mathf.Max(invul, dashDuration + 0.05f);
            atacando = false;
            Som.Tocar("dash");
            Efeitos.Poeira(transform.position, dir);
        }

        if (dashT > 0f)
        {
            dashT -= dt;
            rb.gravityScale = 0f;
            rb.linearVelocity = new Vector2(dir * dashSpeed, 0f);
            if (Random.value < 0.6f) Efeitos.Faiscas((Vector2)transform.position + Vector2.up * 0.7f, new Color(0.5f, 0.85f, 1f, 0.9f), 1, 0.5f, 0.25f, 0f);
            return;
        }

        // ataque
        if (Entrada.Atacar() && atkCd <= 0f && !atacando) IniciarAtaque();
        if (atacando) AtualizarAtaque(dt);

        // andar
        float alvo = eixo * speed * (atacando ? 0.45f : 1f);
        float aceleracao = noChao ? 80f : 55f;
        Vector2 vel = rb.linearVelocity;
        vel.x = Mathf.MoveTowards(vel.x, alvo, aceleracao * dt);

        // pular (com "tempo de graça" na beira e pulo guardado)
        if (bufferPulo > 0f && coyote > 0f)
        {
            vel.y = jumpSpeed;
            coyote = 0f;
            bufferPulo = 0f;
            cortouPulo = false;
            Som.Tocar("pulo");
            Efeitos.Poeira(transform.position, dir);
        }

        // soltou o botão no meio do pulo: pulo mais curto
        if (!Entrada.PularSegurando() && vel.y > 3f && !cortouPulo)
        {
            vel.y *= 0.45f;
            cortouPulo = true;
        }

        rb.linearVelocity = vel;
    }


    // ------------------------------------------------------------------ ataque
    void IniciarAtaque()
    {
        atacando = true;
        golpeFeito = false;
        tAtaque = 0f;
        atkCd = 0.34f;
        acertados.Clear();
        Som.Tocar("ataque");
    }

    void AtualizarAtaque(float dt)
    {
        tAtaque += dt;

        if (!golpeFeito && tAtaque >= 0.06f)
        {
            golpeFeito = true;
            CriarGolpeVisual();
            Acertar();
        }

        if (tAtaque >= 0.30f) atacando = false;
    }

    void CriarGolpeVisual()
    {
        GameObject g = new GameObject("Golpe");
        g.transform.SetParent(transform, false);
        g.transform.localPosition = new Vector3(0.1f * dir, 0.75f, 0f);
        g.transform.localScale = new Vector3(dir, 1f, 1f);

        SpriteRenderer gs = g.AddComponent<SpriteRenderer>();
        gs.sortingLayerName = sr.sortingLayerName;
        gs.sortingOrder = sr.sortingOrder + 1;

        AnimacaoUnica anim = g.AddComponent<AnimacaoUnica>();
        anim.quadros = PixelArt.golpe;
        anim.tempoPorQuadro = 0.06f;
    }

    void Acertar()
    {
        Vector2 centro = (Vector2)transform.position + new Vector2(dir * 1.0f, 0.75f);
        Collider2D[] atingidos = Physics2D.OverlapBoxAll(centro, new Vector2(1.9f, 1.5f), 0f);
        bool acertou = false;

        for (int i = 0; i < atingidos.Length; i++)
        {
            ILevaDano alvo = atingidos[i].GetComponentInParent<ILevaDano>();
            if (alvo != null && alvo.Vivo && acertados.Add(alvo))
            {
                alvo.LevarDano(1, transform.position);
                acertou = true;
            }
        }

        if (acertou)
        {
            Som.Tocar("acerto");
            if (CameraSegue.I != null) CameraSegue.I.Tremer(0.12f, 0.12f);
            if (GameManager.I != null) GameManager.I.HitStop(0.06f);
        }
    }


    // ------------------------------------------------------------------ dano, cura e morte
    public void LevarDano(int dano, Vector2 origem)
    {
        if (morto || invul > 0f || dashT > 0f) return;

        vida -= dano;
        Som.Tocar("dano");
        Efeitos.Faiscas((Vector2)transform.position + Vector2.up * 0.8f, new Color(0.9f, 0.2f, 0.25f), 8);
        if (CameraSegue.I != null) CameraSegue.I.Tremer(0.25f, 0.22f);

        if (vida <= 0)
        {
            vida = 0;
            Morrer();
            return;
        }

        invul = 1.2f;
        stun = 0.25f;
        flash = 0.12f;
        atacando = false;
        dashT = 0f;

        float lado = Mathf.Sign(transform.position.x - origem.x);
        if (lado == 0f) lado = -dir;
        rb.linearVelocity = new Vector2(lado * 6f, 7f);

        if (GameManager.I != null) GameManager.I.HitStop(0.08f);
    }

    public bool Curar(int quanto)
    {
        if (vida >= vidaMax || morto) return false;
        vida = Mathf.Min(vidaMax, vida + quanto);
        Efeitos.Faiscas((Vector2)transform.position + Vector2.up * 0.8f, new Color(1f, 0.5f, 0.6f), 8, 2.5f, 0.5f, -2f);
        return true;
    }

    public void LiberarDash()
    {
        temDash = true;
        GameManager.DashLiberado = true;
    }

    void Morrer()
    {
        morto = true;
        atacando = false;
        dashT = 0f;
        tMorte = 0f;
        Som.Tocar("morte");
        Efeitos.Faiscas((Vector2)transform.position + Vector2.up * 0.8f, new Color(0.9f, 0.2f, 0.25f), 16, 5f, 0.6f);
        rb.gravityScale = gravidadeSubindo;
        rb.linearVelocity = new Vector2(-dir * 2f, 6f);
        if (CameraSegue.I != null) CameraSegue.I.Tremer(0.4f, 0.3f);
    }

    void AnimarMorte(float dt)
    {
        tMorte += dt;
        int f = tMorte < 0.25f ? 0 : (tMorte < 0.6f ? 1 : 2);
        sr.sprite = PixelArt.heroiMorte[f];
        sr.flipX = dir < 0;
        sr.color = Color.white;
        rb.linearVelocity = new Vector2(Mathf.MoveTowards(rb.linearVelocity.x, 0f, 14f * dt), rb.linearVelocity.y);

        if (tMorte > 1.4f && !avisouMorte)
        {
            avisouMorte = true;
            if (GameManager.I != null) GameManager.I.JogadorMorreu();
        }
    }

    void VerificarQueda()
    {
        if (transform.position.y >= GameManager.limiteQueda) return;

        vida -= 2;
        Som.Tocar("dano");
        if (vida <= 0)
        {
            vida = 0;
            Morrer();
            return;
        }

        transform.position = ultimoSeguro;
        rb.linearVelocity = Vector2.zero;
        invul = 1.5f;
        if (CameraSegue.I != null) CameraSegue.I.IrParaOAlvo();
    }

    // Posição para onde voltar se cair (o GameManager e a masmorra chamam)
    public void DefinirInicio(Vector3 pos)
    {
        transform.position = pos;
        ultimoSeguro = pos;
        rb.linearVelocity = Vector2.zero;
    }

    void Perigo(Collider2D o)
    {
        if (!morto && o.CompareTag("Dano")) LevarDano(1, o.bounds.center);
    }

    void OnCollisionEnter2D(Collision2D c) { Perigo(c.collider); }
    void OnCollisionStay2D(Collision2D c) { Perigo(c.collider); }
    void OnTriggerEnter2D(Collider2D o) { Perigo(o); }
    void OnTriggerStay2D(Collider2D o) { Perigo(o); }


    // ------------------------------------------------------------------ animação
    void Animar(float dt)
    {
        Sprite alvo;
        Vector2 v = rb.linearVelocity;

        if (stun > 0f) alvo = PixelArt.heroiDano;
        else if (dashT > 0f) alvo = PixelArt.heroiDash;
        else if (atacando)
        {
            int f = tAtaque < 0.06f ? 0 : (tAtaque < 0.16f ? 1 : 2);
            alvo = PixelArt.heroiAtaque[f];
        }
        else if (!noChao) alvo = v.y > 0f ? PixelArt.heroiPulo : PixelArt.heroiQueda;
        else if (Mathf.Abs(v.x) > 0.6f)
        {
            animT += dt;
            alvo = PixelArt.heroiCorrer[(int)(animT / 0.075f) % PixelArt.heroiCorrer.Length];
        }
        else alvo = PixelArt.heroiParado[(int)(Time.time / 0.5f) % PixelArt.heroiParado.Length];

        sr.sprite = flash > 0f ? PixelArt.Branco(alvo) : alvo;
        sr.flipX = dir < 0;

        // pisca quando está invencível (depois de levar dano)
        if (dashT > 0f) sr.color = new Color(0.6f, 0.85f, 1f, 0.8f);
        else if (invul > 0f) sr.color = new Color(1f, 1f, 1f, ((int)(Time.time * 14f) % 2 == 0) ? 0.35f : 1f);
        else sr.color = Color.white;
    }
}
