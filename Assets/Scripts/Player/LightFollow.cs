using UnityEngine;

// Hace que una luz siga al personaje manteniendo una distancia fija (offset).
// Es la misma lógica que CameraFollow, aplicada a una luz.
public class LightFollow : MonoBehaviour
{
    [Header("Objetivo a seguir")]
    [SerializeField] private Transform target;

    [Header("Posición relativa al objetivo")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 5f, 0f);

    [Header("Suavizado (mayor valor = sigue más rápido)")]
    [SerializeField] private float smoothSpeed = 10f;

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
    }
}
