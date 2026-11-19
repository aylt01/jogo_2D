using UnityEngine;

// Classe responsável por fazer a câmera seguir o jogador
public class CameraFollow : MonoBehaviour
{
    // Objeto que a câmera vai seguir
    public Transform alvo;

    // Distância entre a câmera e o jogador
    public Vector3 distancia = new Vector3(0f, 0f, -10f);

    // Define se a câmera vai se movimentar suavemente
    public bool movimentoSuave = true;

    // Velocidade em que a câmera vai se movimentar
    public float velocidade = 5f;


    // Executa depois que todos os Updates terminam
    void LateUpdate()
    {
        // Calcula a posição que a câmera deve ficar
        Vector3 posicaoDesejada = alvo.position + distancia;

        // Verifica se o movimento suave está ativado
        if (movimentoSuave)
        {
            // Faz a câmera se aproximar da posição desejada suavemente
            transform.position = Vector3.Lerp(transform.position, posicaoDesejada, velocidade * Time.deltaTime);

        }

        // Caso o movimento suave esteja desativado
        else
        {
            // Coloca a câmera diretamente na posição desejada
            transform.position = posicaoDesejada;
        }
    }

}