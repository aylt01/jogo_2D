using UnityEngine;

public class habilidae : MonoBehaviour
{
    bool habilidade = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("bau"))
        {
            habilidade = true;
            Debug.Log("Você pegou uma habilidade! Use no X");
        }
    }

    void Update()
    {
        if (habilidade == true && Input.GetKeyDown(KeyCode.X))
        {
            Debug.Log("Usou a habilidade!");
        }
    }
}