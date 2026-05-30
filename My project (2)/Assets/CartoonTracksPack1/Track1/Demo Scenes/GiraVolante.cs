using UnityEngine;
using static LogitechGSDK;

public class GiraVolante : MonoBehaviour
{
    public float anguloMaximo = 450f;
    public float velocidadeRotacao = 6f;

    private Quaternion rotacaoInicial;
    private Vector3 posicaoInicial;

    void Start()
    {
        bool ok = LogitechGSDK.LogiSteeringInitialize(false);
        Debug.Log("Logitech Init: " + ok);

        rotacaoInicial = transform.localRotation;
        posicaoInicial = transform.localPosition;
    }

    void Update()
    {
        float entrada = 0f;

        // Verifica se há um volante Logitech conectado
        if (LogitechGSDK.LogiUpdate() &&
            LogitechGSDK.LogiIsConnected(0))
        {
            DIJOYSTATE2ENGINES estado =
                LogitechGSDK.LogiGetStateCSharp(0);

            // lX varia de -32768 a 32767
            entrada = estado.lX / 32767f;
        }
        else
        {
            // Fallback para teclado
            entrada = Input.GetAxis("Horizontal");
        }

        float angulo = entrada * anguloMaximo;

        Quaternion rotacaoAlvo =
            rotacaoInicial * Quaternion.AngleAxis(angulo, Vector3.forward);

        transform.localRotation = Quaternion.Lerp(
            transform.localRotation,
            rotacaoAlvo,
            Time.deltaTime * velocidadeRotacao
        );

        transform.localPosition = posicaoInicial;
    }

    void OnApplicationQuit()
    {
        LogitechGSDK.LogiSteeringShutdown();
    }
}