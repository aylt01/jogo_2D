using UnityEngine;
using UnityEngine.SceneManagement
    ;

public class MenuManager : MonoBehaviour
{
    public void StartGmae()
    {
        SceneManager.LoadScene("fase1");
    }
    public void Quitgame()
    {
        Application.Quit();
    }
        
    







}
