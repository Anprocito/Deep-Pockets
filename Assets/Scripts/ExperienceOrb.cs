using UnityEngine;

// Orbe de experiencia que sueltan los enemigos al morir.
// Si el jugador pasa cerca, el orbe empieza a seguirlo; el jugador lo recoge al tocarlo.
// Se coloca en el prefab del orbe (una esfera con collider).
[RequireComponent(typeof(Collider))]
public class ExperienceOrb : MonoBehaviour
{
    [Tooltip("Experiencia que da este orbe.")]
    [Min(1)]
    [SerializeField] private int experience = 10;

    [Header("Atraccion hacia el jugador")]
    [Tooltip("Distancia a la que el orbe detecta al jugador y empieza a seguirlo. 0 = nunca lo sigue.")]
    [Min(0f)]
    [SerializeField] private float detectionRange = 4f;
    [Tooltip("Velocidad con la que empieza a seguir al jugador.")]
    [Min(0f)]
    [SerializeField] private float followSpeed = 6f;
    [Tooltip("Cuanto aumenta su velocidad por segundo mientras lo sigue, para alcanzarlo aunque corra.")]
    [Min(0f)]
    [SerializeField] private float acceleration = 25f;

    private PlayerExperience player;
    private Collider playerCollider;
    private Collider ownCollider;
    private bool collected;
    private bool following;
    private float currentSpeed;
    private float experienceMultiplier = 1f;

    private void Awake()
    {
        ownCollider = GetComponent<Collider>();
    }

    private void Start()
    {
        // Si el enemigo no le asigno el jugador, lo busca en la escena.
        if (player == null)
        {
            PlayerExperience found = FindFirstObjectByType<PlayerExperience>();
            if (found != null)
                Initialize(found);
            else
                Debug.LogWarning($"{name}: no se encontro al jugador (falta PlayerExperience).", this);
        }
    }

    // La experiencia que da el orbe se multiplica segun la oleada del enemigo que lo solto.
    public void SetExperienceMultiplier(float multiplier)
    {
        experienceMultiplier = multiplier;
    }

    // Lo llama el enemigo justo despues de crear el orbe.
    public void Initialize(PlayerExperience playerExperience)
    {
        player = playerExperience;
        playerCollider = playerExperience.GetComponent<Collider>();

        if (playerCollider == null)
            Debug.LogWarning("El jugador no tiene Collider: no se puede detectar el contacto con el orbe.", this);
    }

    private void Update()
    {
        if (collected || playerCollider == null) return;

        FollowPlayer();

        // Contacto por codigo (igual que el dano de los enemigos): funciona aunque el jugador
        // se mueva por Transform y aunque ninguno tenga Rigidbody.
        bool touching = Physics.ComputePenetration(
            ownCollider, ownCollider.transform.position, ownCollider.transform.rotation,
            playerCollider, playerCollider.transform.position, playerCollider.transform.rotation,
            out _, out _);

        if (!touching) return;

        collected = true;
        player.AddExperience(experience * experienceMultiplier);
        Destroy(gameObject);
    }

    // Cuando el jugador entra en el rango, el orbe empieza a seguirlo y ya no lo suelta.
    // Va acelerando, asi termina alcanzandolo aunque el jugador se aleje corriendo.
    private void FollowPlayer()
    {
        Vector3 target = playerCollider.bounds.center;

        if (!following)
        {
            if (detectionRange <= 0f) return;
            if ((target - transform.position).sqrMagnitude > detectionRange * detectionRange) return;

            following = true;
            currentSpeed = followSpeed;
        }

        currentSpeed += acceleration * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, target, currentSpeed * Time.deltaTime);
    }

    // Dibuja el rango de deteccion en la ventana Scene al seleccionar el orbe.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}