using UnityEngine;

// Pocion de vida: recupera un porcentaje de la vida MAXIMA de forma gradual durante su duracion.
// Crear con: clic derecho en Project > Create > Potions > Health Potion
[CreateAssetMenu(fileName = "HealthPotion", menuName = "Potions/Health Potion")]
public class HealthPotionSO : PotionSO
{
    [Header("Curacion")]
    [Tooltip("Porcentaje de la vida maxima que recupera EN TOTAL a lo largo de toda la duracion.")]
    [Min(0f)] public float healPercentOfMaxHealth = 25f;

    public override void Tick(PlayerStats stats, PlayerHealth health, float deltaTime)
    {
        float totalHeal = health.MaxHealth * healPercentOfMaxHealth / 100f;
        health.Heal(totalHeal * deltaTime / duration);
    }

    // Valores iniciales al crear el asset.
    private void Reset()
    {
        potionName = "Pocion de vida";
        trigger = PotionTrigger.HealthThreshold;
        triggerHealthPercent = 45f;
        includeThreshold = true;
        duration = 5f;
        healPercentOfMaxHealth = 25f;
    }
}
