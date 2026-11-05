using UnityEngine;

// Partículas de pixel: faíscas, poeira e fumaça. Cada uma é um quadradinho que voa e some.
public class Particula : MonoBehaviour
{
    public Vector2 vel;
    public float gravidade = 12f;
    public float vida = 0.4f;
    public float tempo;
    public float tamanho = 1f;
    SpriteRenderer sr;
    Color cor;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        cor = sr.color;
    }

    void Update()
    {
        tempo += Time.deltaTime;
        vel.y -= gravidade * Time.deltaTime;
        transform.position += (Vector3)(vel * Time.deltaTime);

        float k = 1f - tempo / vida;
        sr.color = new Color(cor.r, cor.g, cor.b, cor.a * Mathf.Clamp01(k * 1.5f));
        transform.localScale = Vector3.one * tamanho * Mathf.Clamp01(0.4f + k);

        if (tempo >= vida) Destroy(gameObject);
    }
}

public static class Efeitos
{
    public static void Faiscas(Vector2 pos, Color cor, int quantidade, float velocidade = 4.5f, float vida = 0.4f, float gravidade = 12f)
    {
        PixelArt.Iniciar();
        for (int i = 0; i < quantidade; i++)
        {
            GameObject go = new GameObject("Particula");
            go.transform.position = pos + Random.insideUnitCircle * 0.15f;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PixelArt.pixel;
            sr.color = cor;
            sr.sortingLayerName = GameManager.camadaNome;
            sr.sortingOrder = GameManager.ordemBase + 20;

            Particula p = go.AddComponent<Particula>();
            float ang = Random.value * Mathf.PI * 2f;
            p.vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang) * 0.9f + 0.4f) * velocidade * Random.Range(0.4f, 1f);
            p.vida = vida * Random.Range(0.7f, 1.2f);
            p.gravidade = gravidade;
            p.tamanho = Random.Range(0.8f, 1.6f);
        }
    }

    public static void Poeira(Vector2 pos, int direcao)
    {
        PixelArt.Iniciar();
        for (int i = 0; i < 4; i++)
        {
            GameObject go = new GameObject("Poeira");
            go.transform.position = pos + new Vector2(Random.Range(-0.2f, 0.2f), 0.05f);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PixelArt.pixel;
            sr.color = new Color(0.85f, 0.85f, 0.9f, 0.8f);
            sr.sortingLayerName = GameManager.camadaNome;
            sr.sortingOrder = GameManager.ordemBase - 1;

            Particula p = go.AddComponent<Particula>();
            p.vel = new Vector2(-direcao * Random.Range(0.5f, 1.8f), Random.Range(0.3f, 1.2f));
            p.vida = 0.3f;
            p.gravidade = 0f;
            p.tamanho = Random.Range(1.2f, 2.2f);
        }
    }
}


// Toca uma lista de quadros uma vez e some (usado no brilho do golpe da espada)
public class AnimacaoUnica : MonoBehaviour
{
    public Sprite[] quadros;
    public float tempoPorQuadro = 0.06f;
    private float t;
    private SpriteRenderer sr;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        sr.sprite = quadros[0];
    }

    void Update()
    {
        t += Time.deltaTime;
        int i = (int)(t / tempoPorQuadro);
        if (i >= quadros.Length) { Destroy(gameObject); return; }
        sr.sprite = quadros[i];
    }
}
