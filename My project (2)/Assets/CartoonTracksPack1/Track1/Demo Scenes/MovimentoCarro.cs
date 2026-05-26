using UnityEngine;

public class MovimentoCarro : MonoBehaviour
{
    public float velocidadeMaxima = 80f;
    public float aceleracaoForça = 30f;
    public float freioForca = 50f;
    public float resistencia = 20f; // desacelera sozinho quando solta
    public float velocidadeCurva = 100f;

    private Rigidbody rb;
    private float velocidadeAtual = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
    }

    void FixedUpdate()
    {
        float aceleracao = Input.GetAxis("Vertical");
        float direcao = Input.GetAxis("Horizontal");

        if (aceleracao > 0)
        {
            // Acelera progressivamente
            velocidadeAtual += aceleracaoForça * Time.fixedDeltaTime;
        }
        else if (aceleracao < 0)
        {
            // Freia
            velocidadeAtual -= freioForca * Time.fixedDeltaTime;
        }
        else
        {
            // Resistência ao soltar o acelerador
            velocidadeAtual -= resistencia * Time.fixedDeltaTime;
        }

        // Limita entre 0 e velocidade máxima
        velocidadeAtual = Mathf.Clamp(velocidadeAtual, 0f, velocidadeMaxima);

        // Aplica movimento
        Vector3 movimento = transform.forward * velocidadeAtual;
        rb.linearVelocity = new Vector3(movimento.x, rb.linearVelocity.y, movimento.z);

        // Curva só se estiver andando
        if (velocidadeAtual > 0.1f)
        {
            float rotacao = direcao * velocidadeCurva * Time.fixedDeltaTime;
            rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, rotacao, 0f));
        }
    }
}