using UnityEngine;

// Pocion de dano: aumenta el multiplicador de dano del jugador (todas las armas) durante su duracion.
// Crear con: clic derecho en Project > Create > Potions > Damage Potion
[CreateAssetMenu(fileName = "DamagePotion", menuName = "Potions/Damage Potion")]
public class DamagePotionSO : PotionSO
{
    [Header("Dano")]
    [Tooltip("Aumento del dano en porcentaje. 30 = +30% (el multiplicador sube de 1.00 a 1.30).")]
    [Min(0f)] public float damageBonusPercent = 30f;

    public override void Apply(PlayerStats stats, PlayerHealth health)
    {
        // PlayerStats quita el bonus solo cuando termina el tiempo.
        stats.AddTimedBonus(StatType.Damage, damageBonusPercent, duration);
    }

    // Valores iniciales al crear el asset.
    private void Reset()
    {
        potionName = "Pocion de dano";
        trigger = PotionTrigger.Immediately;
        duration = 15f;
        damageBonusPercent = 30f;
    }
}
