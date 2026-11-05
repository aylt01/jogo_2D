using UnityEngine;

// Tudo que pode ser atingido pelo ataque do player (inimigos).
public interface ILevaDano
{
    bool Vivo { get; }
    void LevarDano(int dano, Vector2 origem);
}
