using UnityEngine;
using UnityEngine.InputSystem;

// Solo movimiento del personaje en X y Z, con el nuevo Input System.
// Los límites del nivel y las colisiones los resuelve el Rigidbody con los Colliders de las paredes.
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float speed = 5f;

    private Rigidbody rb;
    private InputAction moveAction;
    private Vector3 direction;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Suaviza el movimiento entre los pasos de física (FixedUpdate) y los frames de render.
        // Sin esto el personaje se ve entrecortado/borroso al moverse.
        rb.interpolation = RigidbodyInterpolation.Interpolate;

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

    // El input se lee en Update (una vez por frame)...
    private void Update()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();

        // x del input -> eje X del mundo, y del input -> eje Z. El eje Y queda en 0.
        direction = new Vector3(input.x, 0f, input.y);
    }

    // ...y el movimiento físico en FixedUpdate (el ritmo con el que trabaja la física).
    private void FixedUpdate()
    {
        rb.MovePosition(rb.position + direction * speed * Time.fixedDeltaTime);
    }
}