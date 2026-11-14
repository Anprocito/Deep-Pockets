using System.Collections.Generic;
using UnityEngine;

// Un arma colocada en una celda de la grilla, con su estado en tiempo de ejecucion
// (cooldown, contador de ataques y habilidad cargada).
// Es una clase normal de C#, no un componente: la actualiza WeaponInventory.
public class EquippedWeapon
{
    public WeaponSO Data { get; }
    public CardinalDirection Direction { get; }
    public int Row { get; }
    public WeaponGridCell Cell { get; set; }

    // Para dibujar el destello de ataque en los Gizmos.
    public float LastAttackTime { get; private set; } = -999f;
    public float LastSkillTime { get; private set; } = -999f;

    // Texto del contador de la celda, por ejemplo "3/5".
    public string CounterText => $"{attacksSinceSkill}/{Data.attacksForSkill}";

    private readonly List<Enemy> candidates = new List<Enemy>();
    private float nextAttackTime;
    private int attacksSinceSkill;
    private bool skillCharged;

    public EquippedWeapon(WeaponSO data, CardinalDirection direction, int row)
    {
        Data = data;
        Direction = direction;
        Row = row;
    }

    // Se llama en cada frame. Devuelve true si cambio el contador (para refrescar la UI).
    public bool Tick(Vector3 zoneCenter, PlayerStats stats, LayerMask layers, Collider[] buffer, bool log)
    {
        bool counterChanged = false;
        float now = Time.time;

        // 1) Ataque normal: solo si ya paso el intervalo y hay un enemigo en la zona.
        if (now >= nextAttackTime)
        {
            float damage = stats != null ? stats.GetFinalDamage(Data.baseDamage) : Data.baseDamage;

            if (Strike(zoneCenter, damage, Data.maxTargets, false, layers, buffer, log))
            {
                nextAttackTime = now + Data.attackInterval;
                LastAttackTime = now;
                counterChanged = true;

                // El contador no pasa del maximo mientras la habilidad espera un objetivo.
                if (!skillCharged)
                {
                    attacksSinceSkill++;
                    if (attacksSinceSkill >= Data.attacksForSkill)
                        skillCharged = true;
                }
            }
        }

        // 2) Habilidad: ataque aparte del normal, en la misma zona, con el doble de dano base.
        //    Si al cargarse no hay enemigos en la zona, queda cargada hasta que aparezca uno.
        if (skillCharged)
        {
            float baseSkillDamage = Data.baseDamage * Data.skillDamageMultiplier;
            float damage = stats != null ? stats.GetFinalDamage(baseSkillDamage) : baseSkillDamage;

            if (Strike(zoneCenter, damage, Data.skillMaxTargets, true, layers, buffer, log))
            {
                skillCharged = false;
                attacksSinceSkill = 0;
                LastSkillTime = now;
                counterChanged = true;
            }
        }

        return counterChanged;
    }

    // Golpea a los enemigos mas cercanos dentro de la zona. Devuelve false si no habia ninguno.
    private bool Strike(Vector3 center, float damage, int maxTargets, bool isSkill,
                        LayerMask layers, Collider[] buffer, bool log)
    {
        // QueryTriggerInteraction.Collide para detectar tambien enemigos con el collider en modo Trigger.
        int count = Physics.OverlapSphereNonAlloc(center, Data.attackRadius, buffer, layers,
            QueryTriggerInteraction.Collide);

        candidates.Clear();
        for (int i = 0; i < count; i++)
        {
            Enemy enemy = buffer[i].GetComponentInParent<Enemy>();

            // Se ignoran los que ya murieron en este mismo frame (Destroy se aplica al final del frame).
            if (enemy != null && enemy.CurrentHealth > 0f && !candidates.Contains(enemy))
                candidates.Add(enemy);
        }

        if (candidates.Count == 0) return false;

        candidates.Sort((a, b) =>
            (a.transform.position - center).sqrMagnitude.CompareTo((b.transform.position - center).sqrMagnitude));

        int hits = maxTargets <= 0 ? candidates.Count : Mathf.Min(maxTargets, candidates.Count);
        for (int i = 0; i < hits; i++)
            candidates[i].TakeDamage(damage);

        if (log)
        {
            Debug.Log($"{Data.weaponName} ({Direction}, fila {Row}) " +
                      $"{(isSkill ? "HABILIDAD" : "ataque")}: {damage} de dano a {hits} enemigo(s)");
        }

        return true;
    }
}
