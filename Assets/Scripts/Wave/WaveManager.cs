using UnityEngine;

// Multiplicadores que recibe un enemigo segun la oleada en la que aparece (1 = sin cambios).
public struct WaveStats
{
    public float damageMultiplier;
    public float healthMultiplier;
    public float speedMultiplier;      // no afecta al enemigo rapido (su velocidad es relativa al jugador)
    public float fastMarginMultiplier; // reduce la distancia entre la velocidad del rapido y la del jugador
    public float experienceMultiplier;

    public static WaveStats Default => new WaveStats
    {
        damageMultiplier = 1f,
        healthMultiplier = 1f,
        speedMultiplier = 1f,
        fastMarginMultiplier = 1f,
        experienceMultiplier = 1f
    };
}

// Controla las oleadas. Se coloca en un objeto vacio de la escena.
// La oleada cambia cuando aparece una cantidad de enemigos (no cuando se matan).
// Los cambios se acumulan oleada tras oleada.
public class WaveManager : MonoBehaviour
{
    [Header("Cambio de oleada")]
    [Tooltip("Enemigos que tienen que aparecer para pasar de la oleada 1 a la 2.")]
    [Min(1)] [SerializeField] private int enemiesInFirstWave = 30;
    [Tooltip("Cuanto aumenta ese requisito en cada oleada, en porcentaje.")]
    [Min(0f)] [SerializeField] private float requirementIncreasePercent = 30f;

    [Header("Cambios en cada oleada (porcentajes)")]
    [Min(0f)] [SerializeField] private float enemyDamageIncreasePercent = 6f;
    [Min(0f)] [SerializeField] private float enemyHealthIncreasePercent = 15f;
    [Min(0f)] [SerializeField] private float enemySpeedIncreasePercent = 8f;
    [Min(0f)] [SerializeField] private float experienceIncreasePercent = 13f;
    [Tooltip("Cuanto se reduce la velocidad de aparicion.")]
    [Range(0f, 99f)] [SerializeField] private float spawnRateReductionPercent = 6f;
    [Tooltip("Cuanto se reduce la distancia entre la velocidad del enemigo rapido y la del jugador.")]
    [Range(0f, 100f)] [SerializeField] private float fastEnemyMarginReductionPercent = 6f;

    private int wave = 1;
    private int spawnedThisWave;

    public int CurrentWave => wave;

    // Enemigos que tienen que aparecer en la oleada actual para pasar a la siguiente.
    public int EnemiesRequired =>
        Mathf.RoundToInt(enemiesInFirstWave * Mathf.Pow(1f + requirementIncreasePercent / 100f, wave - 1));

    // Enemigos que faltan por aparecer para cambiar de oleada.
    public int EnemiesRemaining => Mathf.Max(0, EnemiesRequired - spawnedThisWave);

    // El spawner multiplica su intervalo por esto: una velocidad de aparicion un 6% menor
    // en cada oleada equivale a un intervalo mayor.
    public float SpawnIntervalMultiplier =>
        1f / Mathf.Pow(1f - spawnRateReductionPercent / 100f, wave - 1);

    // Estadisticas que recibe un enemigo que aparece ahora.
    public WaveStats CurrentStats
    {
        get
        {
            int n = wave - 1;

            return new WaveStats
            {
                damageMultiplier = Mathf.Pow(1f + enemyDamageIncreasePercent / 100f, n),
                healthMultiplier = Mathf.Pow(1f + enemyHealthIncreasePercent / 100f, n),
                speedMultiplier = Mathf.Pow(1f + enemySpeedIncreasePercent / 100f, n),
                fastMarginMultiplier = Mathf.Pow(1f - fastEnemyMarginReductionPercent / 100f, n),
                experienceMultiplier = Mathf.Pow(1f + experienceIncreasePercent / 100f, n)
            };
        }
    }

    // Lo llama el spawner cada vez que aparece un enemigo.
    public void RegisterSpawn()
    {
        spawnedThisWave++;

        if (spawnedThisWave >= EnemiesRequired)
        {
            wave++;
            spawnedThisWave = 0;
        }
    }
}
