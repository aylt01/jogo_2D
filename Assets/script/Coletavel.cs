using UnityEngine;

public enum TipoColetavel { Chave, Coracao }

// Itens que flutuam e são pegos ao encostar: chave (abre a porta da masmorra) e coração (cura 1).
public class Coletavel : MonoBehaviour
{
    public TipoColetavel tipo;

    private Vector3 baseP;
    private float fase;

    public static Coletavel Criar(TipoColetavel t, Vector2 pos)
    {
        PixelArt.Iniciar();

        GameObject go = new GameObject(t == TipoColetavel.Chave ? "Chave" : "Coracao");
        go.transform.position = pos;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = t == TipoColetavel.Chave ? PixelArt.chave : PixelArt.coracao;
        sr.sortingLayerName = GameManager.camadaNome;
        sr.sortingOrder = GameManager.ordemBase + 4;

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.5f;

        Coletavel c = go.AddComponent<Coletavel>();
        c.tipo = t;
        return c;
    }

    void Start()
    {
        baseP = transform.position;
        fase = Random.value * 6f;
    }

    void Update()
    {
        transform.position = baseP + Vector3.up * Mathf.Sin(Time.time * 3f + fase) * 0.1f;
    }

    void OnTriggerStay2D(Collider2D outro)
    {
        Player p = outro.GetComponent<Player>();
        if (p == null || !p.Vivo) return;

        if (tipo == TipoColetavel.Chave)
        {
            if (GameManager.I != null) GameManager.I.temChave = true;
            GameManager.Aviso("Você pegou a chave da masmorra!");
            Som.Tocar("pegar");
            Efeitos.Faiscas(transform.position, new Color(1f, 0.85f, 0.3f), 12, 3f, 0.5f, -1f);
            Destroy(gameObject);
        }
        else if (p.Curar(1))
        {
            Som.Tocar("pegar");
            Destroy(gameObject);
        }
    }
}
