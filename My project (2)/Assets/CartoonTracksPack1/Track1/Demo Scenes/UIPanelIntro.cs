using UnityEngine;

// Anima a entrada de um painel: cresce de uma escala menor ate 1 e
// faz fade-in via CanvasGroup. Toda vez que o objeto e ativado.
// Tempo nao-escalado: funciona com o jogo pausado (timeScale 0).
[RequireComponent(typeof(CanvasGroup))]
public class UIPanelIntro : MonoBehaviour
{
    public float duracao = 0.28f;
    public float escalaInicial = 0.85f;

    private CanvasGroup cg;
    private RectTransform rt;
    private float t;

    void Awake()
    {
        cg = GetComponent<CanvasGroup>();
        rt = GetComponent<RectTransform>();
    }

    void OnEnable()
    {
        if (cg == null) cg = GetComponent<CanvasGroup>();
        if (rt == null) rt = GetComponent<RectTransform>();
        t = 0f;
        if (cg != null) cg.alpha = 0f;
        if (rt != null) rt.localScale = Vector3.one * escalaInicial;
    }

    void Update()
    {
        if (t >= duracao) return;
        t += Time.unscaledDeltaTime;
        float p = Mathf.Clamp01(t / duracao);
        float e = 1f - Mathf.Pow(1f - p, 3f); // ease-out cubico
        if (cg != null) cg.alpha = e;
        if (rt != null) rt.localScale = Vector3.one * Mathf.Lerp(escalaInicial, 1f, e);
    }
}
