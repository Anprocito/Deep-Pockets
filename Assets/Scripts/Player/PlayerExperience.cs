using UnityEngine;

// Experiencia y nivel del jugador. Se coloca en el jugador.
// Cada nivel pide un porcentaje mas de experiencia que el anterior.
public class PlayerExperience : MonoBehaviour
{
    [Header("Nivel")]
    [Tooltip("Experiencia necesaria para pasar del nivel 1 al 2.")]
    [Min(1)]
    [SerializeField] private int baseExperienceRequired = 100;

    [Tooltip("Cuanto mas pide cada nivel respecto al anterior, en porcentaje. 25 = +25%.")]
    [Min(0f)]
    [SerializeField] private float requirementIncreasePercent = 25f;

    private int level = 1;
    private float currentExperience;

    // Se lanza una vez por cada nivel que sube, con el nivel nuevo.
    public event System.Action<int> LeveledUp;

    // Experiencia que hace falta para pasar del nivel actual al siguiente.
    private int ExperienceRequired =>
        Mathf.RoundToInt(baseExperienceRequired * Mathf.Pow(1f + requirementIncreasePercent / 100f, level - 1));

    public void AddExperience(float amount)
    {
        if (amount <= 0f) return;

        currentExperience += amount;

        // Un solo orbe grande puede dar varios niveles de una vez; la experiencia sobrante se conserva.
        while (currentExperience >= ExperienceRequired)
        {
            currentExperience -= ExperienceRequired;
            level++;
            Debug.Log($"¡El jugador subió de nivel! Ahora es nivel {level}.");
            LeveledUp?.Invoke(level);
        }
    }
}