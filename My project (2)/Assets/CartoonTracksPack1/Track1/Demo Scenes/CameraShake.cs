using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    private float magnitude = 0.05f; 
    private float duration  = 0.2f;  
    private float decay     = 2.0f;  
    private float rotation  = 0.5f;  

    private Vector3 _initialLocalPosition;
    private Quaternion _initialLocalRotation;

    void Start()
    {
        _initialLocalPosition = transform.localPosition;
        _initialLocalRotation = transform.localRotation;
    }

    public void Shake()
    {
        StopAllCoroutines();
        StartCoroutine(ShakeRoutine(magnitude, duration, decay, rotation));
    }

    // NOVO: Método personalizado (permite configurar um tremor mais leve)
    public void ShakeCustom(float customMagnitude, float customDuration, float customDecay, float customRotation)
    {
        StopAllCoroutines();
        StartCoroutine(ShakeRoutine(customMagnitude, customDuration, customDecay, customRotation));
    }

    private IEnumerator ShakeRoutine(float mag, float dur, float dec, float rot)
    {
        float elapsed = 0f;
        while (elapsed < dur)
        {
            float env = Mathf.Exp(-dec * elapsed);
            float x   = Random.Range(-1f, 1f) * mag * env;
            float y   = Random.Range(-1f, 1f) * mag * env * 0.6f;
            float z   = Random.Range(-1f, 1f) * rot * env;

            transform.localPosition = _initialLocalPosition + new Vector3(x, y, 0);
            transform.localRotation = _initialLocalRotation * Quaternion.Euler(0, 0, z);

            elapsed += Time.deltaTime;
            yield return null;
        }
        
        transform.localPosition = _initialLocalPosition;
        transform.localRotation = _initialLocalRotation;
    }
}