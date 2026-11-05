using UnityEngine;

// Câmera que segue o herói com suavidade, olha um pouco para onde ele anda e treme nos golpes.
public class CameraSegue : MonoBehaviour
{
    public static CameraSegue I;

    public Transform alvo;
    public float suavidade = 7f;
    public Vector2 deslocamento = new Vector2(0f, 1.2f);

    public bool usarLimites;
    public Rect limites;            // área do mapa (a câmera não mostra fora dela)

    private Camera cam;
    private Vector3 posBase;
    private float olharX;
    private float tremorTempo, tremorForca;

    void Awake()
    {
        I = this;
        cam = GetComponent<Camera>();
        posBase = transform.position;
    }

    void OnDestroy()
    {
        if (I == this) I = null;
    }

    public void Tremer(float duracao, float forca)
    {
        tremorTempo = Mathf.Max(tremorTempo, duracao);
        tremorForca = Mathf.Max(tremorForca, forca);
    }

    public void IrParaOAlvo()
    {
        if (alvo == null) return;
        olharX = 0f;
        posBase = Destino();
        transform.position = posBase;
    }

    Vector3 Destino()
    {
        float x = alvo.position.x + olharX + deslocamento.x;
        float y = alvo.position.y + deslocamento.y;

        if (usarLimites && cam != null)
        {
            float metadeH = cam.orthographicSize;
            float metadeW = metadeH * cam.aspect;

            float minX = limites.xMin + metadeW, maxX = limites.xMax - metadeW;
            float minY = limites.yMin + metadeH, maxY = limites.yMax - metadeH;
            x = (minX > maxX) ? limites.center.x : Mathf.Clamp(x, minX, maxX);
            y = (minY > maxY) ? limites.center.y : Mathf.Clamp(y, minY, maxY);
        }

        return new Vector3(x, y, -10f);
    }

    void LateUpdate()
    {
        if (alvo == null) return;

        float dt = Time.deltaTime;
        if (Player.I != null) olharX = Mathf.Lerp(olharX, Player.I.Direcao * 1.5f, 1f - Mathf.Exp(-3f * dt));

        posBase = Vector3.Lerp(posBase, Destino(), 1f - Mathf.Exp(-suavidade * dt));

        Vector3 tremor = Vector3.zero;
        if (tremorTempo > 0f)
        {
            tremorTempo -= Time.unscaledDeltaTime;
            tremor = (Vector3)(Random.insideUnitCircle * tremorForca);
            if (tremorTempo <= 0f) tremorForca = 0f;
        }

        transform.position = posBase + tremor;
    }
}
