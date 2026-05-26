using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuPrincipal : MonoBehaviour
{
    public void IniciarJogo()
    {
        SceneManager.LoadScene("complete_track_demo");
    }

    public void SairJogo()
    {
        Application.Quit();
        Debug.Log("Saindo...");
    }
}