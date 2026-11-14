using System.Collections.Generic;
using UnityEngine;

public static class CardinalDirectionExtensions
{
    // Direccion en el mundo de cada punto cardinal.
    // Norte = +Z (tecla W) y Este = +X (tecla D), igual que el movimiento del jugador.
    public static Vector3 ToVector(this CardinalDirection direction)
    {
        switch (direction)
        {
            case CardinalDirection.North:     return new Vector3(0f, 0f, 1f);
            case CardinalDirection.NorthEast: return new Vector3(1f, 0f, 1f).normalized;
            case CardinalDirection.East:      return new Vector3(1f, 0f, 0f);
            case CardinalDirection.SouthEast: return new Vector3(1f, 0f, -1f).normalized;
            case CardinalDirection.South:     return new Vector3(0f, 0f, -1f);
            case CardinalDirection.SouthWest: return new Vector3(-1f, 0f, -1f).normalized;
            case CardinalDirection.West:      return new Vector3(-1f, 0f, 0f);
            default:                          return new Vector3(-1f, 0f, 1f).normalized; // NorthWest
        }
    }
}

// Inventario de armas del jugador. Se coloca en el jugador.
// La CELDA donde esta un arma decide donde ataca:
//   - la columna (direccion) decide hacia que punto cardinal queda su zona de ataque
//   - la fila decide a que distancia del jugador queda (fila 0 = la mas cercana)
// Cada arma ataca sola cuando detecta un enemigo dentro de su zona.
[RequireComponent(typeof(PlayerStats))]
public class WeaponInventory : MonoBehaviour
{
    [System.Serializable]
    public class StartingWeapon
    {
        public WeaponSO weapon;
        public CardinalDirection direction;
        [Tooltip("0 = fila de arriba de la grilla (la mas cercana al jugador).")]
        [Min(0)] public int row;
    }

    [Header("Armas iniciales")]
    [SerializeField] private List<StartingWeapon> startingWeapons = new List<StartingWeapon>();

    [Header("Distancia de cada fila")]
    [Tooltip("Distancia desde el jugador hasta el CENTRO de la zona de ataque, por fila (la fila 0 es la primera).")]
    [SerializeField] private float[] rowDistances = { 2f, 4f, 6f, 8f };

    [Header("Deteccion")]
    [SerializeField] private LayerMask enemyLayers = ~0;

    [Header("Referencias (opcional, se buscan solas)")]
    [SerializeField] private WeaponGridUI gridUI;

    [Header("Depuracion")]
    [Tooltip("Escribe en la consola cada ataque y cada habilidad.")]
    [SerializeField] private bool logAttacks = true;

    private readonly List<EquippedWeapon> equipped = new List<EquippedWeapon>();
    private readonly Collider[] buffer = new Collider[128];

    private PlayerStats stats;
    private PlayerHealth health;

    private void Awake()
    {
        stats = GetComponent<PlayerStats>();
        TryGetComponent(out health);
    }

    private void Start()
    {
        if (gridUI == null)
            gridUI = FindFirstObjectByType<WeaponGridUI>();

        if (gridUI == null)
            Debug.LogWarning("WeaponInventory: no hay WeaponGridUI en la escena. Las armas funcionaran, pero sin mostrarse en la UI.", this);

        foreach (StartingWeapon start in startingWeapons)
            Equip(start.weapon, start.direction, start.row);
    }

    private void Update()
    {
        if (health != null && health.IsDead) return;

        foreach (EquippedWeapon weapon in equipped)
        {
            Vector3 center = GetZoneCenter(weapon.Direction, weapon.Row);

            if (weapon.Tick(center, stats, enemyLayers, buffer, logAttacks) && weapon.Cell != null)
                weapon.Cell.SetCounter(weapon.CounterText);
        }
    }

    // Coloca un arma en una celda. Devuelve false si no se pudo (celda ocupada o fuera de la grilla).
    public bool Equip(WeaponSO weapon, CardinalDirection direction, int row)
    {
        if (weapon == null) return false;

        if (row < 0 || row >= rowDistances.Length || (gridUI != null && row >= gridUI.Rows))
        {
            Debug.LogWarning($"WeaponInventory: la fila {row} no existe (revisa Row Distances y las filas de la grilla).", this);
            return false;
        }

        if (GetWeaponAt(direction, row) != null)
        {
            Debug.LogWarning($"WeaponInventory: la celda ({direction}, fila {row}) ya esta ocupada.", this);
            return false;
        }

        EquippedWeapon equippedWeapon = new EquippedWeapon(weapon, direction, row);

        if (gridUI != null)
        {
            equippedWeapon.Cell = gridUI.GetCell(direction, row);
            if (equippedWeapon.Cell != null)
            {
                equippedWeapon.Cell.SetWeapon(weapon.weaponName, weapon.icon);
                equippedWeapon.Cell.SetCounter(equippedWeapon.CounterText);
            }
        }

        equipped.Add(equippedWeapon);
        return true;
    }

    public EquippedWeapon GetWeaponAt(CardinalDirection direction, int row)
    {
        foreach (EquippedWeapon weapon in equipped)
            if (weapon.Direction == direction && weapon.Row == row)
                return weapon;

        return null;
    }

    // Centro de la zona de ataque: el jugador + la direccion de la columna * la distancia de la fila.
    private Vector3 GetZoneCenter(CardinalDirection direction, int row)
    {
        return transform.position + direction.ToVector() * rowDistances[row];
    }

    // Dibuja las zonas de ataque (activa el boton Gizmos en la pestana Game para verlas ahi).
    // Amarillo = en espera, rojo = ataque normal, magenta = habilidad.
    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            foreach (EquippedWeapon weapon in equipped)
            {
                Color color = Color.yellow;
                if (Time.time - weapon.LastAttackTime < 0.2f) color = Color.red;
                if (Time.time - weapon.LastSkillTime < 0.2f) color = Color.magenta;

                DrawZone(weapon.Direction, weapon.Row, weapon.Data.attackRadius, color);
            }
        }
        else
        {
            // Vista previa de las armas iniciales mientras editas.
            foreach (StartingWeapon start in startingWeapons)
                if (start.weapon != null)
                    DrawZone(start.direction, start.row, start.weapon.attackRadius, Color.yellow);
        }
    }

    private void DrawZone(CardinalDirection direction, int row, float radius, Color color)
    {
        if (row < 0 || row >= rowDistances.Length) return;

        Gizmos.color = color;
        Gizmos.DrawWireSphere(GetZoneCenter(direction, row), radius);
    }
}
