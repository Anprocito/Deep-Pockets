using UnityEngine;

// Cuando se usa una pocion (siempre de forma automatica, el jugador no tiene controles para esto).
public enum PotionTrigger
{
    HealthThreshold, // cuando la vida del jugador baja del umbral configurado
    Immediately      // apenas el jugador la tiene
}

// Base de todas las pociones. Cada tipo de pocion es una clase hija que define su efecto
// (curacion, velocidad, dano...); lo comun (nombre, icono, cuando se activa y cuanto dura) vive aqui.
// No se puede crear directamente: se crea alguna de las hijas desde Create > Potions.
public abstract class PotionSO : ScriptableObject
{
    [Header("General")]
    public string potionName = "Pocion";
    public Sprite icon;

    [Header("Activacion (automatica)")]
    public PotionTrigger trigger = PotionTrigger.HealthThreshold;
    [Tooltip("Solo si Trigger = Health Threshold: porcentaje de vida maxima que sirve de umbral.")]
    [Range(0f, 100f)] public float triggerHealthPercent = 45f;
    [Tooltip("Marcado: se usa con vida IGUAL O MENOR al umbral. Desmarcado: solo con vida estrictamente MENOR.")]
    public bool includeThreshold = true;

    [Header("Duracion")]
    [Tooltip("Segundos que dura el efecto.")]
    [Min(0.1f)] public float duration = 5f;

    // Indica si, con la vida actual del jugador, hay que usar la pocion ahora.
    public bool ShouldTrigger(PlayerHealth health)
    {
        switch (trigger)
        {
            case PotionTrigger.Immediately:
                return true;

            case PotionTrigger.HealthThreshold:
            {
                // Se multiplica antes de dividir para que valores como 45% de 100 den exactamente 45.
                float limit = health.MaxHealth * triggerHealthPercent / 100f;
                return includeThreshold ? health.CurrentHealth <= limit : health.CurrentHealth < limit;
            }

            default:
                return false;
        }
    }

    // Al empezar el efecto.
    public virtual void Apply(PlayerStats stats, PlayerHealth health) { }

    // En cada frame mientras dura el efecto. deltaTime ya viene recortado para no pasarse de la duracion.
    public virtual void Tick(PlayerStats stats, PlayerHealth health, float deltaTime) { }

    // Al terminar el efecto.
    public virtual void End(PlayerStats stats, PlayerHealth health) { }
}
