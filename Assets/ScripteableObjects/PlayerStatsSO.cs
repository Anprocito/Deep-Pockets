using UnityEngine;

// Estadisticas BASE del jugador. Solo se leen; nunca se modifican durante el juego.
// Las mejoras se aplican sobre la copia en tiempo de ejecucion (PlayerStats).
// Crear con: clic derecho en Project > Create > Player > Player Stats
[CreateAssetMenu(fileName = "PlayerStats", menuName = "Player/Player Stats")]
public class PlayerStatsSO : ScriptableObject
{
    [Header("Estadisticas base del jugador")]
    [Min(1f)] public float maxHealth = 100f;
    [Min(0f)] public float moveSpeed = 5f;

    [Tooltip("Multiplicador de dano para TODAS las armas. 1 = 100% (sin bonus), 1.15 = +15%.")]
    [Min(0f)] public float damageMultiplier = 1f;
}
