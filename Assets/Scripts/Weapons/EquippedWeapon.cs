using System.Collections.Generic;
using UnityEngine;

// Zona de ataque de UNA casilla de la grilla: un sector de anillo alrededor del jugador
// (45 grados de ancho, entre un radio interior y uno exterior). Solo cuenta el plano XZ.
public struct ZoneSector
{
    public Vector3 origin;     // posicion del jugador
    public Vector2 direction;  // direccion central del sector en XZ (normalizada)
    public float innerRadius;
    public float outerRadius;
    public float halfAngle;    // la mitad del ancho del sector, en grados

    // "tolerance" es el margen para contar a un enemigo que toca el borde (su radio aproximado).
    public bool Contains(Vector3 point, float tolerance)
    {
        Vector2 offset = new Vector2(point.x - origin.x, point.z - origin.z);
        float distance = offset.magnitude;

        if (distance > outerRadius + tolerance) return false;
        if (distance < innerRadius - tolerance) return false;
        if (distance < 0.0001f) return true;

        // Angulo entre la direccion hacia el enemigo y el centro del sector.
        // La tolerancia lateral es mayor cuanto mas cerca del jugador esta el enemigo.
        float angle = Vector2.Angle(direction, offset);
        float angularTolerance = Mathf.Atan2(tolerance, distance) * Mathf.Rad2Deg;

        return angle <= halfAngle + angularTolerance;
    }
}

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

    // Se llama en cada frame con la zona de cada casilla que ocupa el arma.
    // Devuelve true si cambio el texto del contador (para refrescar la UI).
    public bool Tick(List<ZoneSector> zones, float tolerance, PlayerStats stats, bool log)
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

            if (Strike(zones, tolerance, damage, Data.maxTargets, false, log))
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
                bool hit = Strike(zones, tolerance, skillDamage, Data.skillMaxTargets, true, log);

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
            if (Strike(zones, tolerance, skillDamage, Data.skillMaxTargets, true, log))
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

    // Golpea a los enemigos mas cercanos al jugador que esten dentro de alguna de las zonas.
    // Devuelve false si no habia ninguno.
    private bool Strike(List<ZoneSector> zones, float tolerance, float damage, int maxTargets,
                        bool isSkill, bool log)
    {
        candidates.Clear();

        List<Enemy> active = Enemy.Active;
        for (int i = 0; i < active.Count; i++)
        {
            Enemy enemy = active[i];

            // Se ignoran los que ya murieron en este mismo frame (Destroy se aplica al final del frame).
            if (enemy.CurrentHealth <= 0f) continue;

            Vector3 position = enemy.transform.position;
            for (int z = 0; z < zones.Count; z++)
            {
                if (zones[z].Contains(position, tolerance))
                {
                    candidates.Add(enemy);
                    break;
                }
            }
        }

        if (candidates.Count == 0) return false;

        Vector3 origin = zones[0].origin;
        candidates.Sort((a, b) =>
            (a.transform.position - origin).sqrMagnitude.CompareTo((b.transform.position - origin).sqrMagnitude));

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
}