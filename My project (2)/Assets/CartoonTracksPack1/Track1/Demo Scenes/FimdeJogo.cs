using UnityEngine;
using UnityEngine.SceneManagement;

public class FimDeJogo : MonoBehaviour
{
    public void JogarNovamente()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("complete_track_demo");
    }

    public void IrParaMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MenuPrincipal");
    }
}