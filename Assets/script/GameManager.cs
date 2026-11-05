using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// O "diretor" do jogo. Ele se cria sozinho em qualquer cena (não precisa colocar na cena) e cuida de:
//   tela de início, pausa, tela de morte (Game Over), fim de fase, vitória, HUD, música e da preparação de cada fase.
public class GameManager : MonoBehaviour
{
    public static GameManager I;

    public enum Estado { Menu, Jogando, Pausado, GameOver, FaseCompleta, Vitoria }
    public Estado estado = Estado.Menu;

    public const string NomeDoJogo = "CAVALEIRO PIXEL";
    public const string Subtitulo = "Uma aventura na masmorra";

    // Onde desenhar herói e inimigos (cada fase define)
    public static string camadaNome = "Default";
    public static int ordemBase = 0;
    public static float limiteQueda = -20f;

    public static bool DashLiberado = false;        // fica liberado depois de pegar o baú
    static bool menuJaVisto = false;                // a tela de início só aparece quando o jogo abre

    public bool temChave;

    private Scene ultimaCena;
    private bool temUltimaCena;
    private int sel;                                // botão escolhido no menu
    private bool mostrandoAjuda;
    private float tempoJogando;
    private string avisoTexto = "";
    private float avisoTimer;
    private int hitStopId;
    private AudioSource musica;


    // ------------------------------------------------------------------ nascimento
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Criar()
    {
        if (I != null) return;
        GameObject go = new GameObject("GameManager");
        DontDestroyOnLoad(go);
        go.AddComponent<GameManager>();
    }

    void Awake()
    {
        I = this;
        PixelArt.Iniciar();
        Physics2D.queriesStartInColliders = false;
        SceneManager.sceneLoaded += AoCarregarCena;

        musica = gameObject.AddComponent<AudioSource>();
        musica.clip = Som.Musica();
        musica.loop = true;
        musica.volume = 0.16f;
        musica.Play();
    }

    void Start()
    {
        StartCoroutine(Configurar(SceneManager.GetActiveScene()));
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= AoCarregarCena;
        if (I == this) I = null;
    }

    void AoCarregarCena(Scene cena, LoadSceneMode modo)
    {
        StartCoroutine(Configurar(cena));
    }

    public static void Aviso(string texto)
    {
        if (I == null) return;
        I.avisoTexto = texto;
        I.avisoTimer = 3.2f;
    }

    string NomeDaFase()
    {
        return SceneManager.GetActiveScene().name == "a2" ? "FASE 2 - A MASMORRA" : "FASE 1 - A VILA";
    }


    // ------------------------------------------------------------------ preparar a fase
    IEnumerator Configurar(Scene cena)
    {
        if (temUltimaCena && cena == ultimaCena) yield break;
        ultimaCena = cena;
        temUltimaCena = true;

        yield return null;      // espera os Start() da cena rodarem

        if (!cena.isLoaded || SceneManager.GetActiveScene() != cena) yield break;

        temChave = false;
        avisoTimer = 0f;
        mostrandoAjuda = false;
        tempoJogando = 0f;
        sel = 0;
        Inimigo.Chefe = null;

        GameObject player = GameObject.FindWithTag("Player");

        if (cena.name == "a2")
        {
            DungeonBuilder.Construir();
        }
        else if (player != null)
        {
            PrepararFase1(player);
        }
        else
        {
            yield break;        // cena sem herói (ex.: cena de teste): não faz nada
        }

        Player p = Player.I;
        if (p != null)
        {
            p.temDash = DashLiberado;
            SpriteRenderer sr = p.GetComponent<SpriteRenderer>();
            sr.sortingLayerName = camadaNome;
            sr.sortingOrder = ordemBase + 10;
        }

        // o morcego que já existia na cena da fase 1
        foreach (InimigoSeguri m in FindObjectsByType<InimigoSeguri>())
        {
            SpriteRenderer msr = m.GetComponent<SpriteRenderer>();
            msr.sortingLayerName = camadaNome;
            msr.sortingOrder = ordemBase + 6;
        }

        if (!menuJaVisto)
        {
            estado = Estado.Menu;
            Time.timeScale = 0f;
        }
        else
        {
            estado = Estado.Jogando;
            Time.timeScale = 1f;
        }
    }

    // Fase 1 (cena "a"): usa o cenário que já existe e só troca o herói, solta a câmera e põe os inimigos
    void PrepararFase1(GameObject player)
    {
        Camera cam = Camera.main;
        SpriteRenderer sr = player.GetComponent<SpriteRenderer>();
        camadaNome = sr != null ? sr.sortingLayerName : "Default";
        ordemBase = sr != null ? sr.sortingOrder : 0;
        limiteQueda = -18f;

        // o herói estava dentro de outro objeto (com física própria) e a câmera dentro do herói
        cam.transform.SetParent(null, true);
        Transform pai = player.transform.parent;
        player.transform.SetParent(null, true);
        if (pai != null) pai.gameObject.SetActive(false);

        CameraSegue seg = cam.gameObject.GetComponent<CameraSegue>();
        if (seg == null) seg = cam.gameObject.AddComponent<CameraSegue>();
        seg.alvo = player.transform;
        seg.usarLimites = true;
        seg.limites = new Rect(-20.4f, -9.5f, 92.3f, 40f);
        seg.deslocamento = new Vector2(0f, 1.6f);
        seg.IrParaOAlvo();

        // inimigos da fase 1 (ficam em cima do chão que já existe)
        float[] slimes = { 24f, 33f, 46f };
        float[] esqueletos = { 39f, 50f };
        foreach (float x in slimes) Colocar(TipoInimigo.Slime, x);
        foreach (float x in esqueletos) Colocar(TipoInimigo.Esqueleto, x);

        InimigoSeguri.Criar(new Vector2(31f, -3.5f));
        InimigoSeguri.Criar(new Vector2(44f, -2.5f));
        InimigoSeguri.Criar(new Vector2(54f, -3.5f));

        Vector2 c;
        if (ChaoEm(22f, out c)) Coletavel.Criar(TipoColetavel.Coracao, c + Vector2.up * 0.5f);
        if (ChaoEm(41f, out c)) Coletavel.Criar(TipoColetavel.Coracao, c + Vector2.up * 0.5f);
    }

    void Colocar(TipoInimigo tipo, float x)
    {
        Vector2 ponto;
        if (ChaoEm(x, out ponto)) Inimigo.Criar(tipo, ponto + Vector2.up * 0.02f);
    }

    // Procura de cima para baixo o primeiro "Ground" nessa posição x
    bool ChaoEm(float x, out Vector2 ponto)
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(new Vector2(x, 15f), Vector2.down, 60f);
        for (int i = 0; i < hits.Length; i++)
        {
            if (!hits[i].collider.isTrigger && hits[i].collider.CompareTag("Ground"))
            {
                ponto = hits[i].point;
                return true;
            }
        }
        ponto = Vector2.zero;
        return false;
    }


    // ------------------------------------------------------------------ fluxo do jogo
    void Update()
    {
        float dt = Time.unscaledDeltaTime;

        switch (estado)
        {
            case Estado.Jogando:
                tempoJogando += Time.deltaTime;
                avisoTimer -= dt;
                if (Entrada.Pausar()) Pausar();
                break;

            case Estado.Pausado:
                avisoTimer -= dt;
                if (Entrada.Pausar()) Continuar();
                else Navegar(3);
                break;

            case Estado.Menu:
                if (mostrandoAjuda)
                {
                    if (Entrada.Pausar() || Entrada.Confirmar()) mostrandoAjuda = false;
                }
                else Navegar(3);
                break;

            case Estado.GameOver: Navegar(2); break;
            case Estado.FaseCompleta: Navegar(1); break;
            case Estado.Vitoria: Navegar(1); break;
        }
    }

    void Navegar(int quantidade)
    {
        if (Entrada.MenuCima()) { sel = (sel + quantidade - 1) % quantidade; Som.Tocar("clique"); }
        if (Entrada.MenuBaixo()) { sel = (sel + 1) % quantidade; Som.Tocar("clique"); }
        if (Entrada.Confirmar()) Escolher(sel);
    }

    void Escolher(int i)
    {
        Som.Tocar("clique");

        switch (estado)
        {
            case Estado.Menu:
                if (i == 0) Comecar();
                else if (i == 1) mostrandoAjuda = true;
                else Sair();
                break;
            case Estado.Pausado:
                if (i == 0) Continuar();
                else if (i == 1) ReiniciarFase();
                else ParaOMenu();
                break;
            case Estado.GameOver:
                if (i == 0) ReiniciarFase(); else ParaOMenu();
                break;
            case Estado.FaseCompleta: ProximaFase(); break;
            case Estado.Vitoria: ParaOMenu(); break;
        }
    }

    void Comecar()
    {
        menuJaVisto = true;
        estado = Estado.Jogando;
        Time.timeScale = 1f;
        tempoJogando = 0f;
    }

    public void Pausar()
    {
        if (estado != Estado.Jogando) return;
        estado = Estado.Pausado;
        Time.timeScale = 0f;
        sel = 0;
    }

    void Continuar()
    {
        estado = Estado.Jogando;
        Time.timeScale = 1f;
    }

    void ReiniciarFase()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void ParaOMenu()
    {
        menuJaVisto = false;
        DashLiberado = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }

    void ProximaFase()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    // O portal / a porta chamam isto quando o herói chega ao fim
    public void FaseCompleta()
    {
        if (estado != Estado.Jogando) return;

        bool ultima = SceneManager.GetActiveScene().buildIndex + 1 >= SceneManager.sceneCountInBuildSettings;
        estado = ultima ? Estado.Vitoria : Estado.FaseCompleta;
        Time.timeScale = 0f;
        sel = 0;
        Som.Tocar("vitoria");
    }

    // O herói chama isto depois da animação de morte
    public void JogadorMorreu()
    {
        estado = Estado.GameOver;
        Time.timeScale = 0f;
        sel = 0;
    }

    // Congela o jogo por um instante quando um golpe acerta (dá peso ao ataque)
    public void HitStop(float segundos)
    {
        if (estado != Estado.Jogando) return;
        StartCoroutine(HitStopRotina(segundos));
    }

    IEnumerator HitStopRotina(float segundos)
    {
        int id = ++hitStopId;
        Time.timeScale = 0.05f;
        yield return new WaitForSecondsRealtime(segundos);
        if (id == hitStopId && estado == Estado.Jogando) Time.timeScale = 1f;
    }

    void Sair()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }


    // ------------------------------------------------------------------ desenho (OnGUI)
    float esc;      // 1 "pixel de arte" na tela (sempre inteiro, para ficar nítido)

    void OnGUI()
    {
        esc = Mathf.Max(1f, Mathf.Round(Screen.height / 270f));
        float W = Screen.width, Hh = Screen.height;

        switch (estado)
        {
            case Estado.Menu:
                if (mostrandoAjuda) { TelaDeFundo(); DesenharAjuda(W, Hh); }
                else DesenharMenu(W, Hh);
                break;

            case Estado.Jogando:
                DesenharHud(W, Hh);
                break;

            case Estado.Pausado:
                DesenharHud(W, Hh);
                Escurecer(0.65f, new Color(0.02f, 0.02f, 0.08f));
                Texto(new Rect(0, Hh * 0.14f, W, 30 * esc), "PAUSA", 18, new Color(1f, 0.95f, 0.7f));
                DesenharControlesPausa(W, Hh);
                Botoes(W, Hh, new[] { "Continuar", "Reiniciar fase", "Menu principal" }, Hh * 0.62f);
                break;

            case Estado.GameOver:
                DesenharHud(W, Hh);
                Escurecer(0.8f, new Color(0.25f, 0.0f, 0.04f));
                Texto(new Rect(0, Hh * 0.16f, W, 36 * esc), "VOCÊ MORREU", 22, new Color(1f, 0.35f, 0.35f));
                Sprite caido = PixelArt.heroiMorte[2];
                DesenharSprite(caido, new Rect(W / 2f - 24 * esc, Hh * 0.38f, 48 * esc, 48 * esc));
                Botoes(W, Hh, new[] { "Tentar de novo", "Menu principal" }, Hh * 0.68f);
                break;

            case Estado.FaseCompleta:
                Escurecer(0.8f, new Color(0.02f, 0.05f, 0.1f));
                Texto(new Rect(0, Hh * 0.2f, W, 36 * esc), "FASE COMPLETA!", 22, new Color(0.6f, 1f, 0.7f));
                Texto(new Rect(0, Hh * 0.38f, W, 14 * esc), "Prepare-se: a masmorra espera por você...", 8, Color.white);
                Botoes(W, Hh, new[] { "Próxima fase" }, Hh * 0.6f);
                break;

            case Estado.Vitoria:
                TelaDeFundo();
                Escurecer(0.55f, new Color(0.02f, 0.03f, 0.08f));
                Texto(new Rect(0, Hh * 0.18f, W, 36 * esc), "VITÓRIA!", 26, new Color(1f, 0.9f, 0.4f));
                Texto(new Rect(0, Hh * 0.38f, W, 14 * esc), "Você escapou da masmorra. Obrigado por jogar!", 8, Color.white);
                Botoes(W, Hh, new[] { "Menu principal" }, Hh * 0.6f);
                break;
        }
    }

    // ----- peças de interface
    void Retangulo(Rect r, Color c)
    {
        GUI.color = c;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    void Escurecer(float alfa, Color cor)
    {
        Retangulo(new Rect(0, 0, Screen.width, Screen.height), new Color(cor.r, cor.g, cor.b, alfa));
    }

    void DesenharSprite(Sprite s, Rect r, bool espelhar = false)
    {
        if (s == null) return;
        if (espelhar) r = new Rect(r.xMax, r.y, -r.width, r.height);
        GUI.DrawTexture(r, s.texture);
    }

    // Texto com contorno escuro (tamanho dado em "pixels de arte")
    void Texto(Rect r, string s, float tamanho, Color cor, TextAnchor alinhamento = TextAnchor.MiddleCenter)
    {
        GUIStyle st = new GUIStyle(GUI.skin.label);
        st.fontSize = Mathf.RoundToInt(tamanho * esc);
        st.fontStyle = FontStyle.Bold;
        st.alignment = alinhamento;
        st.wordWrap = true;

        float o = Mathf.Max(1f, esc * 0.5f);
        st.normal.textColor = new Color(0.04f, 0.02f, 0.08f, cor.a);
        GUI.Label(new Rect(r.x - o, r.y, r.width, r.height), s, st);
        GUI.Label(new Rect(r.x + o, r.y, r.width, r.height), s, st);
        GUI.Label(new Rect(r.x, r.y - o, r.width, r.height), s, st);
        GUI.Label(new Rect(r.x, r.y + o, r.width, r.height), s, st);
        GUI.Label(new Rect(r.x + o, r.y + o, r.width, r.height), s, st);
        st.normal.textColor = cor;
        GUI.Label(r, s, st);
    }

    // Painel de pixel: fundo escuro com borda dupla
    void Painel(Rect r)
    {
        float b = esc;
        Retangulo(r, new Color(0.05f, 0.04f, 0.1f, 0.9f));
        Retangulo(new Rect(r.x, r.y, r.width, b), new Color(0.55f, 0.6f, 0.85f));
        Retangulo(new Rect(r.x, r.yMax - b, r.width, b), new Color(0.55f, 0.6f, 0.85f));
        Retangulo(new Rect(r.x, r.y, b, r.height), new Color(0.55f, 0.6f, 0.85f));
        Retangulo(new Rect(r.xMax - b, r.y, b, r.height), new Color(0.55f, 0.6f, 0.85f));
        Retangulo(new Rect(r.x + b * 2, r.y + b * 2, r.width - b * 4, b), new Color(0.2f, 0.22f, 0.4f));
    }

    // Lista vertical de botões (teclado W/S + Enter, ou mouse)
    void Botoes(float W, float Hh, string[] nomes, float yInicial)
    {
        float bw = 110 * esc, bh = 18 * esc, esp = 6 * esc;
        float x = W / 2f - bw / 2f;
        BotoesEm(x, yInicial, bw, bh, esp, nomes);
    }

    void BotoesEm(float x, float yInicial, float bw, float bh, float esp, string[] nomes)
    {
        for (int i = 0; i < nomes.Length; i++)
        {
            Rect r = new Rect(x, yInicial + i * (bh + esp), bw, bh);

            if (Event.current.type == EventType.MouseMove && r.Contains(Event.current.mousePosition)) sel = i;

            bool escolhido = (sel == i);
            Painel(r);
            if (escolhido)
            {
                float pulsa = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 8f);
                Retangulo(new Rect(r.x + esc * 2, r.y + esc * 2, r.width - esc * 4, r.height - esc * 4), new Color(0.25f + 0.1f * pulsa, 0.4f, 0.8f, 0.7f));
                Texto(r, "> " + nomes[i] + " <", 8, new Color(1f, 0.95f, 0.6f));
            }
            else Texto(r, nomes[i], 8, new Color(0.85f, 0.88f, 1f));

            if (GUI.Button(r, GUIContent.none, GUIStyle.none)) { sel = i; Escolher(i); }
        }
    }

    // ----- telas
    void TelaDeFundo()
    {
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), PixelArt.tituloFundo, ScaleMode.ScaleAndCrop);
    }

    void DesenharMenu(float W, float Hh)
    {
        TelaDeFundo();
        Escurecer(0.25f, new Color(0f, 0f, 0.05f));

        Texto(new Rect(0, Hh * 0.07f, W, 36 * esc), NomeDoJogo, 24, new Color(1f, 0.92f, 0.5f));
        Texto(new Rect(0, Hh * 0.07f + 34 * esc, W, 14 * esc), Subtitulo, 8, new Color(0.75f, 0.85f, 1f));

        // herói de pé ao lado dos botões
        int f = (int)(Time.unscaledTime / 0.5f) % PixelArt.heroiParado.Length;
        DesenharSprite(PixelArt.heroiParado[f], new Rect(W * 0.14f, Hh * 0.52f, 48 * esc, 48 * esc));

        float bw = 110 * esc;
        BotoesEm(W * 0.62f - bw / 2f, Hh * 0.46f, bw, 18 * esc, 6 * esc, new[] { "Jogar", "Como jogar", "Sair" });

        float pisca = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 3f);
        Texto(new Rect(0, Hh - 22 * esc, W, 14 * esc), "W / S e Enter  ou  mouse", 6, new Color(1f, 1f, 1f, pisca));
    }

    void DesenharAjuda(float W, float Hh)
    {
        Rect p = new Rect(W * 0.2f, Hh * 0.12f, W * 0.6f, Hh * 0.76f);
        Painel(p);
        Texto(new Rect(p.x, p.y + 4 * esc, p.width, 20 * esc), "COMO JOGAR", 12, new Color(1f, 0.92f, 0.5f));

        string t =
            "A / D ou setas ......... andar\n" +
            "Espaço ou W ............ pular\n" +
            "J, Z ou mouse .......... atacar com a espada\n" +
            "X ou Shift ............. dash (pegue o baú da fase 1)\n" +
            "Esc ou P ............... pausar\n\n" +
            "Derrote slimes, morcegos e esqueletos.\n" +
            "Cuidado com os espinhos e os buracos!\n" +
            "Na masmorra, pegue a chave e abra a porta.";
        Texto(new Rect(p.x + 10 * esc, p.y + 28 * esc, p.width - 20 * esc, p.height - 50 * esc), t, 7.5f, Color.white, TextAnchor.UpperLeft);
        Texto(new Rect(p.x, p.yMax - 18 * esc, p.width, 14 * esc), "Enter para voltar", 6, new Color(0.8f, 0.85f, 1f));
    }

    void DesenharControlesPausa(float W, float Hh)
    {
        Texto(new Rect(0, Hh * 0.30f, W, 40 * esc),
            "A/D andar   Espaço pular   J atacar   X dash   Esc pausar", 6.5f, new Color(0.8f, 0.85f, 1f));
    }

    // ----- HUD
    void DesenharHud(float W, float Hh)
    {
        Player p = Player.I;
        if (p == null) return;

        float tam = 9 * esc;
        float margem = 6 * esc;

        // corações
        for (int i = 0; i < p.vidaMax; i++)
        {
            Texture2D t = i < p.vida ? PixelArt.uiCoracao : PixelArt.uiCoracaoVazio;
            GUI.DrawTexture(new Rect(margem + i * (tam + esc), margem, tam, tam * 8f / 9f), t);
        }

        // chave e dash
        float y2 = margem + tam + 3 * esc;
        float x2 = margem;
        if (temChave)
        {
            GUI.DrawTexture(new Rect(x2, y2, 10 * esc, 10 * esc), PixelArt.uiChave);
            x2 += 12 * esc;
        }
        if (p.temDash)
        {
            GUI.DrawTexture(new Rect(x2, y2, 11 * esc, 9 * esc), PixelArt.uiDash);
            Retangulo(new Rect(x2 + 12 * esc, y2 + 3 * esc, 22 * esc, 3 * esc), new Color(0.1f, 0.12f, 0.25f));
            Retangulo(new Rect(x2 + 12 * esc, y2 + 3 * esc, 22 * esc * p.DashRecarga, 3 * esc), new Color(0.4f, 0.85f, 1f));
        }

        // nome da fase
        Texto(new Rect(W - 150 * esc, margem, 144 * esc, 12 * esc), NomeDaFase(), 6.5f, new Color(1f, 0.92f, 0.6f), TextAnchor.MiddleRight);

        // barra de vida do chefe
        Inimigo chefe = Inimigo.Chefe;
        if (chefe != null && chefe.Vivo && Vector2.Distance(chefe.transform.position, p.transform.position) < 16f)
        {
            float bw = Mathf.Min(W * 0.5f, 200 * esc);
            Rect area = new Rect(W / 2f - bw / 2f, Hh - 30 * esc, bw, 22 * esc);
            Painel(area);
            Texto(new Rect(area.x, area.y + esc, area.width, 9 * esc), "REI ESQUELETO", 6, new Color(1f, 0.9f, 0.75f));
            Rect barra = new Rect(area.x + 4 * esc, area.yMax - 9 * esc, area.width - 8 * esc, 5 * esc);
            Retangulo(barra, new Color(0.2f, 0.04f, 0.06f));
            Retangulo(new Rect(barra.x, barra.y, barra.width * Mathf.Clamp01(chefe.vida / (float)chefe.vidaMax), barra.height), new Color(0.9f, 0.25f, 0.2f));
        }

        // avisos no meio da tela
        if (avisoTimer > 0f)
        {
            float a = Mathf.Clamp01(avisoTimer / 0.6f);
            Rect r = new Rect(W * 0.15f, Hh * 0.16f, W * 0.7f, 18 * esc);
            GUI.color = new Color(1f, 1f, 1f, a);
            Painel(r);
            Texto(r, avisoTexto, 7.5f, new Color(1f, 0.95f, 0.7f, a));
            GUI.color = Color.white;
        }

        // dica dos controles nos primeiros segundos
        if (estado == Estado.Jogando && tempoJogando < 8f)
        {
            float a = Mathf.Clamp01((8f - tempoJogando) / 2f);
            Texto(new Rect(0, Hh - 20 * esc, W, 14 * esc), "A/D andar    Espaço pular    J atacar    Esc pausa", 6.5f, new Color(1f, 1f, 1f, a));
        }
    }
}
