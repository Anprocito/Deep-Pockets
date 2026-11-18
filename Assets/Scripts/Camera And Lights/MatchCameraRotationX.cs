using UnityEngine;

// Deja el sprite de frente a la camara, igual que el del jugador: copia la inclinacion (eje X) de la
// camara y mantiene sus otros ejes fijos EN EL MUNDO (los que tenia al empezar).
// Como trabaja con la rotacion del mundo, no le afecta que el objeto padre gire
// (por ejemplo, un enemigo girando hacia el jugador).
// Ponerlo en el objeto del sprite (hijo del enemigo), no en el que tiene el collider:
// si se pone en la raiz, el collider tambien se inclinaria.
public class MatchCameraRotationX : MonoBehaviour
{
    private Transform camTransform;
    private float baseYaw;   // rotacion Y del mundo al empezar
    private float baseRoll;  // rotacion Z del mundo al empezar

    private void Awake()
    {
        baseYaw = transform.eulerAngles.y;
        baseRoll = transform.eulerAngles.z;
    }

    private void Start()
    {
        FindCamera();
    }

    private void LateUpdate()
    {
        if (camTransform == null)
        {
            FindCamera(); // por si la camara se crea despues
            if (camTransform == null) return;
        }

        // Solo cambia la inclinacion (X); Y y Z se quedan como estaban, sin importar al padre.
        transform.rotation = Quaternion.Euler(camTransform.eulerAngles.x, baseYaw, baseRoll);
    }

    private void FindCamera()
    {
        if (Camera.main != null)
            camTransform = Camera.main.transform;
    }
}