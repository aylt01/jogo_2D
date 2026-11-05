using System.Collections.Generic;
using UnityEngine;

// Sons "de 8 bits" gerados por código (nenhum arquivo de áudio é necessário).
public static class Som
{
    const int Taxa = 44100;
    static Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    public static float volume = 0.35f;

    delegate float Freq(float t01);

    static AudioClip Criar(string nome, float dur, Freq f, int tipo, float vol, float decaimento)
    {
        int n = (int)(Taxa * dur);
        float[] dados = new float[n];
        float fase = 0f;
        System.Random rnd = new System.Random(nome.GetHashCode());
        float ruido = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)n;
            float freq = f(t);
            fase += freq / Taxa;
            fase -= Mathf.Floor(fase);
            float v;
            if (tipo == 0) v = fase < 0.5f ? 1f : -1f;                       // quadrada
            else if (tipo == 1) v = Mathf.Abs(fase * 4f - 2f) - 1f;          // triângulo
            else
            {
                if (i % Mathf.Max(1, (int)(Taxa / Mathf.Max(200f, freq))) == 0) ruido = (float)(rnd.NextDouble() * 2 - 1);
                v = ruido;                                                    // ruído
            }
            float env = Mathf.Pow(1f - t, decaimento) * Mathf.Min(1f, i / 120f);
            dados[i] = v * env * vol;
        }
        AudioClip c = AudioClip.Create(nome, n, 1, Taxa, false);
        c.SetData(dados, 0);
        return c;
    }

    static AudioClip Pegar(string nome)
    {
        AudioClip c;
        if (clips.TryGetValue(nome, out c)) return c;

        switch (nome)
        {
            case "pulo": c = Criar(nome, 0.14f, t => 280f + 380f * t, 0, 0.5f, 1.2f); break;
            case "ataque": c = Criar(nome, 0.14f, t => 1800f - 1500f * t, 2, 0.6f, 1.5f); break;
            case "acerto": c = Criar(nome, 0.12f, t => 160f - 80f * t, 0, 0.8f, 1.5f); break;
            case "inimigo_morre": c = Criar(nome, 0.3f, t => 600f - 520f * t, 2, 0.7f, 1.2f); break;
            case "dano": c = Criar(nome, 0.28f, t => 420f - 320f * t, 0, 0.7f, 1.2f); break;
            case "morte": c = Criar(nome, 0.9f, t => 330f - 290f * t, 0, 0.6f, 1.0f); break;
            case "pegar": c = Criar(nome, 0.25f, t => t < 0.5f ? 660f : 990f, 0, 0.45f, 1.0f); break;
            case "porta": c = Criar(nome, 0.5f, t => 90f + 40f * Mathf.Sin(t * 30f), 2, 0.7f, 0.8f); break;
            case "clique": c = Criar(nome, 0.06f, t => 880f, 0, 0.4f, 1.0f); break;
            case "dash": c = Criar(nome, 0.16f, t => 900f - 600f * t, 2, 0.45f, 1.2f); break;
            case "vitoria": c = Criar(nome, 0.7f, t => t < 0.25f ? 523f : t < 0.5f ? 659f : t < 0.75f ? 784f : 1046f, 0, 0.45f, 0.8f); break;
            default: return null;
        }
        clips[nome] = c;
        return c;
    }

    public static void Tocar(string nome)
    {
        AudioClip c = Pegar(nome);
        if (c == null) return;
        Vector3 onde = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
        AudioSource.PlayClipAtPoint(c, onde, volume);
    }

    // ----------------------------------------------------------------- música
    static float Nota(int semitons) { return 55f * Mathf.Pow(2f, semitons / 12f); }

    // Um loop curto de masmorra (baixo + arpejo), em lá menor
    public static AudioClip Musica()
    {
        AudioClip c;
        if (clips.TryGetValue("musica", out c)) return c;

        float passo = 60f / 104f / 2f;      // colcheia
        int passos = 32;
        int n = (int)(Taxa * passo * passos);
        float[] dados = new float[n];

        // acordes: Am, F, C, G (raiz em semitons acima de lá 55 Hz)
        int[] raiz = { 0, -4, 3, -2 };
        int[,] arp = { { 0, 3, 7, 12 }, { 0, 4, 7, 12 }, { 0, 4, 7, 12 }, { 0, 4, 7, 12 } };
        int[] arpIdx = { 0, 1, 2, 3, 2, 1, 2, 3 };

        for (int s = 0; s < passos; s++)
        {
            int acorde = s / 8;
            int ini = (int)(s * passo * Taxa);
            int len = (int)(passo * Taxa);

            // baixo (quadrada grave) em tempos fortes
            if (s % 2 == 0)
                Misturar(dados, ini, len, Nota(raiz[acorde]), 0, 0.16f, 2.0f);

            // arpejo (triângulo) uma oitava e meia acima
            int deg = arp[acorde, arpIdx[s % 8]];
            Misturar(dados, ini, len, Nota(raiz[acorde] + deg + 24), 1, 0.13f, 1.6f);
        }

        c = AudioClip.Create("musica", n, 1, Taxa, false);
        c.SetData(dados, 0);
        clips["musica"] = c;
        return c;
    }

    static void Misturar(float[] dados, int ini, int len, float freq, int tipo, float vol, float decaimento)
    {
        float fase = 0f;
        for (int i = 0; i < len && ini + i < dados.Length; i++)
        {
            float t = i / (float)len;
            fase += freq / Taxa;
            fase -= Mathf.Floor(fase);
            float v = tipo == 0 ? (fase < 0.5f ? 1f : -1f) : (Mathf.Abs(fase * 4f - 2f) - 1f);
            dados[ini + i] += v * vol * Mathf.Pow(1f - t, decaimento) * Mathf.Min(1f, i / 80f);
        }
    }
}
