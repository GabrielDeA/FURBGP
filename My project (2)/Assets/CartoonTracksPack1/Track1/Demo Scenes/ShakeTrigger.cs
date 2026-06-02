using UnityEngine;

public class ShakeTrigger : MonoBehaviour
{
    private CameraShake cameraShake;

    [SerializeField] private float forcaMinimaImpacto = 5f;

    // Função auxiliar para garantir que a câmera seja encontrada
    private void VerificarCamera()
    {
        if (cameraShake == null)
        {
            cameraShake = GetComponentInChildren<CameraShake>() ?? Object.FindFirstObjectByType<CameraShake>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Pista"))
        {
            return;
        }

        if (other.CompareTag("Lombada"))
        {
            VerificarCamera();
            
            if (cameraShake != null)
            {
                cameraShake.ShakeCustom(0.02f, 0.15f, 3.0f, 0.1f);
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Pista") || collision.gameObject.name == "Ground")
        {
            return;
        }

        if (collision.gameObject.CompareTag("Lombada"))
        {
            return;
        }

        float forcaDoImpacto = collision.relativeVelocity.magnitude;

        if (forcaDoImpacto < forcaMinimaImpacto)
        {
            return; 
        }

        VerificarCamera();
        if (cameraShake != null)
        {
            cameraShake.Shake(); 
        }
    }
}