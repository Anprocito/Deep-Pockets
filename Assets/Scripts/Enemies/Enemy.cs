using System;
using UnityEngine;

// Enemigo que persigue SIEMPRE al jugador (sin rango de vision) y le hace dano al tocarlo.
// Se coloca en la capsula del enemigo. El tipo (Normal / Fast / Tank) se elige en el
// Inspector y las estadisticas salen del EnemyStatsSO.
[RequireComponent(typeof(CapsuleCollider))]
public class Enemy : MonoBehaviour
{
    [Header("Configuracion")]
    [SerializeField] private EnemyStatsSO stats;
    [SerializeField] private EnemyType type = EnemyType.Normal;

    public event Action<Enemy> Died;

    private Transform player;
    private PlayerStats playerStats;
    private PlayerHealth playerHealth;
    private Collider playerCollider;
    private Collider ownCollider;

    private float currentHealth;
    private float nextAttackTime;

    public EnemyType Type => type;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => stats != null ? stats.GetMaxHealth(type) : 0f;
    public float Damage => stats != null ? stats.GetDamage(type) : 0f;

    // Si es rapido, depende de la velocidad actual del jugador.
    public float MoveSpeed =>
        stats != null ? stats.GetMoveSpeed(type, playerStats != null ? playerStats.MoveSpeed : 0f) : 0f;

    private void Awake()
    {
        ownCollider = GetComponent<Collider>();

        if (stats == null)
        {
            Debug.LogError($"{name}: falta asignar el EnemyStatsSO.", this);
            enabled = false;
            return;
        }

        currentHealth = MaxHealth;
    }

    private void Start()
    {
        // Si el spawner no le asigno el jugador (por ejemplo, un enemigo puesto a mano), lo busca.
        if (player == null)
        {
            PlayerController found = FindFirstObjectByType<PlayerController>();
            if (found != null)
                Initialize(found.transform);
            else
                Debug.LogWarning($"{name}: no se encontro al jugador en la escena.", this);
        }
    }

    // Lo llama el spawner justo despues de crear al enemigo.
    public void Initialize(Transform playerTransform)
    {
        player = playerTransform;
        playerStats = playerTransform.GetComponent<PlayerStats>();
        playerHealth = playerTransform.GetComponent<PlayerHealth>();
        playerCollider = playerTransform.GetComponent<Collider>();

        if (playerStats == null)
            Debug.LogWarning("El jugador no tiene PlayerStats: el enemigo rapido no sabra la velocidad del jugador.", this);
        if (playerHealth == null)
            Debug.LogWarning("El jugador no tiene PlayerHealth: los enemigos no podran hacerle dano.", this);
        if (playerCollider == null)
            Debug.LogWarning("El jugador no tiene Collider: no se puede detectar el contacto.", this);
    }

    private void Update()
    {
        if (player == null) return;

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f; // se mueve solo en X y Z

        bool touching = IsTouchingPlayer();

        if (toPlayer.sqrMagnitude > 0.0001f)
        {
            Vector3 dir = toPlayer.normalized;
            transform.rotation = Quaternion.LookRotation(dir);

            // Se detiene al tocar al jugador; si el jugador se aleja, vuelve a perseguirlo.
            if (!touching)
                transform.position += dir * (MoveSpeed * Time.deltaTime);
        }

        if (touching)
            TryAttack();
    }

    // Detecta si el collider del enemigo y el del jugador se solapan.
    // Se hace por codigo para que funcione aunque el jugador se mueva por Transform y
    // aunque ninguno tenga Rigidbody.
    private bool IsTouchingPlayer()
    {
        if (playerCollider == null || ownCollider == null) return false;

        return Physics.ComputePenetration(
            ownCollider, ownCollider.transform.position, ownCollider.transform.rotation,
            playerCollider, playerCollider.transform.position, playerCollider.transform.rotation,
            out _, out _);
    }

    private void TryAttack()
    {
        if (playerHealth == null) return;
        if (Time.time < nextAttackTime) return;

        nextAttackTime = Time.time + stats.attackInterval;
        playerHealth.TakeDamage(Damage);
    }

    // Para usar mas adelante desde armas, proyectiles, etc.
    public void TakeDamage(float amount)
    {
        if (amount <= 0f) return;

        currentHealth -= amount;
        if (currentHealth <= 0f)
            Die();
    }

    private void Die()
    {
        Died?.Invoke(this);
        Destroy(gameObject);
    }
}