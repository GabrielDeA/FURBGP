using UnityEngine;
using TMPro;

public class Timer : MonoBehaviour
{
    public TextMeshProUGUI textoTempo;
    public TextMeshProUGUI textoTempoFinal;
    public GameObject painelFim;

    private float tempo = 0f;
    private bool rodando = true;

    void Update()
    {
        if (!rodando) return;

        tempo += Time.deltaTime;

        int minutos = (int)(tempo / 60);
        int segundos = (int)(tempo % 60);
        int milissegundos = (int)((tempo * 100) % 100);

        textoTempo.text = string.Format("{0:00}:{1:00}:{2:00}", minutos, segundos, milissegundos);
    }

    public void PararTimer()
    {
        rodando = false;
        Time.timeScale = 0f;

        int minutos = (int)(tempo / 60);
        int segundos = (int)(tempo % 60);
        int milissegundos = (int)((tempo * 100) % 100);

        textoTempoFinal.text = string.Format("Seu tempo: {0:00}:{1:00}:{2:00}", minutos, segundos, milissegundos);
        painelFim.SetActive(true);
    }
}