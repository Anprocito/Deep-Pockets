using UnityEngine;

// Orbe de experiencia que sueltan los enemigos al morir.
// El jugador lo recoge al tocarlo. Se coloca en el prefab del orbe (una esfera con collider).
[RequireComponent(typeof(Collider))]
public class ExperienceOrb : MonoBehaviour
{
    [Tooltip("Experiencia que da este orbe.")]
    [Min(1)]
    [SerializeField] private int experience = 10;

    private PlayerExperience player;
    private Collider playerCollider;
    private Collider ownCollider;
    private bool collected;

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

        // Contacto por codigo (igual que el dano de los enemigos): funciona aunque el jugador
        // se mueva por Transform y aunque ninguno tenga Rigidbody.
        bool touching = Physics.ComputePenetration(
            ownCollider, ownCollider.transform.position, ownCollider.transform.rotation,
            playerCollider, playerCollider.transform.position, playerCollider.transform.rotation,
            out _, out _);

        if (!touching) return;

        collected = true;
        player.AddExperience(experience);
        Destroy(gameObject);
    }
}
