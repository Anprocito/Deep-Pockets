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

// Armas del jugador. Se coloca en el jugador (junto a InventoryGrid).
// La CASILLA donde esta un arma decide donde ataca:
//   - la columna (direccion) decide hacia que punto cardinal queda su zona de ataque
//   - la fila decide a que distancia del jugador queda (la fila de abajo es la mas cercana)
// Un arma que ocupa varias casillas tiene una zona de ataque en cada una.
// Cada arma ataca sola cuando detecta un enemigo dentro de alguna de sus zonas.
[RequireComponent(typeof(PlayerStats))]
[RequireComponent(typeof(InventoryGrid))]
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

    [Header("Distancia de cada fila")]
    [Tooltip("Distancias desde el jugador hasta el CENTRO de la zona de ataque, una por fila. " +
             "El orden en que las escribas no importa: la fila de ABAJO de la grilla toma la mas CERCANA " +
             "y la de ARRIBA la mas LEJANA.")]
    [SerializeField] private float[] rowDistances = { 2f, 4f, 6f, 8f };

    [Header("Deteccion")]
    [SerializeField] private LayerMask enemyLayers = ~0;

    [Header("Zonas visibles en el juego")]
    [Tooltip("Dibuja en el piso la zona de ataque de cada arma. Se ve tambien en el juego final.")]
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

    [Header("Depuracion")]
    [Tooltip("Escribe en la consola cada ataque y cada habilidad.")]
    [SerializeField] private bool logAttacks = true;

    private readonly List<EquippedWeapon> equipped = new List<EquippedWeapon>();
    private readonly List<Vector3> zoneCenters = new List<Vector3>();
    private readonly Collider[] buffer = new Collider[128];

    private float[] sortedRowDistances;
    private Transform zonesRoot;
    private float lastGroundY;

    private PlayerStats stats;
    private PlayerHealth health;
    private InventoryGrid inventoryGrid;

    private void Awake()
    {
        stats = GetComponent<PlayerStats>();
        inventoryGrid = GetComponent<InventoryGrid>();
        TryGetComponent(out health);
        lastGroundY = transform.position.y;

        if (inventoryGrid == null)
        {
            Debug.LogError("WeaponInventory: falta el componente InventoryGrid en el jugador.", this);
            enabled = false;
            return;
        }

        inventoryGrid.ItemAdded += OnItemAdded;
    }

    private void Start()
    {
        foreach (StartingWeapon start in startingWeapons)
            Equip(start.weapon, start.direction, start.row);

        if (startingWeapons.Count == 0)
            Debug.LogWarning("WeaponInventory: no hay armas iniciales. Agrega tu daga en Starting Weapons " +
                             "(Weapon = el asset de la daga, Direction = North, Row = 0).", this);
    }

    private void OnDestroy()
    {
        if (inventoryGrid != null)
            inventoryGrid.ItemAdded -= OnItemAdded;

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

        if (playerAlive)
        {
            foreach (EquippedWeapon weapon in equipped)
            {
                FillZoneCenters(weapon.Item);

                if (weapon.Tick(zoneCenters, stats, enemyLayers, buffer, logAttacks))
                    inventoryGrid.SetCounter(weapon.Item, weapon.CounterText);
            }
        }

        UpdateZoneVisuals();
    }

    // Coloca un arma en la grilla, con su esquina superior izquierda en esa direccion y fila.
    public bool Equip(WeaponSO weapon, CardinalDirection direction, int row)
    {
        if (weapon == null) return false;

        GridItem placed = inventoryGrid.Place(weapon, (int)direction, row);

        if (placed == null)
            Debug.LogWarning($"WeaponInventory: {weapon.weaponName} no cabe en ({direction}, fila {row}).", this);

        return placed != null;
    }

    // Se llama cada vez que se coloca un objeto en la grilla (por Equip o por el sistema de mejoras).
    private void OnItemAdded(GridItem item)
    {
        WeaponSO weaponData = item.Data as WeaponSO;
        if (weaponData == null) return; // las pociones las gestiona PotionInventory

        EquippedWeapon weapon = new EquippedWeapon(weaponData, item);
        item.Weapon = weapon;
        equipped.Add(weapon);

        if (showZones)
        {
            // Una zona visible por cada casilla que ocupa el arma.
            int cells = item.Width * item.Height;
            for (int i = 0; i < cells; i++)
                weapon.Visuals.Add(CreateZoneVisual(weaponData));
        }

        inventoryGrid.SetCounter(item, weapon.CounterText);
    }

    // Calcula el centro de la zona de ataque de cada casilla que ocupa el objeto
    // (en el mismo orden en que se crearon las zonas visibles).
    private void FillZoneCenters(GridItem item)
    {
        zoneCenters.Clear();

        for (int dr = 0; dr < item.Height; dr++)
        {
            for (int dc = 0; dc < item.Width; dc++)
                zoneCenters.Add(GetZoneCenter((CardinalDirection)(item.Column + dc), item.Row + dr));
        }
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

        int rowCount = inventoryGrid != null ? inventoryGrid.Rows : sortedRowDistances.Length;
        int index = Mathf.Clamp(rowCount - 1 - row, 0, sortedRowDistances.Length - 1);
        return sortedRowDistances[index];
    }

    // ----- Zonas de ataque visibles en el piso (tambien en el juego final) -----

    private WeaponZoneVisual CreateZoneVisual(WeaponSO weapon)
    {
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
            FillZoneCenters(weapon.Item);

            // 1 = recien disparado, 0 = destello terminado.
            float attackAmount = Mathf.Clamp01(1f - (Time.time - weapon.LastAttackTime) / flashDuration);
            float skillAmount = Mathf.Clamp01(1f - (Time.time - weapon.LastSkillTime) / flashDuration);

            Color color = idleColor;
            if (attackAmount > 0f) color = Color.Lerp(idleColor, attackColor, attackAmount);
            if (skillAmount > 0f) color = Color.Lerp(idleColor, skillColor, skillAmount); // la habilidad tiene prioridad

            for (int i = 0; i < weapon.Visuals.Count && i < zoneCenters.Count; i++)
            {
                Vector3 center = zoneCenters[i];
                center.y = groundY;
                weapon.Visuals[i].Apply(center, color);
            }
        }
    }

    // Altura del piso bajo el jugador (se asume un piso plano, igual que en el spawner).
    private float GetGroundY()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 50f, ~0, QueryTriggerInteraction.Ignore))
            lastGroundY = hit.point.y + zoneHeightOffset;

        return lastGroundY;
    }
}