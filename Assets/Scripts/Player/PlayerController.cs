using UnityEngine;
using UnityEngine.InputSystem;

// Movimiento del personaje mediante su propio Transform (nuevo Input System),
// con colisiones contra paredes, bloques y piso usando Physics.CapsuleCast.
// La velocidad sale de PlayerStats.
[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(PlayerStats))]
public class PlayerController : MonoBehaviour
{
    [Header("Gravedad / Piso")]
    [SerializeField] private bool useGravity = true;
    [Tooltip("Aceleracion de caida en unidades por segundo al cuadrado.")]
    [SerializeField] private float gravity = 20f;

    [Header("Colisiones")]
    [Tooltip("Margen de seguridad frente a paredes y piso.")]
    [SerializeField] private float skinWidth = 0.03f;
    [SerializeField] private LayerMask collisionMask = ~0;

    private PlayerStats stats;
    private CapsuleCollider capsule;
    private InputAction moveAction;
    private float fallSpeed;

    private void Awake()
    {
        stats = GetComponent<PlayerStats>();
        capsule = GetComponent<CapsuleCollider>();

        // Si el personaje todavia tiene un Rigidbody, se deja inactivo para que no
        // aplique su propia gravedad ni interfiera con el movimiento por Transform.
        if (TryGetComponent(out Rigidbody rb))
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        moveAction = new InputAction("Move", InputActionType.Value);

        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");

        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow");
    }

    private void OnEnable()
    {
        moveAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
    }

    private void OnDestroy()
    {
        moveAction.Dispose();
    }

    private void Update()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();
        float step = stats.MoveSpeed * Time.deltaTime;

        // Cada eje se mueve por separado: asi el personaje desliza por las paredes.
        MoveAlong(Vector3.right * (input.x * step));   // X del input -> eje X del mundo
        MoveAlong(Vector3.forward * (input.y * step)); // Y del input -> eje Z del mundo

        if (useGravity)
        {
            fallSpeed += gravity * Time.deltaTime;
            float fallDistance = fallSpeed * Time.deltaTime;

            float moved = MoveAlong(Vector3.down * fallDistance);

            // Si no pudo caer todo lo que queria, esta apoyado en el piso.
            if (moved < fallDistance - 0.0001f)
                fallSpeed = 0f;
        }
    }

    // Mueve el personaje como maximo hasta el primer collider que encuentre.
    // Devuelve la distancia realmente recorrida.
    private float MoveAlong(Vector3 move)
    {
        float distance = move.magnitude;
        if (distance <= 0.000001f) return 0f;

        Vector3 dir = move / distance;
        GetCapsulePoints(out Vector3 p1, out Vector3 p2, out float radius);

        float allowed = distance;
        if (Physics.CapsuleCast(p1, p2, radius, dir, out RaycastHit hit,
                distance + skinWidth, collisionMask, QueryTriggerInteraction.Ignore))
        {
            allowed = Mathf.Max(0f, hit.distance - skinWidth);
        }

        transform.position += dir * allowed;
        return allowed;
    }

    // Calcula los dos extremos de la capsula en coordenadas del mundo.
    private void GetCapsulePoints(out Vector3 p1, out Vector3 p2, out float radius)
    {
        Vector3 scale = transform.lossyScale;
        Vector3 center = transform.TransformPoint(capsule.center);

        radius = capsule.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
        float height = Mathf.Max(capsule.height * Mathf.Abs(scale.y), radius * 2f);
        float half = height * 0.5f - radius;

        p1 = center + Vector3.up * half;
        p2 = center - Vector3.up * half;
    }
}