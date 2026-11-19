using UnityEngine;
using UnityEngine.SceneManagement
    ;

public class MenuManager : MonoBehaviour
{
    public void StartGmae() // classe puxada de outro lugar
    {
        SceneManager.LoadScene("fase1"); //vai carregar a scene 1 de acordo com sua função 
    }
    public void Quitgame()// classe puxada de outro script
    {
        SceneManager.LoadScene(0); // vai carregar a scens 0 
    }
        
    







}
