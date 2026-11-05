using UnityEngine;
using UnityEngine.Tilemaps;

// Monta a Fase 2 (a masmorra) a partir de um mapa em texto, na hora que a cena "a2" abre.
//   #  parede de pedra        ^  espinhos            P  começo do herói
//   s  slime                  S  esqueleto           b  morcego            M  chefe
//   k  chave                  h  coração (cura)      D  porta de saída (precisa da chave)
//   T  tocha (enfeite)        .  vazio
// Para mudar a fase, é só mexer nas letras do mapa abaixo (a primeira linha é a de CIMA).
public static class DungeonBuilder
{
    static readonly string[] Mapa = new string[]
    {
        "##################################################################################################################",
        "##############################################################..................................................##",
        "##############################################################.....................T......b..T....T.............##",
        "##############################################################..................................................##",
        "##############################################################..................................................##",
        "##############################################################...............T.............S..^^.h.k............##",
        "##############################################################.................b....#################...........##",
        "##############################################################......................#################...........##",
        "##############################################################...................####################...........##",
        "##############################################################.........T.........####################...........##",
        "##############################################################................#######################...........##",
        "##################################....................................b.......#######################...........##",
        "##################################.........................................##########################...........##",
        "##..........................######...............................T.........##########################...T....T..##",
        "##..........................######........................b.............#############################...........##",
        "##..........................######.............b........................#############################...........##",
        "##..........................######....T.........T.........T..........################################...........##",
        "##......T..........T........######...................................################################...........##",
        "##................................................................###################################...........##",
        "##.............................T..................................###################################...........##",
        "##..........................................................h..######################################...........##",
        "##..P.......s...^^....s.h...........s.........s.......s.##..s..######################################..hS..M..D.##",
        "########################################...#######...#############################################################",
        "########################################^^^#######^^^#############################################################",
        "##################################################################################################################",
        "##################################################################################################################"
    };

    public static int Largura { get { return Mapa[0].Length; } }
    public static int Altura { get { return Mapa.Length; } }

    // y = 0 é a linha de baixo
    static char Letra(int x, int y)
    {
        int linha = Altura - 1 - y;
        if (x < 0 || y < 0 || x >= Largura || linha < 0 || linha >= Altura) return '#';
        return Mapa[linha][x];
    }

    public static void Construir()
    {
        PixelArt.Iniciar();

        GameManager.camadaNome = "Default";
        GameManager.ordemBase = 0;
        GameManager.limiteQueda = -6f;

        Camera cam = Camera.main;
        cam.orthographic = true;
        cam.orthographicSize = 6f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.04f, 0.03f, 0.09f);

        Transform raiz = new GameObject("Masmorra").transform;

        CriarFundo(raiz);
        CriarChao(raiz);
        GameObject player = CriarObjetos(raiz);

        CameraSegue seg = cam.gameObject.GetComponent<CameraSegue>();
        if (seg == null) seg = cam.gameObject.AddComponent<CameraSegue>();
        seg.alvo = player.transform;
        seg.usarLimites = true;
        seg.limites = new Rect(0f, 0f, Largura, Altura);
        seg.deslocamento = new Vector2(0f, 1.0f);
        seg.IrParaOAlvo();
    }

    static void CriarFundo(Transform raiz)
    {
        GameObject fundo = new GameObject("Fundo");
        fundo.transform.parent = raiz;
        fundo.transform.position = new Vector3(Largura / 2f, Altura / 2f, 0f);

        SpriteRenderer sr = fundo.AddComponent<SpriteRenderer>();
        sr.sprite = PixelArt.fundoTijolo;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = new Vector2(Largura, Altura);
        sr.sortingOrder = -50;
    }

    static void CriarChao(Transform raiz)
    {
        GameObject grade = new GameObject("Grade");
        grade.transform.parent = raiz;
        grade.AddComponent<Grid>();

        GameObject obj = new GameObject("Chao");
        obj.transform.parent = grade.transform;
        obj.tag = "Ground";                 // o herói só pisa em coisas com a tag Ground

        Tilemap tilemap = obj.AddComponent<Tilemap>();
        TilemapRenderer render = obj.AddComponent<TilemapRenderer>();
        render.sortingOrder = -10;

        Rigidbody2D corpo = obj.AddComponent<Rigidbody2D>();
        corpo.bodyType = RigidbodyType2D.Static;
        CompositeCollider2D composto = obj.AddComponent<CompositeCollider2D>();
        composto.geometryType = CompositeCollider2D.GeometryType.Polygons;
        TilemapCollider2D colisor = obj.AddComponent<TilemapCollider2D>();
        colisor.compositeOperation = Collider2D.CompositeOperation.Merge;

        Tile[] corpos = new Tile[PixelArt.paredeCorpo.Length];
        Tile[] topos = new Tile[PixelArt.paredeTopo.Length];
        for (int i = 0; i < corpos.Length; i++) corpos[i] = NovoTile(PixelArt.paredeCorpo[i]);
        for (int i = 0; i < topos.Length; i++) topos[i] = NovoTile(PixelArt.paredeTopo[i]);

        for (int x = 0; x < Largura; x++)
        {
            for (int y = 0; y < Altura; y++)
            {
                if (Letra(x, y) != '#') continue;

                bool temParedeEmCima = Letra(x, y + 1) == '#';
                int sorteio = (x * 7 + y * 13) % 6;
                Tile t = temParedeEmCima ? corpos[sorteio % corpos.Length] : topos[sorteio % topos.Length];
                tilemap.SetTile(new Vector3Int(x, y, 0), t);
            }
        }
    }

    static Tile NovoTile(Sprite s)
    {
        Tile t = ScriptableObject.CreateInstance<Tile>();
        t.sprite = s;
        t.colliderType = Tile.ColliderType.Grid;
        return t;
    }

    static GameObject CriarObjetos(Transform raiz)
    {
        GameObject player = null;

        for (int x = 0; x < Largura; x++)
        {
            for (int y = 0; y < Altura; y++)
            {
                char c = Letra(x, y);
                Vector2 pes = new Vector2(x + 0.5f, y + 0.02f);

                switch (c)
                {
                    case 'P':
                        player = new GameObject("Player");
                        player.tag = "Player";
                        player.transform.position = pes;
                        player.AddComponent<Player>();
                        SpriteRenderer psr = player.GetComponent<SpriteRenderer>();
                        psr.sortingLayerName = "Default";
                        psr.sortingOrder = 10;
                        break;
                    case '^': CriarEspinhos(raiz, x, y); break;
                    case 'T': Tocha.Criar(new Vector2(x + 0.5f, y + 0.5f), raiz); break;
                    case 's': Inimigo.Criar(TipoInimigo.Slime, pes).transform.parent = raiz; break;
                    case 'S': Inimigo.Criar(TipoInimigo.Esqueleto, pes).transform.parent = raiz; break;
                    case 'M': Inimigo.Criar(TipoInimigo.Chefe, pes).transform.parent = raiz; break;
                    case 'b': InimigoSeguri.Criar(new Vector2(x + 0.5f, y + 0.5f)).transform.parent = raiz; break;
                    case 'k': Coletavel.Criar(TipoColetavel.Chave, new Vector2(x + 0.5f, y + 0.5f)); break;
                    case 'h': Coletavel.Criar(TipoColetavel.Coracao, new Vector2(x + 0.5f, y + 0.5f)); break;
                    case 'D': PortaMasmorra.Criar(new Vector2(x + 0.5f, y)); break;
                }
            }
        }

        return player;
    }

    static void CriarEspinhos(Transform raiz, int x, int y)
    {
        GameObject e = new GameObject("Espinhos");
        e.transform.parent = raiz;
        e.transform.position = new Vector3(x + 0.5f, y, 0f);
        e.tag = "Dano";

        SpriteRenderer sr = e.AddComponent<SpriteRenderer>();
        sr.sprite = PixelArt.espinhos;
        sr.sortingOrder = -5;

        BoxCollider2D col = e.AddComponent<BoxCollider2D>();
        col.isTrigger = true;                       // o herói pode cair dentro dos espinhos e se machucar
        col.size = new Vector2(0.9f, 0.35f);
        col.offset = new Vector2(0f, 0.2f);
    }
}
