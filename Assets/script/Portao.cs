using UnityEngine;
using UnityEngine.SceneManagement;

public class Portao : MonoBehaviour
{
    // 2D: quando o Player entra no portal, a fase termina
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (GameManager.I != null)
        {
            GameManager.I.FaseCompleta();       // mostra a tela de "Fase completa" e vai para a próxima cena
            return;
        }

        // Sem GameManager: vai direto para a próxima cena da lista (ou volta à primeira)
        int proxima = SceneManager.GetActiveScene().buildIndex + 1;
        if (proxima >= SceneManager.sceneCountInBuildSettings) proxima = 0;
        SceneManager.LoadScene(proxima);
    }
}
