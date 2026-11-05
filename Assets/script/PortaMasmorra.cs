using System.Collections;
using UnityEngine;

// A porta de saída da masmorra: só abre se o herói tiver a chave.
public class PortaMasmorra : MonoBehaviour
{
    private SpriteRenderer sr;
    private bool aberta;
    private float proximoAviso;

    public static PortaMasmorra Criar(Vector2 pes)
    {
        PixelArt.Iniciar();

        GameObject go = new GameObject("Porta");
        go.transform.position = pes;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = PixelArt.portaFechada;
        sr.sortingOrder = -4;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1.2f, 2.2f);
        col.offset = new Vector2(0f, 1.1f);

        PortaMasmorra p = go.AddComponent<PortaMasmorra>();
        p.sr = sr;
        return p;
    }

    void OnTriggerStay2D(Collider2D outro)
    {
        if (aberta || outro.GetComponent<Player>() == null) return;

        if (GameManager.I != null && GameManager.I.temChave)
        {
            aberta = true;
            sr.sprite = PixelArt.portaAberta;
            Som.Tocar("porta");
            StartCoroutine(Terminar());
        }
        else if (Time.time > proximoAviso)
        {
            GameManager.Aviso("A porta está trancada. Ache a chave!");
            proximoAviso = Time.time + 2.5f;
        }
    }

    IEnumerator Terminar()
    {
        yield return new WaitForSeconds(0.8f);
        if (GameManager.I != null) GameManager.I.FaseCompleta();
    }
}
