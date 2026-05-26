using UnityEngine;
using UnityEngine.SceneManagement;

public class LinhaDeLargada : MonoBehaviour
{
    public Timer timer;
    private bool largadaFeita = false;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (!largadaFeita)
            {
                largadaFeita = true;
                Debug.Log("Largada!");
            }
            else
            {
                timer.PararTimer();
            }
        }
    }
}