using UnityEngine;

public enum EnemyType
{
    Normal,
    Fast,
    Tank
}

// Datos de todos los enemigos en un solo asset.
// Se define el enemigo normal como base y los otros tipos se calculan con multiplicadores
// configurables, para poder balancear todo desde un unico lugar.
// Crear con: clic derecho en Project > Create > Enemies > Enemy Stats
[CreateAssetMenu(fileName = "EnemyStats", menuName = "Enemies/Enemy Stats")]
public class EnemyStatsSO : ScriptableObject
{
    [Header("Enemigo normal (base)")]
    [Min(1f)] public float normalHealth = 100f;
    [Min(0f)] public float normalMoveSpeed = 3f;
    [Min(0f)] public float normalDamage = 10f;

    [Header("Ataque (todos los tipos)")]
    [Tooltip("Segundos entre golpes mientras el enemigo sigue tocando al jugador.")]
    [Min(0.05f)] public float attackInterval = 1f;

    [Header("Enemigo rapido")]
    [Tooltip("Unidades que se restan a la velocidad del jugador.")]
    [Min(0f)] public float fastSpeedBelowPlayer = 2f;
    [Min(0f)] public float fastHealthMultiplier = 0.5f;
    [Min(0f)] public float fastDamageMultiplier = 0.5f;

    [Header("Enemigo tanque")]
    [Min(0f)] public float tankHealthMultiplier = 3f;
    [Min(0f)] public float tankSpeedMultiplier = 0.5f;
    [Min(0f)] public float tankDamageMultiplier = 2f;

    public float GetMaxHealth(EnemyType type)
    {
        switch (type)
        {
            case EnemyType.Fast: return normalHealth * fastHealthMultiplier;
            case EnemyType.Tank: return normalHealth * tankHealthMultiplier;
            default:             return normalHealth;
        }
    }

    public float GetDamage(EnemyType type)
    {
        switch (type)
        {
            case EnemyType.Fast: return normalDamage * fastDamageMultiplier;
            case EnemyType.Tank: return normalDamage * tankDamageMultiplier;
            default:             return normalDamage;
        }
    }

    // El enemigo rapido depende de la velocidad actual del jugador.
    public float GetMoveSpeed(EnemyType type, float playerSpeed)
    {
        switch (type)
        {
            case EnemyType.Fast: return Mathf.Max(0f, playerSpeed - fastSpeedBelowPlayer);
            case EnemyType.Tank: return normalMoveSpeed * tankSpeedMultiplier;
            default:             return normalMoveSpeed;
        }
    }
}
