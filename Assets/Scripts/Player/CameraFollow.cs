using UnityEngine;

// Cámara Top Down: sigue al personaje manteniendo una distancia fija (offset).
// La rotación de la cámara se configura en el Inspector, este script solo la mueve.
public class CameraFollow : MonoBehaviour
{
    [Header("Objetivo a seguir")]
    [SerializeField] private Transform target;

    [Header("Posición relativa al objetivo")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 12f, -6f);

    [Header("Suavizado (mayor valor = sigue más rápido)")]
    [SerializeField] private float smoothSpeed = 8f;

    // LateUpdate se ejecuta después de todos los Update,
    // así el personaje ya se movió y la cámara no "tiembla".
    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;

        // Lerp interpola entre la posición actual y la deseada.
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
    }
}
