using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MovimentoCarro : MonoBehaviour
{
    [Header("Movimento")]
    public float velocidadeMaxima = 80f;
    public float aceleracaoForca = 30f;
    public float freioForca = 50f;
    public float resistencia = 20f;
    public float velocidadeCurva = 100f;

    [Header("Alinhamento do carro")]
    public float velocidadeAlinhamento = 15f; 
    public float distanciaDoChao = 2.0f;
    public LayerMask camadaDaPista;          

    [Header("Audio")]
    [Range(0f, 1f)] public float volumeMotor = 0.9f;
    [Range(0f, 1f)] public float volumeDesaceleracao = 0.45f;
    [Range(0f, 1f)] public float volumeImpacto = 0.5f;
    public float impactoMinimo = 4f;
    public float impactoMaximo = 20f;
    public float respostaAudio = 6f;

    private const string LowClipPath = "FurbGpAudio/Sfx/VnsCar/1000a";
    private const string MidClipPath = "FurbGpAudio/Sfx/VnsCar/3000a";
    private const string HighClipPath = "FurbGpAudio/Sfx/VnsCar/5000a";
    private const string DecelClipPath = "FurbGpAudio/Sfx/VnsCar/3000d";
    private const string ImpactClipPath = "FurbGpAudio/Sfx/impact_cc0";

    private Rigidbody rb;
    private float velocidadeAtual;
    private AudioSource lowSource;
    private AudioSource midSource;
    private AudioSource highSource;
    private AudioSource decelSource;
    private AudioSource impactSource;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    
        rb.freezeRotation = true; 
        
        CriarAudio();
    }

    void Update()
    {
        AtualizarAudio();
    }

    void FixedUpdate()
    {
        float aceleracao;
        float freio;
        float direcao;

        if (Logitech.IsLigado())
        {
            aceleracao = Logitech.Acelerador();
            freio = Logitech.Embreagem(); // embreagem faz papel de freio
        }
        else
        {
            float vertical = Input.GetAxis("Vertical");

            aceleracao = Mathf.Max(0f, vertical);
            freio = Mathf.Max(0f, -vertical);
            //Debug.Log("Freio: " + freio);
        }
            direcao = Input.GetAxis("Horizontal");

        if (aceleracao > 0.2f)
        {
            //Debug.Log("ACELERANDO");
            velocidadeAtual += aceleracao * aceleracaoForca * Time.fixedDeltaTime;
        }
        else if (freio > 0.2f)
        {
            //Debug.Log("freio: "+ freio);
            velocidadeAtual -= freio * freioForca * Time.fixedDeltaTime;
        }
        else
        {
            //Debug.Log("SOLTO");
            velocidadeAtual -= resistencia * Time.fixedDeltaTime;
        }

        velocidadeAtual = Mathf.Clamp(velocidadeAtual, 0f, velocidadeMaxima);

        if (velocidadeAtual > 0.1f)
        {
            float rotacao = direcao * velocidadeCurva * Time.fixedDeltaTime;
            transform.Rotate(transform.up, rotacao, Space.World);
        }

        AlinharCarro();

        rb.linearVelocity = transform.forward * velocidadeAtual;
    }

    void AlinharCarro()
    {
        RaycastHit hit;

        if (Physics.Raycast(transform.position, -transform.up, out hit, distanciaDoChao, camadaDaPista))
        {
            Quaternion rotacaoAlvo = Quaternion.FromToRotation(transform.up, hit.normal) * transform.rotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, rotacaoAlvo, velocidadeAlinhamento * Time.fixedDeltaTime);
        }
        else
        {
            Quaternion rotacaoReta = Quaternion.Euler(0, transform.eulerAngles.y, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotacaoReta, 2f * Time.fixedDeltaTime);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (impactSource == null || impactSource.clip == null) return;

        float intensidade = collision.relativeVelocity.magnitude;
        if (intensidade < impactoMinimo) return;

        float blend = Mathf.InverseLerp(impactoMinimo, impactoMaximo, intensidade);
        float volume = Mathf.Lerp(0.1f, volumeImpacto, blend);
        float pitch = Mathf.Lerp(1.05f, 0.82f, blend);
        impactSource.pitch = pitch;
        impactSource.PlayOneShot(impactSource.clip, volume);
    }

    private void CriarAudio()
    {
        lowSource = CriarLoop("EngineLow", LowClipPath);
        midSource = CriarLoop("EngineMid", MidClipPath);
        highSource = CriarLoop("EngineHigh", HighClipPath);
        decelSource = CriarLoop("EngineDecel", DecelClipPath);
        impactSource = CriarOneShot("Impact", ImpactClipPath);

        if (lowSource == null && midSource == null && highSource == null && decelSource == null)
        {
            Debug.LogWarning("MovimentoCarro nao encontrou nenhum clip de motor em Resources/FurbGpAudio/Sfx/VnsCar.", this);
        }
    }

    private void AtualizarAudio()
    {
        float throttle;

        if (Logitech.IsLigado())
            throttle = Logitech.Acelerador();
        else
            throttle = Mathf.Clamp01(Input.GetAxis("Vertical"));

        float speedRatio = velocidadeMaxima > 0.01f ? Mathf.Clamp01(velocidadeAtual / velocidadeMaxima) : 0f;
        float blend = 1f - Mathf.Exp(-respostaAudio * Time.deltaTime);

        float lowVolume = 0f; float midVolume = 0f; float highVolume = 0f; float decelVolume = 0f;

        if (speedRatio > 0.02f || throttle > 0.05f)
        {
            lowVolume = volumeMotor * Mathf.Clamp01(1f - (speedRatio * 2f)) * (0.4f + (throttle * 0.6f));
            midVolume = volumeMotor * Mathf.SmoothStep(0f, 1f, speedRatio) * (0.35f + (throttle * 0.65f));
            highVolume = volumeMotor * 0.35f * Mathf.SmoothStep(0.45f, 1f, speedRatio) * throttle;
            decelVolume = volumeDesaceleracao * Mathf.SmoothStep(0.12f, 0.9f, speedRatio) * (1f - throttle);
        }

        AtualizarFonte(lowSource, lowVolume, Mathf.Lerp(0.93f, 0.99f, speedRatio), blend);
        AtualizarFonte(midSource, midVolume, Mathf.Lerp(0.95f, 1.02f, speedRatio), blend);
        AtualizarFonte(highSource, highVolume, Mathf.Lerp(0.98f, 1.03f, speedRatio), blend);
        AtualizarFonte(decelSource, decelVolume, Mathf.Lerp(0.92f, 0.98f, speedRatio), blend);
    }

    private AudioSource CriarLoop(string nome, string resourcePath)
    {
        AudioClip clip = Resources.Load<AudioClip>(resourcePath);
        if (clip == null) return null;

        GameObject child = new GameObject(nome);
        child.transform.SetParent(transform, false);

        AudioSource source = child.AddComponent<AudioSource>();
        source.clip = clip; source.loop = true; source.spatialBlend = 0f; source.volume = 0f;
        return source;
    }

    private AudioSource CriarOneShot(string nome, string resourcePath)
    {
        AudioClip clip = Resources.Load<AudioClip>(resourcePath);
        if (clip == null) return null;

        GameObject child = new GameObject(nome);
        child.transform.SetParent(transform, false);

        AudioSource source = child.AddComponent<AudioSource>();
        source.clip = clip; source.loop = false; source.spatialBlend = 0f; source.volume = 1f;
        return source;
    }

    private void AtualizarFonte(AudioSource source, float targetVolume, float targetPitch, float blend)
    {
        if (source == null) return;
        if (source.clip != null && !source.isPlaying) source.Play();

        source.volume = Mathf.Lerp(source.volume, Mathf.Clamp01(targetVolume), blend);
        source.pitch = Mathf.Lerp(source.pitch, targetPitch, blend);
    }
}