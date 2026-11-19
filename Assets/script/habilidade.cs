using UnityEngine;

// Classe que controla o funcionamento da habilidade
public class habilidade : MonoBehaviour
{
    // Começa com a habilidade desativada
    bool Habilidade = false;

    // Verifica quando o jogador encosta em algum objeto
    void OnTriggerEnter2D(Collider2D other)
    {
        // Verifica se o objeto encostado tem a tag "bau"
        if (other.CompareTag("bau"))
        {
            // Ativa a habilidade
            Habilidade = true;

            // Mostra uma mensagem no console
            Debug.Log("Você pegou uma habilidade! Use no X");
        }
    }

    // Fica verificando os comandos do jogador
    void Update()
    {
        // Verifica se a habilidade foi pega e se a tecla X foi apertada
        if (Habilidade == true && Input.GetKeyDown(KeyCode.X))
        {
            // Mostra uma mensagem avisando que a habilidade foi usada
            Debug.Log("Usou a habilidade!");
        }
    }
}