using System.Collections.Generic;
using UnityEngine;

// Estadisticas que pueden recibir bonus temporales.
public enum StatType
{
    MoveSpeed,
    Damage
}

// Estadisticas del jugador en tiempo de ejecucion. Se coloca en el jugador.
// Copia los valores del PlayerStatsSO al empezar. Las mejoras permanentes y los bonus
// temporales (pociones) se aplican sobre esta copia, asi el asset original queda intacto.
[DefaultExecutionOrder(-100)] // su Awake corre antes que el de los demas scripts del jugador
public class PlayerStats : MonoBehaviour
{
    [SerializeField] private PlayerStatsSO baseStats;

    private class TimedBonus
    {
        public StatType stat;
        public float percent;
        public float endTime;
    }

    private readonly List<TimedBonus> timedBonuses = new List<TimedBonus>();
    private float baseMoveSpeed;
    private float baseDamageMultiplier = 1f;

    public float MaxHealth { get; private set; }

    // Velocidad con los bonus temporales activos (por ejemplo, +50% de la pocion de velocidad).
    public float MoveSpeed =>
        Mathf.Max(0f, baseMoveSpeed * (1f + SumTimedBonuses(StatType.MoveSpeed) / 100f));

    // Multiplicador de dano con los bonus temporales activos. 1 = 100%, 1.3 = +30%.
    public float DamageMultiplier =>
        Mathf.Max(0f, baseDamageMultiplier + SumTimedBonuses(StatType.Damage) / 100f);

    private void Awake()
    {
        if (baseStats == null)
        {
            Debug.LogError("PlayerStats: falta asignar el PlayerStatsSO.", this);
            return;
        }

        MaxHealth = baseStats.maxHealth;
        baseMoveSpeed = baseStats.moveSpeed;
        baseDamageMultiplier = baseStats.damageMultiplier;
    }

    private void Update()
    {
        // Elimina los bonus temporales que ya terminaron.
        for (int i = timedBonuses.Count - 1; i >= 0; i--)
            if (Time.time >= timedBonuses[i].endTime)
                timedBonuses.RemoveAt(i);
    }

    // Mejora PERMANENTE de dano en porcentaje: AddDamagePercent(15) suma +15%.
    // Las mejoras se suman entre si: dos de +15 dan 1.30.
    public void AddDamagePercent(float percent)
    {
        baseDamageMultiplier = Mathf.Max(0f, baseDamageMultiplier + percent / 100f);
    }

    // Bonus TEMPORAL en porcentaje: se suma a las demas mejoras y desaparece solo al terminar el tiempo.
    public void AddTimedBonus(StatType stat, float percent, float duration)
    {
        timedBonuses.Add(new TimedBonus
        {
            stat = stat,
            percent = percent,
            endTime = Time.time + duration
        });
    }

    // Las armas usan esto para calcular su dano final.
    public float GetFinalDamage(float weaponBaseDamage)
    {
        return weaponBaseDamage * DamageMultiplier;
    }

    private float SumTimedBonuses(StatType stat)
    {
        float sum = 0f;
        foreach (TimedBonus bonus in timedBonuses)
            if (bonus.stat == stat && Time.time < bonus.endTime)
                sum += bonus.percent;

        return sum;
    }
}