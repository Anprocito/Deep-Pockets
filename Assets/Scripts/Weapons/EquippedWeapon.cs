using System.Collections.Generic;
using UnityEngine;

// Estado en tiempo de ejecucion de un arma colocada en la grilla (tiempo de espera, contador de
// ataques o temporizador, y habilidad cargada). Su posicion la da el GridItem.
// Es una clase normal de C#, no un componente: la actualiza WeaponInventory.
public class EquippedWeapon
{
    public WeaponSO Data { get; }
    public GridItem Item { get; }

    // Una zona visible por cada casilla que ocupa el arma.
    public List<WeaponZoneVisual> Visuals { get; } = new List<WeaponZoneVisual>();

    // Para el destello de las zonas visibles.
    public float LastAttackTime { get; private set; } = -999f;
    public float LastSkillTime { get; private set; } = -999f;

    // Texto del contador de la casilla: "3/5" (por ataques) o "8s" (por tiempo).
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

    public EquippedWeapon(WeaponSO data, GridItem item)
    {
        Data = data;
        Item = item;

        // En las armas por tiempo, el temporizador empieza a correr al equiparlas.
        nextSkillTime = Time.time + data.skillCooldown;
    }

    // Se llama en cada frame con el centro de la zona de cada casilla que ocupa el arma.
    // Devuelve true si cambio el texto del contador (para refrescar la UI).
    public bool Tick(List<Vector3> zoneCenters, PlayerStats stats, LayerMask layers, Collider[] buffer, bool log)
    {
        bool counterChanged = false;
        bool byTime = Data.skillTrigger == SkillTrigger.Time;
        float now = Time.time;

        float baseSkillDamage = Data.baseDamage * Data.skillDamageMultiplier;
        float skillDamage = stats != null ? stats.GetFinalDamage(baseSkillDamage) : baseSkillDamage;

        // 1) Ataque normal: solo si ya paso el intervalo y hay un enemigo en alguna zona.
        if (now >= nextAttackTime)
        {
            float damage = stats != null ? stats.GetFinalDamage(Data.baseDamage) : Data.baseDamage;

            if (Strike(zoneCenters, damage, Data.maxTargets, false, layers, buffer, log))
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
            //    haya enemigos en las zonas o no. Golpea a los que esten dentro en ese momento.
            if (now >= nextSkillTime)
            {
                bool hit = Strike(zoneCenters, skillDamage, Data.skillMaxTargets, true, layers, buffer, log);

                if (!hit && log)
                    Debug.Log($"{Data.weaponName} HABILIDAD: sin enemigos en la zona");

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
            // 3) Habilidad por ataques: ataque aparte del normal, en las mismas zonas.
            //    Si al cargarse no hay enemigos, espera cargada hasta que aparezca uno.
            if (Strike(zoneCenters, skillDamage, Data.skillMaxTargets, true, layers, buffer, log))
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

    // Golpea a los enemigos mas cercanos que esten dentro de alguna de las zonas.
    // Devuelve false si no habia ninguno.
    private bool Strike(List<Vector3> zoneCenters, float damage, int maxTargets, bool isSkill,
                        LayerMask layers, Collider[] buffer, bool log)
    {
        candidates.Clear();

        for (int z = 0; z < zoneCenters.Count; z++)
        {
            // QueryTriggerInteraction.Collide para detectar tambien enemigos con el collider en modo Trigger.
            int count = Physics.OverlapSphereNonAlloc(zoneCenters[z], Data.attackRadius, buffer, layers,
                QueryTriggerInteraction.Collide);

            for (int i = 0; i < count; i++)
            {
                Enemy enemy = buffer[i].GetComponentInParent<Enemy>();

                // Se ignoran los que ya murieron en este mismo frame (Destroy se aplica al final del frame).
                if (enemy != null && enemy.CurrentHealth > 0f && !candidates.Contains(enemy))
                    candidates.Add(enemy);
            }
        }

        if (candidates.Count == 0) return false;

        candidates.Sort((a, b) =>
            NearestZoneSqrDistance(a, zoneCenters).CompareTo(NearestZoneSqrDistance(b, zoneCenters)));

        int hits = maxTargets <= 0 ? candidates.Count : Mathf.Min(maxTargets, candidates.Count);
        for (int i = 0; i < hits; i++)
            candidates[i].TakeDamage(damage);

        if (log)
        {
            Debug.Log($"{Data.weaponName} ({(CardinalDirection)Item.Column}, fila {Item.Row}) " +
                      $"{(isSkill ? "HABILIDAD" : "ataque")}: {damage} de dano a {hits} enemigo(s)");
        }

        return true;
    }

    private static float NearestZoneSqrDistance(Enemy enemy, List<Vector3> zoneCenters)
    {
        float best = float.MaxValue;

        for (int i = 0; i < zoneCenters.Count; i++)
        {
            float distance = (enemy.transform.position - zoneCenters[i]).sqrMagnitude;
            if (distance < best) best = distance;
        }

        return best;
    }
}