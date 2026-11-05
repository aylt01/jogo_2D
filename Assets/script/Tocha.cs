using UnityEngine;

// Tocha de parede animada, com um brilho que treme (enfeite da masmorra)
public class Tocha : MonoBehaviour
{
    private SpriteRenderer sr;
    private SpriteRenderer brilho;
    private float fase;

    public static void Criar(Vector2 pos, Transform pai)
    {
        PixelArt.Iniciar();

        GameObject go = new GameObject("Tocha");
        go.transform.parent = pai;
        go.transform.position = pos;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = PixelArt.tocha[0];
        sr.sortingOrder = -6;

        GameObject luz = new GameObject("Brilho");
        luz.transform.parent = go.transform;
        luz.transform.localPosition = new Vector3(0f, 0.3f, 0f);
        luz.transform.localScale = Vector3.one * 2.2f;
        SpriteRenderer lsr = luz.AddComponent<SpriteRenderer>();
        lsr.sprite = PixelArt.brilho;
        lsr.sortingOrder = -7;

        Tocha t = go.AddComponent<Tocha>();
        t.sr = sr;
        t.brilho = lsr;
        t.fase = Random.value * 10f;
    }

    void Update()
    {
        // a chama troca de quadro e o brilho treme
        int f = (int)((Time.time + fase) / 0.12f) % PixelArt.tocha.Length;
        sr.sprite = PixelArt.tocha[f];

        float tremor = 0.75f + 0.25f * Mathf.PerlinNoise(Time.time * 5f, fase);
        brilho.color = new Color(1f, 1f, 1f, tremor);
    }
}
