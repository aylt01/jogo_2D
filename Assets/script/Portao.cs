using UnityEngine;
using UnityEngine.SceneManagement;

// Classe responsável por controlar a passagem de uma cena para outra
public class Portao : MonoBehaviour
{
    // Verifica quando algum objeto entra no portal
    // 2D
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Verifica se o objeto que encostou tem a tag "Player"
        if (other.CompareTag("Player"))
        {
            // Leva o jogador para a cena chamada Fase2
            SceneManager.LoadScene("Fase2");
        }
    }
}