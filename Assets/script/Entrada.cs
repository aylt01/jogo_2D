using UnityEngine;

// Lê as teclas em um lugar só. Os testes automáticos podem "simular" as teclas ligando `simulando`.
public static class Entrada
{
    public static bool simulando = false;
    public static float eixoSim;
    public static bool pularSim, pularSegurandoSim, atacarSim, dashSim;

    // -1 (esquerda) .. 1 (direita): A/D ou setas
    public static float Eixo()
    {
        if (simulando) return eixoSim;
        return Input.GetAxisRaw("Horizontal");
    }

    public static bool Pular()
    {
        if (simulando) { bool r = pularSim; pularSim = false; return r; }
        return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);
    }

    public static bool PularSegurando()
    {
        if (simulando) return pularSegurandoSim;
        return Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
    }

    // J, Z ou botão esquerdo do mouse
    public static bool Atacar()
    {
        if (simulando) { bool r = atacarSim; atacarSim = false; return r; }
        return Input.GetKeyDown(KeyCode.J) || Input.GetKeyDown(KeyCode.Z) || Input.GetMouseButtonDown(0);
    }

    // X ou Shift
    public static bool Dash()
    {
        if (simulando) { bool r = dashSim; dashSim = false; return r; }
        return Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift);
    }

    public static bool Pausar()
    {
        return Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P);
    }

    public static bool MenuCima()
    {
        return Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);
    }

    public static bool MenuBaixo()
    {
        return Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow);
    }

    public static bool Confirmar()
    {
        return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
    }
}
