using UnityEngine;

public enum TipoInimigo { Slime, Esqueleto, Chefe }

// Inimigos que andam no chão:
//   Slime     - patrulha e pula; machuca ao encostar
//   Esqueleto - patrulha, persegue o herói e ataca com a espada (levanta a espada antes de bater)
//   Chefe     - esqueleto gigante com muita vida
public class Inimigo : MonoBehaviour, ILevaDano
{
    public static Inimigo Chefe;        // o HUD mostra a barra de vida dele

    public TipoInimigo tipo = TipoInimigo.Slime;
    public int vida = 2;
    public int vidaMax = 2;
    public float velocidade = 1.3f;
    public float raioVisao = 7f;

    private enum Est { Patrulha, Perseguir, Preparar, Golpear, Recuperar, Morrendo }
    private Est est = Est.Patrulha;

    private Rigidbody2D rb;
    private BoxCollider2D col;
    private SpriteRenderer sr;

    private int dir = 1;
    private float tEst, stun, flash, animT;
    private float escala = 1f;
    private float tempoPreparo = 0.45f;
    private int danoGolpe = 1;
    private Color tinta = Color.white;
    private bool configurado;

    public bool Vivo { get { return est != Est.Morrendo; } }

    public static Inimigo Criar(TipoInimigo t, Vector2 pes)
    {
        PixelArt.Iniciar();
        GameObject go = new GameObject(t.ToString());
        go.transform.position = pes;
        Inimigo i = go.AddComponent<Inimigo>();
        i.tipo = t;
        i.Configurar();
        return i;
    }

    void Start()
    {
        if (!configurado) Configurar();
    }

    void Configurar()
    {
        configurado = true;
        PixelArt.Iniciar();

        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = GameManager.camadaNome;
        sr.sortingOrder = GameManager.ordemBase + 5;

        rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 3f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        col = GetComponent<BoxCollider2D>();
        if (col == null) col = gameObject.AddComponent<BoxCollider2D>();
        PhysicsMaterial2D mat = new PhysicsMaterial2D("InimigoSemAtrito");
        mat.friction = 0f;
        col.sharedMaterial = mat;

        switch (tipo)
        {
            case TipoInimigo.Slime:
                vida = vidaMax = 2; velocidade = 1.3f;
                col.size = new Vector2(0.85f, 0.6f); col.offset = new Vector2(0f, 0.3f);
                sr.sprite = PixelArt.slime[0];
                break;
            case TipoInimigo.Esqueleto:
                vida = vidaMax = 3; velocidade = 1.5f; raioVisao = 7f;
                col.size = new Vector2(0.6f, 1.4f); col.offset = new Vector2(0f, 0.7f);
                sr.sprite = PixelArt.esqueleto[0];
                break;
            case TipoInimigo.Chefe:
                vida = vidaMax = 12; velocidade = 2.1f; raioVisao = 12f;
                escala = 1.7f; tempoPreparo = 0.4f; danoGolpe = 2;
                transform.localScale = Vector3.one * escala;
                tinta = new Color(1f, 0.82f, 0.42f);      // dourado de rei
                col.size = new Vector2(0.6f, 1.4f); col.offset = new Vector2(0f, 0.7f);
                sr.sprite = PixelArt.esqueleto[0];
                Chefe = this;
                break;
        }

        sr.color = tinta;
        dir = Random.value < 0.5f ? -1 : 1;
    }

    void OnDestroy()
    {
        if (Chefe == this) Chefe = null;
    }


    // ------------------------------------------------------------------ comportamento
    void Update()
    {
        if (Time.timeScale == 0f || !configurado) return;
        float dt = Time.deltaTime;
        flash -= dt; stun -= dt; tEst += dt; animT += dt;

        if (est == Est.Morrendo)
        {
            AnimarMorte();
            return;
        }

        if (stun <= 0f)
        {
            Player p = Player.I;

            switch (est)
            {
                case Est.Patrulha:
                    Andar(velocidade, true);
                    if (tipo != TipoInimigo.Slime && JogadorNaVisao(p)) Mudar(Est.Perseguir);
                    break;

                case Est.Perseguir:
                    if (!JogadorNaVisao(p)) { Mudar(Est.Patrulha); break; }
                    dir = p.transform.position.x > transform.position.x ? 1 : -1;
                    if (Mathf.Abs(p.transform.position.x - transform.position.x) < 1.5f * escala)
                    {
                        Parar();
                        Mudar(Est.Preparar);
                    }
                    else Andar(velocidade * 1.7f, false);
                    break;

                case Est.Preparar:
                    Parar();
                    if (tEst >= tempoPreparo) { Mudar(Est.Golpear); Golpear(); }
                    break;

                case Est.Golpear:
                    Parar();
                    if (tEst >= 0.14f) Mudar(Est.Recuperar);
                    break;

                case Est.Recuperar:
                    Parar();
                    if (tEst >= 0.6f) Mudar(Est.Patrulha);
                    break;
            }
        }

        Desenhar();
    }

    void Mudar(Est novo) { est = novo; tEst = 0f; }

    void Parar() { rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y); }

    bool JogadorNaVisao(Player p)
    {
        if (p == null || !p.Vivo) return false;
        Vector2 d = p.transform.position - transform.position;
        return Mathf.Abs(d.x) < raioVisao && Mathf.Abs(d.y) < 2.5f * escala;
    }

    // Anda; na patrulha vira na parede ou na beira do buraco, na perseguição só para.
    void Andar(float v, bool virar)
    {
        if (ParedeAdiante(dir) || !ChaoAdiante(dir))
        {
            if (virar) dir = -dir;
            else { Parar(); return; }
        }
        rb.linearVelocity = new Vector2(dir * v, rb.linearVelocity.y);
    }

    bool TemChao(Vector2 ponto)
    {
        Collider2D[] cs = Physics2D.OverlapPointAll(ponto);
        for (int i = 0; i < cs.Length; i++)
        {
            if (cs[i] != col && !cs[i].isTrigger && cs[i].CompareTag("Ground")) return true;
        }
        return false;
    }

    bool ParedeAdiante(int d)
    {
        float lado = col.size.x * escala * 0.5f + 0.12f;
        return TemChao(new Vector2(transform.position.x + d * lado, transform.position.y + 0.4f * escala));
    }

    bool ChaoAdiante(int d)
    {
        float lado = col.size.x * escala * 0.5f + 0.15f;
        return TemChao(new Vector2(transform.position.x + d * lado, transform.position.y - 0.15f));
    }

    // O golpe da espada do esqueleto: machuca o herói se ele estiver na frente
    void Golpear()
    {
        Vector2 centro = (Vector2)transform.position + new Vector2(dir * 0.9f * escala, 0.7f * escala);
        Collider2D[] cs = Physics2D.OverlapBoxAll(centro, new Vector2(1.5f * escala, 1.3f * escala), 0f);
        for (int i = 0; i < cs.Length; i++)
        {
            Player p = cs[i].GetComponent<Player>();
            if (p != null) p.LevarDano(danoGolpe, transform.position);
        }
        Som.Tocar("ataque");
    }

    // Encostar no herói também machuca
    void OnCollisionStay2D(Collision2D c)
    {
        if (!Vivo) return;
        Player p = c.collider.GetComponent<Player>();
        if (p != null) p.LevarDano(danoGolpe, transform.position);
    }


    // ------------------------------------------------------------------ dano e morte
    public void LevarDano(int dano, Vector2 origem)
    {
        if (!Vivo) return;

        vida -= dano;
        flash = 0.12f;
        stun = tipo == TipoInimigo.Chefe ? 0.05f : 0.2f;
        if (est == Est.Preparar && tipo != TipoInimigo.Chefe) Mudar(Est.Recuperar);

        float lado = Mathf.Sign(transform.position.x - origem.x);
        if (lado == 0f) lado = 1f;
        rb.linearVelocity = new Vector2(lado * (tipo == TipoInimigo.Chefe ? 2f : 5f), tipo == TipoInimigo.Chefe ? 1f : 4f);

        Efeitos.Faiscas(Centro(), CorDoSangue(), 6 + (int)escala * 2);

        if (vida <= 0) Morrer();
    }

    Vector2 Centro() { return (Vector2)transform.position + Vector2.up * 0.6f * escala; }

    Color CorDoSangue()
    {
        return tipo == TipoInimigo.Slime ? new Color(0.4f, 0.9f, 0.4f) : new Color(0.95f, 0.92f, 0.8f);
    }

    void Morrer()
    {
        est = Est.Morrendo;
        tEst = 0f;
        col.enabled = false;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;
        Som.Tocar("inimigo_morre");
        Efeitos.Faiscas(Centro(), CorDoSangue(), 14 + (int)(escala * 6), 6f, 0.6f);

        if (tipo == TipoInimigo.Chefe)
        {
            GameManager.Aviso("O chefe da masmorra caiu!");
            Coletavel.Criar(TipoColetavel.Coracao, (Vector2)transform.position + Vector2.up * 0.8f);
            if (CameraSegue.I != null) CameraSegue.I.Tremer(0.5f, 0.3f);
        }
        else if (Random.value < 0.25f)
        {
            Coletavel.Criar(TipoColetavel.Coracao, (Vector2)transform.position + Vector2.up * 0.6f);
        }
    }

    void AnimarMorte()
    {
        // pisca em branco, achata e some
        sr.sprite = PixelArt.Branco(sr.sprite);
        sr.color = new Color(1f, 1f, 1f, Mathf.Clamp01(1f - tEst / 0.35f));
        transform.localScale = new Vector3(escala * (1f + tEst), escala * Mathf.Max(0.1f, 1f - tEst * 2.5f), 1f);
        if (tEst >= 0.35f) Destroy(gameObject);
    }

    // ------------------------------------------------------------------ desenho
    void Desenhar()
    {
        Sprite s;

        if (tipo == TipoInimigo.Slime)
        {
            s = PixelArt.slime[(int)(animT / 0.16f) % PixelArt.slime.Length];
        }
        else
        {
            switch (est)
            {
                case Est.Preparar: s = PixelArt.esqueletoAtaque[0]; break;
                case Est.Golpear: s = PixelArt.esqueletoAtaque[1]; break;
                case Est.Recuperar: s = PixelArt.esqueletoAtaque[2]; break;
                default:
                    bool andando = Mathf.Abs(rb.linearVelocity.x) > 0.2f;
                    s = andando ? PixelArt.esqueleto[(int)(animT / 0.15f) % PixelArt.esqueleto.Length] : PixelArt.esqueleto[0];
                    break;
            }
        }

        sr.sprite = flash > 0f ? PixelArt.Branco(s) : s;
        sr.color = flash > 0f ? Color.white : tinta;
        sr.flipX = dir < 0;
    }
}
