using UnityEngine;

// Estadisticas del jugador en tiempo de ejecucion. Se coloca en el jugador.
// Copia los valores del PlayerStatsSO al empezar, y las mejoras se aplican sobre esta copia,
// asi el asset original queda intacto.
[DefaultExecutionOrder(-100)] // su Awake corre antes que el de los demas scripts del jugador
public class PlayerStats : MonoBehaviour
{
    [SerializeField] private PlayerStatsSO baseStats;

    public float MaxHealth { get; private set; }
    public float MoveSpeed { get; private set; }
    public float DamageMultiplier { get; private set; } = 1f;

    private void Awake()
    {
        if (baseStats == null)
        {
            Debug.LogError("PlayerStats: falta asignar el PlayerStatsSO.", this);
            return;
        }

        MaxHealth = baseStats.maxHealth;
        MoveSpeed = baseStats.moveSpeed;
        DamageMultiplier = baseStats.damageMultiplier;
    }

    // Mejora de dano en porcentaje: AddDamagePercent(15) suma +15% (el multiplicador pasa de 1.00 a 1.15).
    // Las mejoras se suman entre si: dos de +15 dan 1.30.
    public void AddDamagePercent(float percent)
    {
        DamageMultiplier = Mathf.Max(0f, DamageMultiplier + percent / 100f);
    }

    // Las armas usan esto para calcular su dano final.
    public float GetFinalDamage(float weaponBaseDamage)
    {
        return weaponBaseDamage * DamageMultiplier;
    }
}
