using UnityEngine;

// Faz um elemento de UI "pulsar" (escala sobe e desce suavemente).
// Usa tempo nao-escalado para funcionar mesmo com o jogo pausado.
public class UIPulse : MonoBehaviour
{
    public float velocidade = 2f;
    public float intensidade = 0.06f;

    private RectTransform rt;
    private Vector3 baseScale = Vector3.one;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        if (rt != null) baseScale = rt.localScale;
    }

    void Update()
    {
        if (rt == null) return;
        float f = 1f + Mathf.Sin(Time.unscaledTime * velocidade) * intensidade;
        rt.localScale = baseScale * f;
    }
}
