using UnityEngine;
using UnityEngine.SceneManagement;

public class vitoria : MonoBehaviour
{
    // 2D
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            SceneManager.LoadScene("vitoria");
        }
    }
}