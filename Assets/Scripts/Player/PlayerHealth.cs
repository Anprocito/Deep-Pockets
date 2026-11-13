using UnityEngine;
using UnityEngine.Events;

// Vida del jugador. Se coloca en el mismo objeto que PlayerController.
public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;

    [Header("Eventos (opcional, para UI, sonidos, pantalla de derrota...)")]
    public UnityEvent<float, float> onHealthChanged; // vida actual, vida maxima
    public UnityEvent onDied;

    private float currentHealth;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead => currentHealth <= 0f;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f) return;

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        Debug.Log($"Jugador recibio {amount} de dano. Vida: {currentHealth}/{maxHealth}");
        onHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0f)
            Die();
    }

    private void Die()
    {
        Debug.Log("El jugador murio.");

        // Deja al jugador sin control al morir.
        if (TryGetComponent(out PlayerController controller))
            controller.enabled = false;

        onDied?.Invoke();
    }
}
