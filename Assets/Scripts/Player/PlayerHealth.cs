using UnityEngine;
using UnityEngine.Events;

// Vida del jugador. Se coloca en el mismo objeto que PlayerController.
// La vida maxima sale de PlayerStats.
[RequireComponent(typeof(PlayerStats))]
public class PlayerHealth : MonoBehaviour
{
    [Header("Eventos (opcional, para UI, sonidos, pantalla de derrota...)")]
    public UnityEvent<float, float> onHealthChanged; // vida actual, vida maxima
    public UnityEvent onDied;

    private PlayerStats stats;
    private float currentHealth;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => stats.MaxHealth;
    public bool IsDead => currentHealth <= 0f;

    private void Awake()
    {
        stats = GetComponent<PlayerStats>();
        currentHealth = stats.MaxHealth;
    }

    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f) return;

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        Debug.Log($"Jugador recibio {amount} de dano. Vida: {currentHealth}/{stats.MaxHealth}");
        onHealthChanged?.Invoke(currentHealth, stats.MaxHealth);

        if (currentHealth <= 0f)
            Die();
    }

    // Sube la vida maxima y suma esa misma cantidad a la vida actual.
    public void IncreaseMaxHealth(float amount)
    {
        if (amount <= 0f) return;

        stats.AddMaxHealth(amount);
        currentHealth = Mathf.Min(stats.MaxHealth, currentHealth + amount);
        onHealthChanged?.Invoke(currentHealth, stats.MaxHealth);
    }

    // Recupera vida sin pasarse de la vida maxima.
    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f) return;

        float before = currentHealth;
        currentHealth = Mathf.Min(stats.MaxHealth, currentHealth + amount);

        if (currentHealth != before)
            onHealthChanged?.Invoke(currentHealth, stats.MaxHealth);
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