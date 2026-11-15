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
            case CardinalDirection.North: return new Vector3(0f, 0f, 1f);
            case CardinalDirection.NorthEast: return new Vector3(1f, 0f, 1f).normalized;
            case CardinalDirection.East: return new Vector3(1f, 0f, 0f);
            case CardinalDirection.SouthEast: return new Vector3(1f, 0f, -1f).normalized;
            case CardinalDirection.South: return new Vector3(0f, 0f, -1f);
            case CardinalDirection.SouthWest: return new Vector3(-1f, 0f, -1f).normalized;
            case CardinalDirection.West: return new Vector3(-1f, 0f, 0f);
            default: return new Vector3(-1f, 0f, 1f).normalized; // NorthWest
        }
    }
}

// Inventario de armas del jugador. Se coloca en el jugador.
// La CELDA donde esta un arma decide donde ataca:
//   - la columna (direccion) decide hacia que punto cardinal queda su zona de ataque
//   - la fila decide a que distancia del jugador queda (fila 0 = la de arriba = la mas lejana)
// Cada arma ataca sola cuando detecta un enemigo dentro de su zona.
[RequireComponent(typeof(PlayerStats))]
public class WeaponInventory : MonoBehaviour
{
    [System.Serializable]
    public class StartingWeapon
    {
        public WeaponSO weapon;
        public CardinalDirection direction;
        [Tooltip("0 = fila de arriba de la grilla (la mas lejana al jugador).")]
        [Min(0)] public int row;
    }

    [Header("Armas iniciales")]
    [SerializeField] private List<StartingWeapon> startingWeapons = new List<StartingWeapon>();
    [Tooltip("Si la lista de armas iniciales esta vacia, equipa una daga de prueba (valores por defecto de WeaponSO) en Norte, fila 0 (la celda de arriba a la izquierda, la mas lejana).")]
    [SerializeField] private bool addTestDagger = true;

    [Header("Distancia de cada fila")]
    [Tooltip("Distancias desde el jugador hasta el CENTRO de la zona de ataque, una por fila. El orden en que las escribas no importa: la fila de ABAJO de la grilla toma la mas CERCANA y la de ARRIBA la mas LEJANA.")]
    [SerializeField] private float[] rowDistances = { 2f, 4f, 6f, 8f };

    [Header("Deteccion")]
    [SerializeField] private LayerMask enemyLayers = ~0;

    [Header("Zonas visibles en el juego")]
    [Tooltip("Dibuja en el piso la zona de ataque de cada arma. Se ve tambien en el juego final (los Gizmos no).")]
    [SerializeField] private bool showZones = true;
    [Tooltip("Color de la zona en reposo.")]
    [SerializeField] private Color idleColor = new Color(1f, 1f, 1f, 0.3f);
    [Tooltip("Color del destello al hacer un ataque normal.")]
    [SerializeField] private Color attackColor = new Color(1f, 0.35f, 0.2f, 1f);
    [Tooltip("Color del destello al usar una habilidad.")]
    [SerializeField] private Color skillColor = new Color(0.75f, 0.35f, 1f, 1f);
    [Tooltip("Segundos que dura el destello.")]
    [Min(0.05f)][SerializeField] private float flashDuration = 0.25f;
    [SerializeField] private float ringWidth = 0.08f;
    [Tooltip("Altura sobre el piso, para que la zona no se mezcle con el.")]
    [SerializeField] private float zoneHeightOffset = 0.03f;

    [Header("Referencias (opcional, se buscan solas)")]
    [SerializeField] private WeaponGridUI gridUI;

    [Header("Depuracion")]
    [Tooltip("Escribe en la consola cada ataque y cada habilidad.")]
    [SerializeField] private bool logAttacks = true;

    private readonly List<EquippedWeapon> equipped = new List<EquippedWeapon>();
    private readonly Collider[] buffer = new Collider[128];

    private float[] sortedRowDistances;
    private Transform zonesRoot;
    private float lastGroundY;

    private PlayerStats stats;
    private PlayerHealth health;
    private WeaponSO testDagger;

    private void Awake()
    {
        stats = GetComponent<PlayerStats>();
        TryGetComponent(out health);
        lastGroundY = transform.position.y;
    }

    private void Start()
    {
        if (gridUI == null)
            gridUI = FindFirstObjectByType<WeaponGridUI>();

        if (gridUI == null)
            Debug.LogWarning("WeaponInventory: no hay WeaponGridUI en la escena. Las armas funcionaran, pero sin mostrarse en la UI.", this);

        foreach (StartingWeapon start in startingWeapons)
            Equip(start.weapon, start.direction, start.row);

        // Daga de prueba: se crea por codigo, sin necesidad de crear el asset a mano.
        if (addTestDagger && startingWeapons.Count == 0)
        {
            testDagger = ScriptableObject.CreateInstance<WeaponSO>();
            testDagger.name = "Daga (prueba)";
            Equip(testDagger, CardinalDirection.North, 0);
        }

        // Escucha cuando el jugador suelta un arma sobre otra celda con el mouse.
        if (gridUI != null)
            gridUI.DropRequested += OnDropRequested;
    }

    private void OnDestroy()
    {
        if (gridUI != null)
            gridUI.DropRequested -= OnDropRequested;

        if (testDagger != null)
            Destroy(testDagger);

        if (zonesRoot != null)
            Destroy(zonesRoot.gameObject);
    }

    // Se llama al cambiar valores en el Inspector: obliga a recalcular las distancias ordenadas.
    private void OnValidate()
    {
        sortedRowDistances = null;
    }

    private void Update()
    {
        bool playerAlive = health == null || !health.IsDead;

        foreach (EquippedWeapon weapon in equipped)
        {
            if (!playerAlive) break;

            Vector3 center = GetZoneCenter(weapon.Direction, weapon.Row);

            if (weapon.Tick(center, stats, enemyLayers, buffer, logAttacks) && weapon.Cell != null)
                weapon.Cell.SetCounter(weapon.CounterText);
        }

        UpdateZoneVisuals();
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

        equippedWeapon.Visual = CreateZoneVisual(weapon);
        equipped.Add(equippedWeapon);
        return true;
    }

    private void OnDropRequested(WeaponGridCell from, WeaponGridCell to)
    {
        Move(from.Direction, from.Row, to.Direction, to.Row);
    }

    // Mueve un arma a otra celda. Solo se permite si la celda de destino esta libre;
    // si no, el arma se queda donde estaba.
    public bool Move(CardinalDirection fromDirection, int fromRow, CardinalDirection toDirection, int toRow)
    {
        EquippedWeapon weapon = GetWeaponAt(fromDirection, fromRow);
        if (weapon == null) return false;

        if (toRow < 0 || toRow >= rowDistances.Length)
        {
            Debug.LogWarning($"WeaponInventory: la fila {toRow} no tiene distancia asignada en Row Distances.", this);
            return false;
        }

        if (GetWeaponAt(toDirection, toRow) != null) return false;

        WeaponGridCell oldCell = weapon.Cell;
        weapon.MoveTo(toDirection, toRow);

        if (gridUI != null)
        {
            if (oldCell != null)
                oldCell.Clear();

            weapon.Cell = gridUI.GetCell(toDirection, toRow);
            if (weapon.Cell != null)
            {
                weapon.Cell.SetWeapon(weapon.Data.weaponName, weapon.Data.icon);
                weapon.Cell.SetCounter(weapon.CounterText);
            }
        }

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
        return transform.position + direction.ToVector() * GetRowDistance(row);
    }

    // Las distancias se ordenan de menor a mayor: la fila de ABAJO de la grilla toma la mas
    // cercana al jugador y la de ARRIBA (fila 0) la mas lejana.
    private float GetRowDistance(int row)
    {
        if (sortedRowDistances == null || sortedRowDistances.Length != rowDistances.Length)
        {
            sortedRowDistances = (float[])rowDistances.Clone();
            System.Array.Sort(sortedRowDistances);
        }

        if (sortedRowDistances.Length == 0) return 0f;

        int rowCount = gridUI != null ? gridUI.Rows : sortedRowDistances.Length;
        int index = Mathf.Clamp(rowCount - 1 - row, 0, sortedRowDistances.Length - 1);
        return sortedRowDistances[index];
    }

    // ----- Zonas de ataque visibles en el piso (tambien en el juego final) -----

    private WeaponZoneVisual CreateZoneVisual(WeaponSO weapon)
    {
        if (!showZones) return null;

        if (zonesRoot == null)
            zonesRoot = new GameObject("WeaponZones").transform;

        GameObject zone = new GameObject($"Zone_{weapon.weaponName}");
        zone.transform.SetParent(zonesRoot, false);

        WeaponZoneVisual visual = zone.AddComponent<WeaponZoneVisual>();
        visual.Build(weapon.attackRadius, ringWidth);
        return visual;
    }

    private void UpdateZoneVisuals()
    {
        if (!showZones) return;

        float groundY = GetGroundY();

        foreach (EquippedWeapon weapon in equipped)
        {
            if (weapon.Visual == null) continue;

            Vector3 center = GetZoneCenter(weapon.Direction, weapon.Row);
            center.y = groundY;

            // 1 = recien disparado, 0 = destello terminado.
            float attackAmount = Mathf.Clamp01(1f - (Time.time - weapon.LastAttackTime) / flashDuration);
            float skillAmount = Mathf.Clamp01(1f - (Time.time - weapon.LastSkillTime) / flashDuration);

            Color color = idleColor;
            if (attackAmount > 0f) color = Color.Lerp(idleColor, attackColor, attackAmount);
            if (skillAmount > 0f) color = Color.Lerp(idleColor, skillColor, skillAmount); // la habilidad tiene prioridad

            weapon.Visual.Apply(center, color);
        }
    }

    // Altura del piso bajo el jugador (se asume un piso plano, igual que en el spawner).
    private float GetGroundY()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 50f, ~0, QueryTriggerInteraction.Ignore))
            lastGroundY = hit.point.y + zoneHeightOffset;

        return lastGroundY;
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