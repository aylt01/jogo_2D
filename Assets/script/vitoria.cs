using UnityEngine;
using UnityEngine.SceneManagement;

public class vitoria : MonoBehaviour
{
    // vai reconhcer um objeto 2d
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) // vai comparar a tag do objeto
        {
            SceneManager.LoadScene("vitoria"); // vai carregar a scene vitoria
        }
    }
}