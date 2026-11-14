using System.Collections.Generic;
using UnityEngine;

// Un arma colocada en una celda de la grilla, con su estado en tiempo de ejecucion
// (cooldown, contador de ataques o temporizador, y habilidad cargada).
// Es una clase normal de C#, no un componente: la actualiza WeaponInventory.
public class EquippedWeapon
{
    public WeaponSO Data { get; }
    public CardinalDirection Direction { get; private set; }
    public int Row { get; private set; }
    public WeaponGridCell Cell { get; set; }

    // Para dibujar el destello de ataque en los Gizmos.
    public float LastAttackTime { get; private set; } = -999f;
    public float LastSkillTime { get; private set; } = -999f;

    // Texto del contador de la celda: "3/5" (por ataques) o "8s" (por tiempo).
    public string CounterText
    {
        get
        {
            if (Data.skillTrigger == SkillTrigger.Time)
                return $"{SecondsLeft(Time.time)}s";

            return $"{attacksSinceSkill}/{Data.attacksForSkill}";
        }
    }

    private readonly List<Enemy> candidates = new List<Enemy>();
    private float nextAttackTime;
    private float nextSkillTime;
    private int attacksSinceSkill;
    private int lastDisplayedSeconds = -1;
    private bool skillCharged;

    public EquippedWeapon(WeaponSO data, CardinalDirection direction, int row)
    {
        Data = data;
        Direction = direction;
        Row = row;

        // En las armas por tiempo, el temporizador empieza a correr al equiparlas.
        nextSkillTime = Time.time + data.skillCooldown;
    }

    // Cambia el arma de celda. Conserva su contador, su temporizador y su tiempo de espera.
    public void MoveTo(CardinalDirection direction, int row)
    {
        Direction = direction;
        Row = row;
    }

    // Se llama en cada frame. Devuelve true si cambio el texto del contador (para refrescar la UI).
    public bool Tick(Vector3 zoneCenter, PlayerStats stats, LayerMask layers, Collider[] buffer, bool log)
    {
        bool counterChanged = false;
        bool byTime = Data.skillTrigger == SkillTrigger.Time;
        float now = Time.time;

        float baseSkillDamage = Data.baseDamage * Data.skillDamageMultiplier;
        float skillDamage = stats != null ? stats.GetFinalDamage(baseSkillDamage) : baseSkillDamage;

        // 1) Ataque normal: solo si ya paso el intervalo y hay un enemigo en la zona.
        if (now >= nextAttackTime)
        {
            float damage = stats != null ? stats.GetFinalDamage(Data.baseDamage) : Data.baseDamage;

            if (Strike(zoneCenter, damage, Data.maxTargets, false, layers, buffer, log))
            {
                nextAttackTime = now + Data.attackInterval;
                LastAttackTime = now;

                // Solo las armas por ataques cuentan golpes. El contador no pasa del maximo
                // mientras la habilidad espera un objetivo.
                if (!byTime && !skillCharged)
                {
                    attacksSinceSkill++;
                    counterChanged = true;

                    if (attacksSinceSkill >= Data.attacksForSkill)
                        skillCharged = true;
                }
            }
        }

        if (byTime)
        {
            // 2) Habilidad por tiempo: se lanza sola cada vez que termina el temporizador,
            //    haya enemigos en la zona o no. Golpea a los que esten dentro en ese momento.
            if (now >= nextSkillTime)
            {
                bool hit = Strike(zoneCenter, skillDamage, Data.skillMaxTargets, true, layers, buffer, log);

                if (!hit && log)
                    Debug.Log($"{Data.weaponName} ({Direction}, fila {Row}) HABILIDAD: sin enemigos en la zona");

                LastSkillTime = now;
                nextSkillTime = now + Data.skillCooldown; // el temporizador vuelve a empezar
                counterChanged = true;
            }

            // Refresca el texto cuando cambia el numero de segundos mostrado.
            int seconds = SecondsLeft(now);
            if (seconds != lastDisplayedSeconds)
            {
                lastDisplayedSeconds = seconds;
                counterChanged = true;
            }
        }
        else if (skillCharged)
        {
            // 3) Habilidad por ataques: ataque aparte del normal, en la misma zona.
            //    Si al cargarse no hay enemigos en la zona, espera cargada hasta que aparezca uno.
            if (Strike(zoneCenter, skillDamage, Data.skillMaxTargets, true, layers, buffer, log))
            {
                skillCharged = false;
                attacksSinceSkill = 0;
                LastSkillTime = now;
                counterChanged = true;
            }
        }

        return counterChanged;
    }

    private int SecondsLeft(float now)
    {
        return Mathf.CeilToInt(Mathf.Max(0f, nextSkillTime - now));
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