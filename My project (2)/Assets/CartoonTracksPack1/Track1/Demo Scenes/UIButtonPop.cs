using UnityEngine;
using UnityEngine.EventSystems;

// Faz o botao crescer suavemente ao passar o mouse e voltar ao sair.
// Tempo nao-escalado: funciona com o jogo pausado (timeScale 0).
public class UIButtonPop : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public float escalaHover = 1.08f;
    public float velocidade = 12f;

    private RectTransform rt;
    private Vector3 baseScale = Vector3.one;
    private float alvo = 1f;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        if (rt != null) baseScale = rt.localScale;
    }

    void OnEnable() { alvo = 1f; }
    void OnDisable() { if (rt != null) rt.localScale = baseScale; }

    public void OnPointerEnter(PointerEventData e) { alvo = escalaHover; }
    public void OnPointerExit(PointerEventData e) { alvo = 1f; }

    void Update()
    {
        if (rt == null) return;
        float k = 1f - Mathf.Exp(-velocidade * Time.unscaledDeltaTime);
        rt.localScale = Vector3.Lerp(rt.localScale, baseScale * alvo, k);
    }
}
