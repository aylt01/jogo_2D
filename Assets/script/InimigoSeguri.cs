using UnityEngine;

// Morcego: fica pairando e, quando o herói chega perto (e não tem parede no meio), voa para cima dele.
// Depois de morder, recua um pouco. O ataque do herói derrota o morcego.
// (Este script já estava ligado ao quadrado vermelho da fase 1; agora ele vira um morcego.)
public class InimigoSeguri : MonoBehaviour, ILevaDano
{
    public float velocidade = 2f;   // velocidade base (o morcego ganha um bônus)
    public float raio = 9f;         // a que distância ele percebe o herói
    public int vida = 2;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private BoxCollider2D col;

    private bool morto;
    private float flash, stun, recuo, animT, fase;

    public bool Vivo { get { return !morto; } }

    public static InimigoSeguri Criar(Vector2 pos)
    {
        GameObject go = new GameObject("Morcego");
        go.transform.position = pos;
        InimigoSeguri m = go.AddComponent<InimigoSeguri>();
        m.sr.sortingLayerName = GameManager.camadaNome;
        m.sr.sortingOrder = GameManager.ordemBase + 6;
        return m;
    }

    void Awake()
    {
        PixelArt.Iniciar();
        fase = Random.value * 10f;

        transform.localScale = Vector3.one;
        transform.rotation = Quaternion.identity;
        gameObject.tag = "Untagged";            // antes era "Dano"; agora o dano vem deste script

        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = PixelArt.morcego[0];
        sr.color = Color.white;

        rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.linearDamping = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        foreach (Collider2D antigo in GetComponents<Collider2D>()) Destroy(antigo);
        col = gameObject.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.8f, 0.5f);
    }

    void Update()
    {
        if (Time.timeScale == 0f || morto) return;
        float dt = Time.deltaTime;
        flash -= dt; stun -= dt; recuo -= dt; animT += dt;

        Player p = Player.I;
        Vector2 pos = transform.position;
        float vel = velocidade + 1.2f;
        Vector2 alvo;

        if (recuo > 0f && p != null)
        {
            // depois da mordida: foge um pouco para cima
            Vector2 fora = (pos - (Vector2)p.transform.position).normalized;
            alvo = (fora + Vector2.up * 0.6f).normalized * vel * 0.9f;
        }
        else if (p != null && p.Vivo && Vector2.Distance(pos, p.transform.position) < raio && TemVisao(pos, (Vector2)p.transform.position + Vector2.up * 0.7f))
        {
            Vector2 ate = ((Vector2)p.transform.position + Vector2.up * 0.7f - pos).normalized;
            Vector2 lado = new Vector2(-ate.y, ate.x) * Mathf.Sin(Time.time * 6f + fase) * 0.8f;   // voo "bêbado"
            alvo = ate * vel + lado;
        }
        else
        {
            alvo = new Vector2(Mathf.Cos(Time.time * 1.3f + fase) * 0.6f, Mathf.Sin(Time.time * 2.1f + fase) * 0.9f);
        }

        if (stun <= 0f)
        {
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, alvo, 1f - Mathf.Exp(-6f * dt));
        }

        // desenho
        Sprite s = PixelArt.morcego[(int)(animT / 0.08f) % PixelArt.morcego.Length];
        sr.sprite = flash > 0f ? PixelArt.Branco(s) : s;
        if (Mathf.Abs(rb.linearVelocity.x) > 0.3f) sr.flipX = rb.linearVelocity.x < 0f;
    }

    // Tem parede entre o morcego e o herói?
    bool TemVisao(Vector2 de, Vector2 ate)
    {
        RaycastHit2D[] hits = Physics2D.LinecastAll(de, ate);
        for (int i = 0; i < hits.Length; i++)
        {
            if (!hits[i].collider.isTrigger && hits[i].collider.CompareTag("Ground")) return false;
        }
        return true;
    }

    void OnCollisionStay2D(Collision2D c)
    {
        if (morto) return;
        Player p = c.collider.GetComponent<Player>();
        if (p != null)
        {
            p.LevarDano(1, transform.position);
            recuo = 0.8f;
        }
    }

    public void LevarDano(int dano, Vector2 origem)
    {
        if (morto) return;

        vida -= dano;
        flash = 0.12f;
        stun = 0.15f;

        Vector2 fora = ((Vector2)transform.position - origem).normalized;
        rb.linearVelocity = fora * 6f;

        Efeitos.Faiscas(transform.position, new Color(0.7f, 0.4f, 0.9f), 6);

        if (vida <= 0)
        {
            morto = true;
            Som.Tocar("inimigo_morre");
            Efeitos.Faiscas(transform.position, new Color(0.7f, 0.4f, 0.9f), 14, 6f, 0.6f);
            Destroy(gameObject);
        }
    }
}
