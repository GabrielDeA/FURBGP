using System;
using UnityEngine;
public class VolanteTeclado : MonoBehaviour
{

    public float RotacaoMaxima = 135f;
    public float VelocidadeRotacao = 1f;
    public float VelocidadeRetorno = 4f;

    private float RotacaoTotal = 0f;
    private static readonly int VOLANTE_PARADO = 0;
    private static readonly int VIRANDO_PARA_ESQUERDA = 1;
    private static readonly int VIRANDO_PARA_DIREITA = -1;

    private bool isLogitech = false;

    void Start()
    {

    }

    void Update()
    {

        int direcao = ObterDirecao();
        if (direcao != VOLANTE_PARADO)
        {
            GirarVolante(direcao);
        }
        else
        {
            RetornarParaPosicaoInicial();
        }
    }

    private int ObterDirecao()
    {
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            return VIRANDO_PARA_ESQUERDA;
        }
        else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            return VIRANDO_PARA_DIREITA;
        }
        else
        {
            return VOLANTE_PARADO;
        }
    }

    private void RetornarParaPosicaoInicial()
    {
        float velocidadeRetornoReal = CalcularVelocidadeRetorno();
        float deslocamento = 0f;
        if (Rotacao() > 0) {
            deslocamento = -velocidadeRetornoReal * Time.deltaTime;
        } else if (Rotacao() < 0)
        {
            deslocamento = velocidadeRetornoReal * Time.deltaTime;
        }

        float novaRotacao = Rotacao() + deslocamento;
        AtualizarRotacao(novaRotacao);

    }

    private void GirarVolante(int direcao)
    {
        float velocidadeRotacaoReal = CalcularVelocidadeRotacao(direcao);
        float deslocamento = direcao * velocidadeRotacaoReal * Time.deltaTime;
        float novaRotacao = Rotacao() + deslocamento;
        novaRotacao = Mathf.Clamp(novaRotacao, -RotacaoMaxima, RotacaoMaxima);

        AtualizarRotacao(novaRotacao);
    }

    private float CalcularVelocidadeRotacao(int direcao) 
    {
        return VelocidadeRotacao * Math.Abs(direcao * RotacaoMaxima - Rotacao());
    }

    private float CalcularVelocidadeRetorno() 
    {
        return VelocidadeRetorno * Math.Abs(Rotacao());
    }

    private void AtualizarRotacao(float novaRotacao)
    {
        RotacaoTotal = novaRotacao;
        transform.localRotation = Quaternion.Euler(0, 0, novaRotacao);
    }

    private float Rotacao()
    {
        return RotacaoTotal;
    }
}