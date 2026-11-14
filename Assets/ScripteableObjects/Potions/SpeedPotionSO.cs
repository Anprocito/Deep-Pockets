using UnityEngine;

// Pocion de velocidad: aumenta la velocidad de movimiento del jugador durante su duracion.
// Crear con: clic derecho en Project > Create > Potions > Speed Potion
[CreateAssetMenu(fileName = "SpeedPotion", menuName = "Potions/Speed Potion")]
public class SpeedPotionSO : PotionSO
{
    [Header("Velocidad")]
    [Tooltip("Aumento de la velocidad de movimiento en porcentaje. 50 = +50%.")]
    [Min(0f)] public float speedBonusPercent = 50f;

    public override void Apply(PlayerStats stats, PlayerHealth health)
    {
        // PlayerStats quita el bonus solo cuando termina el tiempo.
        stats.AddTimedBonus(StatType.MoveSpeed, speedBonusPercent, duration);
    }

    // Valores iniciales al crear el asset.
    private void Reset()
    {
        potionName = "Pocion de velocidad";
        trigger = PotionTrigger.HealthThreshold;
        triggerHealthPercent = 30f;
        includeThreshold = false; // solo con vida MENOR al 30%
        duration = 7f;
        speedBonusPercent = 50f;
    }
}
