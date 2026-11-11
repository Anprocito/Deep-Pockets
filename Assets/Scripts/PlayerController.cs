using UnityEngine;
using UnityEngine.InputSystem;

// Movimiento del personaje en X y Z mediante su propio Transform (nuevo Input System).
// Como mover el Transform ignora la física, antes de cada desplazamiento se hace un
// "barrido" con el Rigidbody (SweepTest) para detectar paredes y no atravesarlas.
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float speed = 5f;

    [Header("Colisiones")]
    [Tooltip("Margen de seguridad frente a las paredes.")]
    [SerializeField] private float skinWidth = 0.02f;

    private Rigidbody rb;
    private InputAction moveAction;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // El Rigidbody solo se usa para detectar colisiones; el movimiento lo hace el Transform.
        rb.isKinematic = true;
        rb.useGravity = false;

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
        if (input == Vector2.zero) return;

        // x del input -> eje X del mundo, y del input -> eje Z. El eje Y queda en 0.
        Vector3 move = new Vector3(input.x, 0f, input.y) * (speed * Time.deltaTime);

        // Sincroniza el Transform con el motor de fisica para que el barrido use la posicion actual.
        Physics.SyncTransforms();

        // Intenta el movimiento completo; si choca, desliza por cada eje por separado.
        if (CanMove(move))
        {
            transform.position += move;
        }
        else
        {
            Vector3 moveX = new Vector3(move.x, 0f, 0f);
            Vector3 moveZ = new Vector3(0f, 0f, move.z);

            if (moveX != Vector3.zero && CanMove(moveX))
            {
                transform.position += moveX;
                Physics.SyncTransforms();
            }

            if (moveZ != Vector3.zero && CanMove(moveZ))
            {
                transform.position += moveZ;
            }
        }
    }

    // Devuelve true si no hay ningun collider en el camino del desplazamiento.
    private bool CanMove(Vector3 move)
    {
        float distance = move.magnitude;
        if (distance <= 0f) return true;

        return !rb.SweepTest(move / distance, out _, distance + skinWidth, QueryTriggerInteraction.Ignore);
    }
}