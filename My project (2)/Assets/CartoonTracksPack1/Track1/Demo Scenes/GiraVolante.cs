using UnityEngine;

public class GiraVolante : MonoBehaviour
{
    public float anguloMaximo = 450f;
    public float velocidadeRotacao = 6f;

    private Quaternion rotacaoInicial;
    private Vector3 posicaoInicial;

    void Start()
    {
        rotacaoInicial = transform.localRotation;
        posicaoInicial = transform.localPosition;
    }

    void Update()
    {
        float entrada = Input.GetAxis("Horizontal");
        float angulo = entrada * anguloMaximo;

        Quaternion rotacaoAlvo = rotacaoInicial * Quaternion.AngleAxis(angulo, Vector3.forward);

        transform.localRotation = Quaternion.Lerp(
            transform.localRotation,
            rotacaoAlvo,
            Time.deltaTime * velocidadeRotacao
        );

        transform.localPosition = posicaoInicial;
    }
}