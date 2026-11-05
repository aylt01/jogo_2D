using System.Collections.Generic;
using UnityEngine;

// Uma "tela" de pixels para desenhar pixel art por código (y = 0 é a linha de CIMA).
public class Pix
{
    public int w, h;
    public Color32[] d;

    public Pix(int w, int h)
    {
        this.w = w; this.h = h;
        d = new Color32[w * h];
    }

    public bool Dentro(int x, int y) { return x >= 0 && y >= 0 && x < w && y < h; }

    public void P(int x, int y, Color32 c)
    {
        if (Dentro(x, y)) d[y * w + x] = c;
    }

    public void R(int x, int y, int ww, int hh, Color32 c)
    {
        for (int j = 0; j < hh; j++)
            for (int i = 0; i < ww; i++)
                P(x + i, y + j, c);
    }

    public void Linha(int x0, int y0, int x1, int y1, Color32 c)
    {
        int dx = Mathf.Abs(x1 - x0), dy = -Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
        int err = dx + dy;
        while (true)
        {
            P(x0, y0, c);
            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 >= dy) { err += dy; x0 += sx; }
            if (e2 <= dx) { err += dx; y0 += sy; }
        }
    }

    public void Elipse(float cx, float cy, float rx, float ry, Color32 c)
    {
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float nx = (x + 0.5f - cx) / rx, ny = (y + 0.5f - cy) / ry;
                if (nx * nx + ny * ny <= 1f) P(x, y, c);
            }
    }

    // Triângulo preenchido (asas do morcego)
    public void Tri(int ax, int ay, int bx, int by, int cx, int cy, Color32 c)
    {
        int minX = Mathf.Min(ax, Mathf.Min(bx, cx)), maxX = Mathf.Max(ax, Mathf.Max(bx, cx));
        int minY = Mathf.Min(ay, Mathf.Min(by, cy)), maxY = Mathf.Max(ay, Mathf.Max(by, cy));
        for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
            {
                float d1 = (x - bx) * (ay - by) - (ax - bx) * (y - by);
                float d2 = (x - cx) * (by - cy) - (bx - cx) * (y - cy);
                float d3 = (x - ax) * (cy - ay) - (cx - ax) * (y - ay);
                bool neg = d1 < 0 || d2 < 0 || d3 < 0, pos = d1 > 0 || d2 > 0 || d3 > 0;
                if (!(neg && pos)) P(x, y, c);
            }
    }

    public Pix Copia()
    {
        Pix o = new Pix(w, h);
        System.Array.Copy(d, o.d, d.Length);
        return o;
    }

    bool Op(int x, int y) { return Dentro(x, y) && d[y * w + x].a > 0; }

    // Contorno escuro de 1 pixel em volta do desenho
    public Pix Contorno(Color32 c)
    {
        Pix o = Copia();
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                if (d[y * w + x].a == 0 && (Op(x - 1, y) || Op(x + 1, y) || Op(x, y - 1) || Op(x, y + 1)))
                    o.d[y * w + x] = c;
        return o;
    }

    public Pix Branco()
    {
        Pix o = Copia();
        for (int i = 0; i < o.d.Length; i++)
            if (o.d[i].a > 0) o.d[i] = new Color32(255, 255, 255, 255);
        return o;
    }

    // Gira 90 graus para a esquerda (usado no herói caído)
    public Pix GiraEsquerda()
    {
        Pix o = new Pix(h, w);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                o.d[(w - 1 - x) * o.w + y] = d[y * w + x];
        return o;
    }

    // Encosta o desenho na linha de baixo
    public Pix EncostarEmbaixo()
    {
        int ultimo = -1;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                if (d[y * w + x].a > 0) ultimo = y;
        int desloca = (h - 1) - ultimo;
        Pix o = new Pix(w, h);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int ny = y + desloca;
                if (ny >= 0 && ny < h) o.d[ny * w + x] = d[y * w + x];
            }
        return o;
    }

    public Texture2D ParaTextura()
    {
        Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        t.filterMode = FilterMode.Point;
        t.wrapMode = TextureWrapMode.Clamp;
        Color32[] linhas = new Color32[d.Length];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                linhas[(h - 1 - y) * w + x] = d[y * w + x];
        t.SetPixels32(linhas);
        t.Apply(false, false);
        return t;
    }

    public Sprite ParaSprite(float ppu, float pivotX, float pivotY)
    {
        Texture2D t = ParaTextura();
        return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(pivotX, pivotY), ppu, 0, SpriteMeshType.FullRect);
    }

    // Desenha a partir de texto ('.' = vazio) e uma paleta de letras
    public static Pix Texto(string[] linhas, Dictionary<char, Color32> paleta)
    {
        int w = 0;
        foreach (string l in linhas) w = Mathf.Max(w, l.Length);
        Pix p = new Pix(w, linhas.Length);
        for (int y = 0; y < linhas.Length; y++)
            for (int x = 0; x < linhas[y].Length; x++)
            {
                char ch = linhas[y][x];
                if (paleta.ContainsKey(ch)) p.P(x, y, paleta[ch]);
            }
        return p;
    }
}


// Toda a arte do jogo, desenhada por código (nenhuma imagem externa).
public static class PixelArt
{
    public const float PPU = 16f;   // 16 pixels = 1 unidade do mundo

    // ----- herói (24x24, pivô nos pés)
    public static Sprite[] heroiParado, heroiCorrer, heroiAtaque, heroiMorte;
    public static Sprite heroiPulo, heroiQueda, heroiDano, heroiDash;
    // ----- inimigos
    public static Sprite[] slime, morcego, esqueleto, esqueletoAtaque;
    public static Sprite slimeMorto;
    // ----- efeitos
    public static Sprite[] golpe;
    public static Sprite pixel, brilho;
    // ----- cenário da masmorra
    public static Sprite[] paredeCorpo, paredeTopo;
    public static Sprite fundoTijolo, espinhos, portaFechada, portaAberta, chave, coracao;
    public static Sprite[] tocha;
    // ----- interface
    public static Texture2D uiCoracao, uiCoracaoVazio, uiChave, uiDash, tituloFundo;

    static Dictionary<Sprite, Sprite> brancos = new Dictionary<Sprite, Sprite>();
    static bool pronto = false;

    static Color32 H(int rgb) { return new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 255); }

    // Paleta
    static readonly Color32 Contorn = new Color32(26, 16, 32, 255);
    static readonly Color32 Aco = new Color32(0xc4, 0xd0, 0xe4, 255), AcoClaro = new Color32(0xee, 0xf4, 0xff, 255), AcoEscuro = new Color32(0x7a, 0x86, 0x9e, 255);
    static readonly Color32 Tunica = new Color32(0x3b, 0x6f, 0xd4, 255), TunicaClara = new Color32(0x6a, 0x9a, 0xf0, 255), Calca = new Color32(0x2a, 0x3a, 0x7a, 255);
    static readonly Color32 Pele = new Color32(0xf2, 0xc9, 0xa0, 255), Penacho = new Color32(0xd9, 0x40, 0x40, 255), Capa = new Color32(0xb0, 0x30, 0x30, 255), CapaClara = new Color32(0xd8, 0x50, 0x50, 255);
    static readonly Color32 Bota = new Color32(0x5a, 0x3a, 0x28, 255), Cinto = new Color32(0x8b, 0x5a, 0x2b, 255), Ouro = new Color32(0xf2, 0xc7, 0x44, 255), Preto = new Color32(0x10, 0x0c, 0x18, 255);

    static Sprite S(Pix p, float px = 0.5f, float py = 0f)
    {
        Pix final = p.Contorno(Contorn);
        Sprite s = final.ParaSprite(PPU, px, py);
        brancos[s] = final.Branco().ParaSprite(PPU, px, py);
        return s;
    }

    // Versão toda branca de um sprite (piscada quando leva dano)
    public static Sprite Branco(Sprite s)
    {
        Sprite b;
        return (s != null && brancos.TryGetValue(s, out b)) ? b : s;
    }

    public static void Iniciar()
    {
        if (pronto) return;
        pronto = true;

        ConstruirHeroi();
        ConstruirInimigos();
        ConstruirEfeitos();
        ConstruirCenario();
        ConstruirInterface();
    }

    // ============================================================ HERÓI
    static void Perna(Pix p, int x0, int lift, int top, Color32 calca)
    {
        int bottom = 23 - lift;
        if (bottom < top + 1) bottom = top + 1;
        p.R(x0, top, 3, bottom - top + 1, calca);
        p.R(x0, bottom - 1, 3, 2, Bota);
        p.P(x0 + 3, bottom, Bota);
    }

    static void Espada(Pix p, int pose, int bob)
    {
        switch (pose)
        {
            case 0:     // em pé, ponta para cima
                p.R(16, 12 + bob, 4, 1, Ouro);
                p.R(18, 3 + bob, 1, 9, AcoClaro);
                p.R(19, 3 + bob, 1, 9, AcoEscuro);
                p.P(18, 2 + bob, AcoClaro);
                break;
            case 1:     // preparando o golpe (para trás/cima)
                p.Linha(16, 11 + bob, 11, 0 + bob, AcoClaro);
                p.Linha(17, 11 + bob, 12, 0 + bob, AcoEscuro);
                p.R(15, 11 + bob, 3, 1, Ouro);
                break;
            case 2:     // golpe para a frente
                p.R(17, 10 + bob, 1, 5, Ouro);
                p.R(18, 12 + bob, 6, 1, AcoClaro);
                p.R(18, 13 + bob, 6, 1, AcoEscuro);
                break;
            case 3:     // depois do golpe (para baixo)
                p.Linha(17, 13 + bob, 22, 19 + bob, AcoClaro);
                p.Linha(17, 14 + bob, 22, 20 + bob, AcoEscuro);
                p.R(16, 12 + bob, 3, 1, Ouro);
                break;
        }
    }

    static Pix Heroi(int bob, int ax, int al, int bx, int bl, int espada, int capa, bool olhoX = false)
    {
        Pix p = new Pix(24, 24);

        // capa (atrás de tudo)
        int ct = 10 + bob;
        p.R(6, ct, 3, 9, Capa);
        p.R(4 - capa, ct + 4, 2 + capa, 6, Capa);
        p.R(6, ct, 1, 9, CapaClara);

        // pernas
        Perna(p, 8 + bx, bl, 17 + bob, Calca);
        Perna(p, 12 + ax, al, 17 + bob, new Color32(0x34, 0x48, 0x90, 255));

        // tronco
        p.R(8, 10 + bob, 8, 7, Tunica);
        p.R(8, 10 + bob, 8, 1, TunicaClara);
        p.R(8, 15 + bob, 8, 1, Cinto);
        p.R(11, 15 + bob, 2, 1, Ouro);
        p.R(7, 10 + bob, 3, 2, Aco);       // ombreira

        // cabeça com elmo
        p.R(8, 3 + bob, 8, 7, Pele);
        p.R(8, 3 + bob, 8, 3, Aco);
        p.R(8, 3 + bob, 2, 7, Aco);
        p.R(9, 3 + bob, 5, 1, AcoClaro);
        p.R(13, 4 + bob, 1, 3, AcoEscuro);
        p.R(8, 9 + bob, 8, 1, AcoEscuro);
        if (olhoX)
        {
            p.P(14, 6 + bob, Preto); p.P(15, 8 + bob, Preto); p.P(15, 6 + bob, Preto); p.P(14, 8 + bob, Preto);
        }
        else
        {
            p.P(14, 7 + bob, Preto); p.P(14, 6 + bob, Preto);
        }
        // penacho
        p.R(9, 1 + bob, 4, 2, Penacho);
        p.R(7, 2 + bob, 3, 2, Penacho);
        p.P(6, 4 + bob, Penacho);

        // braço da frente
        p.R(14, 11 + bob, 3, 3, Tunica);
        p.R(16, 12 + bob, 2, 2, Pele);

        Espada(p, espada, bob);
        return p;
    }

    static void ConstruirHeroi()
    {
        heroiParado = new Sprite[] { S(Heroi(0, 0, 0, 0, 0, 0, 0)), S(Heroi(1, 0, 0, 0, 0, 0, 1)) };

        heroiCorrer = new Sprite[6];
        for (int f = 0; f < 6; f++)
        {
            float ph = f * Mathf.PI * 2f / 6f;
            int ax = Mathf.RoundToInt(3f * Mathf.Sin(ph));
            int al = Mathf.Max(0, Mathf.RoundToInt(2f * Mathf.Cos(ph)));
            int bl = Mathf.Max(0, Mathf.RoundToInt(-2f * Mathf.Cos(ph)));
            heroiCorrer[f] = S(Heroi(f % 3 == 0 ? 1 : 0, ax, al, -ax, bl, 0, f % 3));
        }

        heroiPulo = S(Heroi(0, -1, 3, 2, 2, 0, 2));
        heroiQueda = S(Heroi(0, -2, 0, 3, 0, 0, 2));
        heroiAtaque = new Sprite[]
        {
            S(Heroi(0, 2, 0, -2, 0, 1, 1)),
            S(Heroi(0, 3, 0, -3, 0, 2, 0)),
            S(Heroi(0, 2, 0, -2, 0, 3, 0)),
        };
        heroiDano = S(Heroi(1, -1, 0, 2, 0, 3, 2, true));

        // dash: corpo inclinado com riscos de velocidade
        Pix dash = Heroi(2, 3, 0, -3, 0, 2, 2);
        Color32 vento = new Color32(255, 255, 255, 200);
        dash.R(0, 9, 4, 1, vento); dash.R(1, 13, 5, 1, vento); dash.R(0, 17, 4, 1, vento);
        heroiDash = S(dash);

        Pix cai = Heroi(1, -1, 0, 2, 0, 3, 2, true);
        Pix ajoelha = Heroi(6, 0, 0, 0, 0, 3, 2, true);
        heroiMorte = new Sprite[] { heroiDano, S(ajoelha), S(cai.GiraEsquerda().EncostarEmbaixo()) };
    }

    // ============================================================ INIMIGOS
    static void ConstruirInimigos()
    {
        // slime verde: 4 quadros de "pulinho"
        float[,] forma = { { 7f, 4.5f }, { 6.5f, 5.5f }, { 5.5f, 6.8f }, { 6.5f, 5.2f } };
        slime = new Sprite[4];
        for (int f = 0; f < 4; f++)
        {
            Pix p = new Pix(16, 12);
            float rx = forma[f, 0], ry = forma[f, 1], cy = 12f - ry;
            p.Elipse(8f, cy, rx, ry, H(0x5ad25a));
            p.Elipse(8f, cy + 1.2f, rx - 1f, ry - 1.2f, H(0x46b84a));
            p.R(3, 11, 10, 1, H(0x2d7a34));                                    // sombra de baixo
            p.R(4, Mathf.Max(1, (int)(cy - ry * 0.6f)), 3, 1, H(0xb6f5a8));    // brilho
            int ey = Mathf.RoundToInt(cy);
            p.R(5, ey, 2, 2, Color.white); p.R(9, ey, 2, 2, Color.white);
            p.P(6, ey + 1, Preto); p.P(10, ey + 1, Preto);
            slime[f] = S(p);
        }
        Pix splat = new Pix(16, 12);
        splat.R(1, 9, 14, 2, H(0x5ad25a)); splat.R(3, 8, 10, 1, H(0x5ad25a)); splat.R(0, 11, 16, 1, H(0x2d7a34));
        slimeMorto = S(splat);

        // morcego: asas batendo
        morcego = new Sprite[4];
        int[] asa = { 0, 1, 2, 1 };
        for (int f = 0; f < 4; f++)
        {
            Pix p = new Pix(18, 12);
            Color32 corpo = H(0x6b3f8f), asaC = H(0x4a2a66), claro = H(0x9a66c0);
            int cx = 9;
            if (asa[f] == 0) { p.Tri(cx, 6, 0, 0, 6, 7, asaC); p.Tri(cx, 6, 17, 0, 12, 7, asaC); }
            else if (asa[f] == 1) { p.Tri(cx, 6, 0, 5, 6, 8, asaC); p.Tri(cx, 6, 17, 5, 12, 8, asaC); }
            else { p.Tri(cx, 6, 1, 11, 6, 7, asaC); p.Tri(cx, 6, 16, 11, 12, 7, asaC); }
            p.Elipse(cx, 6.5f, 3f, 3.2f, corpo);
            p.P(cx - 2, 3, corpo); p.P(cx + 1, 3, corpo); p.P(cx - 2, 2, corpo); p.P(cx + 1, 2, corpo);
            p.P(cx - 1, 6, H(0xff3030)); p.P(cx + 1, 6, H(0xff3030));
            p.P(cx - 1, 8, Color.white); p.P(cx + 1, 8, Color.white);
            p.P(cx - 1, 4, claro);
            morcego[f] = S(p, 0.5f, 0.5f);
        }

        // esqueleto (22x24): 4 quadros andando + 3 de ataque
        esqueleto = new Sprite[4];
        int[] dxA = { -2, -1, 2, 1 }, lift = { 0, 1, 0, 1 };
        for (int f = 0; f < 4; f++)
            esqueleto[f] = S(Esqueleto(dxA[f], lift[f], -dxA[f], 1 - lift[f], 0), 8.5f / 22f, 0f);
        esqueletoAtaque = new Sprite[]
        {
            S(Esqueleto(0, 0, 0, 0, 1), 8.5f / 22f, 0f),
            S(Esqueleto(1, 0, -1, 0, 2), 8.5f / 22f, 0f),
            S(Esqueleto(0, 0, 0, 0, 3), 8.5f / 22f, 0f),
        };
    }

    static Pix Esqueleto(int ax, int al, int bx, int bl, int espada)
    {
        Pix p = new Pix(22, 24);
        Color32 osso = H(0xe8e4d0), sombra = H(0xb0aa94), ferro = H(0x9aa0a8), ferrugem = H(0x7a5a3a);

        // pernas
        p.R(6 + bx, 18, 2, 5 - bl, osso); p.R(6 + bx, 23 - bl, 3, 1, sombra);
        p.R(9 + ax, 18, 2, 5 - al, osso); p.R(9 + ax, 23 - al, 3, 1, sombra);
        // bacia, coluna e costelas
        p.R(6, 16, 5, 2, osso);
        p.R(7, 9, 2, 7, sombra);
        p.R(5, 10, 6, 1, osso); p.R(5, 12, 6, 1, osso); p.R(6, 14, 4, 1, osso);
        // braço de trás
        p.R(4, 10, 1, 5, sombra);
        // crânio
        p.R(5, 1, 7, 6, osso);
        p.R(6, 7, 5, 2, osso);
        p.R(5, 1, 7, 1, H(0xfffbe8));
        p.R(8, 3, 2, 2, Preto); p.R(6, 3, 1, 2, Preto);
        p.P(7, 8, Preto); p.P(9, 8, Preto);
        // braço da frente + espada enferrujada
        p.R(11, 10, 3, 1, osso);
        switch (espada)
        {
            case 0: p.R(14, 11, 1, 8, ferro); p.R(13, 10, 3, 1, ferrugem); break;
            case 1: p.Linha(14, 10, 17, 1, ferro); p.Linha(15, 10, 18, 1, sombra); p.R(13, 9, 3, 1, ferrugem); break;
            case 2: p.R(14, 11, 8, 1, ferro); p.R(14, 12, 8, 1, sombra); p.R(13, 9, 1, 5, ferrugem); break;
            default: p.Linha(14, 11, 19, 19, ferro); p.R(13, 10, 3, 1, ferrugem); break;
        }
        return p;
    }

    // ============================================================ EFEITOS
    static void ConstruirEfeitos()
    {
        // golpe da espada: um arco que gira (3 quadros), centro no peito do herói
        golpe = new Sprite[3];
        float[] fim = { -20f, 15f, 50f };
        for (int f = 0; f < 3; f++)
        {
            Pix p = new Pix(48, 48);
            float cx = 8f, cy = 24f;
            for (int y = 0; y < 48; y++)
                for (int x = 0; x < 48; x++)
                {
                    float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float ang = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;   // y para baixo: ângulo positivo = baixo
                    float ini = fim[f] - 85f;
                    if (ang < ini || ang > fim[f]) continue;
                    float t = (ang - ini) / 85f;                       // 0 = cauda, 1 = ponta
                    float rMax = 26f - (1f - t) * 6f;                  // a cauda é mais fina
                    if (r < 13f + (1f - t) * 4f || r > rMax) continue;
                    Color32 c = t > 0.75f ? new Color32(255, 255, 255, 255) : t > 0.4f ? new Color32(190, 225, 255, 235) : new Color32(120, 170, 255, 180);
                    p.P(x, y, c);
                }
            golpe[f] = p.ParaSprite(PPU, 8f / 48f, 0.5f);
        }

        Pix px = new Pix(2, 2); px.R(0, 0, 2, 2, Color.white);
        pixel = px.ParaSprite(PPU, 0.5f, 0.5f);

        // brilho redondo (luz de tocha)
        Pix g = new Pix(48, 48);
        for (int y = 0; y < 48; y++)
            for (int x = 0; x < 48; x++)
            {
                float dx = (x + 0.5f - 24f) / 24f, dy = (y + 0.5f - 24f) / 24f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                if (r < 1f)
                {
                    float a = Mathf.Pow(1f - r, 2f) * 110f;
                    g.P(x, y, new Color32(255, 170, 60, (byte)a));
                }
            }
        brilho = g.ParaSprite(PPU, 0.5f, 0.5f);
    }

    // ============================================================ CENÁRIO
    static int Hash(int a, int b, int c)
    {
        unchecked
        {
            int h = a * 73856093 ^ b * 19349663 ^ c * 83492791;
            h ^= h >> 13; h *= 0x5bd1e995; h ^= h >> 15;
            return h & 0x7fffffff;
        }
    }

    static Pix Tijolos(int seed, bool topo, bool fundo)
    {
        Pix p = new Pix(16, 16);
        int[] base_ = fundo ? new[] { 0x2e3150, 0x282b46, 0x34385a } : new[] { 0x585e7c, 0x4c5270, 0x666c8a };
        Color32 argamassa = fundo ? H(0x1a1c30) : H(0x2c2f44);
        for (int r = 0; r < 4; r++)
        {
            int y0 = r * 4;
            int off = (r % 2) * 4;
            for (int x = 0; x < 16; x++)
            {
                int bloco = ((x + 16 - off) % 16) / 8;
                Color32 c = H(base_[Hash(seed, r, bloco + (x + 16 - off >= 16 ? 2 : 0)) % 3]);
                for (int y = 0; y < 4; y++) p.P(x, y0 + y, c);
            }
            for (int x = 0; x < 16; x++) p.P(x, y0 + 3, argamassa);            // linha de baixo do tijolo
            p.R((off + 0) % 16, y0, 1, 4, argamassa);                           // juntas verticais
            p.R((off + 8) % 16, y0, 1, 4, argamassa);
        }
        // rachaduras/pontinhos
        for (int i = 0; i < 4; i++)
        {
            int x = Hash(seed, i, 9) % 16, y = Hash(seed, i, 11) % 16;
            p.P(x, y, fundo ? H(0x1f2138) : H(0x3d4260));
        }
        if (topo)
        {
            p.R(0, 0, 16, 2, H(0x8a91b4));
            p.R(0, 2, 16, 1, H(0x6c7396));
            for (int x = 0; x < 16; x++)
                if (Hash(seed, x, 5) % 3 == 0) p.P(x, 0, H(0x5f9a52));
            for (int x = 0; x < 16; x++)
                if (Hash(seed, x, 7) % 4 == 0) p.P(x, 1, H(0x4a7e42));
        }
        return p;
    }

    static void ConstruirCenario()
    {
        paredeCorpo = new Sprite[3];
        paredeTopo = new Sprite[2];
        for (int i = 0; i < 3; i++) paredeCorpo[i] = Tijolos(i + 1, false, false).ParaSprite(PPU, 0.5f, 0.5f);
        for (int i = 0; i < 2; i++) paredeTopo[i] = Tijolos(i + 11, true, false).ParaSprite(PPU, 0.5f, 0.5f);
        fundoTijolo = Tijolos(77, false, true).ParaSprite(PPU, 0.5f, 0.5f);

        // espinhos de ferro (16x8)
        Pix sp = new Pix(16, 8);
        for (int i = 0; i < 4; i++)
        {
            int x = i * 4;
            sp.Tri(x, 7, x + 3, 7, x + 1, 1, H(0xc8ced8));
            sp.Tri(x + 2, 7, x + 3, 7, x + 2, 2, H(0x8a92a0));
        }
        sp.R(0, 7, 16, 1, H(0x5a606c));
        espinhos = S(sp, 0.5f, 0f);

        // porta da masmorra (16x32)
        Pix pf = new Pix(16, 32);
        Color32 mad = H(0x7a4a28), madEsc = H(0x5a3418), ferro = H(0x3a3e48);
        pf.R(0, 3, 16, 29, mad);
        pf.R(2, 1, 12, 2, mad); pf.R(4, 0, 8, 1, mad);
        for (int x = 3; x < 16; x += 4) pf.R(x, 1, 1, 31, madEsc);
        pf.R(0, 9, 16, 2, ferro); pf.R(0, 24, 16, 2, ferro);
        pf.R(0, 3, 1, 29, madEsc); pf.R(15, 3, 1, 29, madEsc);
        pf.R(10, 16, 4, 4, Ouro); pf.P(11, 17, Preto); pf.P(12, 18, Preto);      // fechadura
        portaFechada = S(pf, 0.5f, 0f);

        Pix po = new Pix(16, 32);
        po.R(0, 3, 16, 29, H(0x08060f));
        po.R(2, 1, 12, 2, H(0x08060f)); po.R(4, 0, 8, 1, H(0x08060f));
        po.R(3, 5, 10, 26, H(0xffe9a0)); po.R(5, 3, 6, 2, H(0xffe9a0));            // luz do outro lado
        po.R(0, 3, 3, 29, mad); po.R(0, 9, 3, 2, ferro); po.R(0, 24, 3, 2, ferro);
        portaAberta = S(po, 0.5f, 0f);

        // chave (8x8)
        var cp = new Dictionary<char, Color32> { { 'X', Ouro }, { 'o', H(0xb8901c) } };
        Pix ch = Pix.Texto(new[]
        {
            "..XXX...",
            ".X...X..",
            ".X...X..",
            "..XXX...",
            "...X....",
            "...XX...",
            "...X....",
            "...XXo..",
        }, cp);
        chave = S(ch, 0.5f, 0.5f);

        // coração (pickup)
        coracao = S(Coracao(false), 0.5f, 0.5f);

        // tocha (8x16): suporte de ferro + 3 chamas diferentes
        tocha = new Sprite[3];
        for (int f = 0; f < 3; f++)
        {
            Pix t = new Pix(10, 18);
            t.R(4, 8, 2, 9, H(0x4a4e58));
            t.R(2, 8, 6, 2, H(0x6a6e7a));
            int alt = 5 + (f == 1 ? 1 : 0);
            t.Tri(5, 8 - alt - 1, 2, 8, 8, 8, H(0xff7a1a));
            t.Tri(5, 8 - alt + 1, 3, 8, 7, 8, H(0xffc83c));
            t.Tri(5, 8 - alt + 3, 4, 8, 6, 8, H(0xfff2b0));
            if (f == 2) t.P(7, 3, H(0xffc83c));
            tocha[f] = t.ParaSprite(PPU, 0.5f, 0.5f);
        }
    }

    static Pix Coracao(bool vazio)
    {
        var pal = new Dictionary<char, Color32>();
        if (!vazio) { pal['X'] = H(0xe0303c); pal['h'] = H(0xff9a9a); }
        else { pal['X'] = H(0x3a2a3a); pal['h'] = H(0x4a3a4a); }
        Pix base_ = Pix.Texto(new[]
        {
            ".XX.XX.",
            "XhXXXXX",
            "XXXXXXX",
            ".XXXXX.",
            "..XXX..",
            "...X...",
        }, pal);
        Pix p = new Pix(9, 8);
        for (int y = 0; y < base_.h; y++)
            for (int x = 0; x < base_.w; x++)
                p.P(x + 1, y + 1, base_.d[y * base_.w + x]);
        return p.Contorno(vazio ? H(0x8a7a8a) : Contorn);
    }

    // ============================================================ INTERFACE
    static void ConstruirInterface()
    {
        uiCoracao = Coracao(false).ParaTextura();
        uiCoracaoVazio = Coracao(true).ParaTextura();

        var cp = new Dictionary<char, Color32> { { 'X', Ouro }, { 'o', H(0xb8901c) } };
        Pix ch = Pix.Texto(new[]
        {
            "..XXX...",
            ".X...X..",
            ".X...X..",
            "..XXX...",
            "...X....",
            "...XX...",
            "...X....",
            "...XXo..",
        }, cp);
        Pix chOut = new Pix(10, 10);
        for (int y = 0; y < 8; y++) for (int x = 0; x < 8; x++) chOut.P(x + 1, y + 1, ch.d[y * 8 + x]);
        uiChave = chOut.Contorno(Contorn).ParaTextura();

        Pix dash = new Pix(11, 9);
        Color32 azul = H(0x6ad0ff);
        dash.Linha(1, 1, 4, 4, azul); dash.Linha(1, 7, 4, 4, azul);
        dash.Linha(5, 1, 8, 4, azul); dash.Linha(5, 7, 8, 4, azul);
        dash.Linha(2, 1, 5, 4, azul); dash.Linha(2, 7, 5, 4, azul);
        dash.Linha(6, 1, 9, 4, azul); dash.Linha(6, 7, 9, 4, azul);
        uiDash = dash.Contorno(Contorn).ParaTextura();

        tituloFundo = FundoDoTitulo().ParaTextura();
    }

    // Ilustração da tela inicial (160x90): noite, lua, castelo e morcegos
    static Pix FundoDoTitulo()
    {
        Pix p = new Pix(160, 90);
        for (int y = 0; y < 90; y++)
        {
            float t = y / 89f;
            Color32 c = new Color32((byte)(10 + 50 * t), (byte)(8 + 24 * t), (byte)(36 + 60 * t), 255);
            p.R(0, y, 160, 1, c);
        }
        for (int i = 0; i < 70; i++)
        {
            int x = Hash(i, 1, 1) % 160, y = Hash(i, 2, 2) % 55;
            p.P(x, y, Hash(i, 3, 3) % 4 == 0 ? H(0xffffff) : H(0x9aa4d8));
        }
        // lua
        p.Elipse(122, 22, 11, 11, H(0xf6efc8));
        p.Elipse(125, 20, 9, 9, H(0xfffbe0));
        p.R(118, 25, 3, 2, H(0xd8d0a0)); p.R(126, 17, 2, 2, H(0xd8d0a0)); p.R(121, 14, 2, 1, H(0xd8d0a0));
        // montanhas
        for (int x = 0; x < 160; x++)
        {
            int topo = 62 + (int)(8 * Mathf.Sin(x * 0.07f) + 5 * Mathf.Sin(x * 0.19f + 1f));
            p.R(x, topo, 1, 90 - topo, H(0x1c1838));
        }
        // castelo
        Color32 pedra = H(0x120e26), janela = H(0xffd25a);
        p.R(92, 40, 36, 40, pedra);
        p.R(84, 30, 12, 50, pedra); p.R(124, 34, 12, 46, pedra);
        p.R(102, 24, 14, 20, pedra);
        for (int x = 84; x < 96; x += 4) p.R(x, 27, 2, 3, pedra);
        for (int x = 124; x < 136; x += 4) p.R(x, 31, 2, 3, pedra);
        for (int x = 92; x < 128; x += 4) p.R(x, 37, 2, 3, pedra);
        p.Tri(102, 24, 116, 24, 109, 10, pedra);
        p.R(108, 4, 1, 7, pedra); p.R(109, 4, 4, 2, H(0xd94040));
        int[,] jan = { { 88, 40 }, { 129, 44 }, { 107, 32 }, { 100, 54 }, { 114, 54 }, { 88, 58 }, { 129, 60 } };
        for (int i = 0; i < jan.GetLength(0); i++) p.R(jan[i, 0], jan[i, 1], 2, 4, janela);
        p.R(106, 66, 6, 14, H(0x3a2a14)); p.R(107, 67, 4, 13, H(0xffb83c));      // portão iluminado
        // chão com grama
        for (int x = 0; x < 160; x++)
        {
            int topo = 76 + (int)(2 * Mathf.Sin(x * 0.2f));
            p.R(x, topo, 1, 90 - topo, H(0x0c0a1a));
            if (Hash(x, 4, 4) % 3 == 0) p.P(x, topo - 1, H(0x0c0a1a));
        }
        // morcegos
        int[,] mo = { { 30, 20 }, { 48, 30 }, { 70, 14 }, { 20, 40 } };
        for (int i = 0; i < mo.GetLength(0); i++)
        {
            int x = mo[i, 0], y = mo[i, 1];
            p.Tri(x, y, x - 5, y - 2, x - 2, y + 1, H(0x05030c));
            p.Tri(x, y, x + 5, y - 2, x + 2, y + 1, H(0x05030c));
            p.R(x - 1, y - 1, 2, 3, H(0x05030c));
        }
        return p;
    }
}
