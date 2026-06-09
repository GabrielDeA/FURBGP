using UnityEngine;
using System;

public class VolanteLogitech : MonoBehaviour
{
    [Tooltip("Arraste o script de controle do teclado aqui")]
    public VolanteTeclado ScriptTeclado;
    public float RotacaoMaxima = 450f;


    void Start()
    {
        Logitech.Ligar();
    }

    void Update()
    {
        bool logitechLigado = Logitech.IsLigado();
        if (ScriptTeclado != null)
        {
            ScriptTeclado.enabled = !logitechLigado;
        }

        if (logitechLigado)
        {
            AtualizarRotacao(Logitech.RotacaoVolante(RotacaoMaxima));
        }
        
    }

    void OnApplicationQuit()
    {
        Logitech.Desligar();
    }


    private void AtualizarRotacao(float novaRotacao)
    {
        transform.localRotation = Quaternion.Euler(0, 0, novaRotacao);
    }

    private float Rotacao()
    {
        return transform.localRotation.z;
    }

}