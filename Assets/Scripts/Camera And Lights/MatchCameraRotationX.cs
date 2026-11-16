using UnityEngine;

public class MatchCameraRotationX : MonoBehaviour
{
    private Transform camTransform;

    void Start()
    {
        if (Camera.main != null)
        {
            camTransform = Camera.main.transform;
        }
    }

    void LateUpdate()
    {
        if (camTransform != null)
        {
            // Copiamos únicamente la rotación en el eje X de la cámara
            Vector3 currentRotation = transform.localEulerAngles;
            currentRotation.x = camTransform.eulerAngles.x;
            
            // Aplicamos la rotación manteniendo los demás ejes intactos
            transform.localEulerAngles = currentRotation;
        }
    }
}