using UnityEngine;

// O nome da classe precisa ser igual ao nome do arquivo (habilidade.cs),
// senão o Unity não consegue ligar o script ao objeto.
// Ao pegar o baú (tag "bau") o herói ganha o DASH (X ou Shift).
public class habilidade : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("bau")) return;

        Player p = GetComponent<Player>();
        if (p == null || p.temDash) return;

        p.LiberarDash();
        Som.Tocar("pegar");
        Efeitos.Faiscas(other.bounds.center, new Color(1f, 0.85f, 0.3f), 16, 4f, 0.6f, 2f);
        GameManager.Aviso("Habilidade nova: DASH! Aperte X ou Shift");

        // o baú some
        SpriteRenderer sr = other.GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = false;
        other.enabled = false;
    }
}
