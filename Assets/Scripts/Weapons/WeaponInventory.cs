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
// Cada casilla de la grilla es un sector de anillo alrededor del jugador:
//   - la columna (direccion) decide hacia que punto cardinal apunta el sector (45 grados de ancho)
//   - la fila decide a que distancia queda (la fila de abajo es la mas cercana)
// Un arma que ocupa varias casillas cubre todos sus sectores juntos.
// Cada arma ataca sola cuando hay un enemigo dentro de alguno de sus sectores.
[RequireComponent(typeof(PlayerStats))]
[RequireComponent(typeof(InventoryGrid))]
public class WeaponInventory : MonoBehaviour
{
    private const float SectorAngle = 45f;

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

    [Header("Zonas de ataque (sectores alrededor del jugador)")]
    [Tooltip("Radio del hueco libre alrededor del jugador. Ahi empieza la fila de abajo de la grilla.")]
    [Min(0f)][SerializeField] private float innerRadius = 1.5f;
    [Tooltip("Ancho de cada fila: cuanto se aleja del jugador cada fila.")]
    [Min(0.1f)][SerializeField] private float rowWidth = 2f;
    [Tooltip("Margen para contar a un enemigo que toca el borde de la zona (su radio aproximado).")]
    [Min(0f)][SerializeField] private float enemyMargin = 0.4f;

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
    [Tooltip("Altura sobre el piso, para que la zona no se mezcle con el.")]
    [SerializeField] private float zoneHeightOffset = 0.03f;

    [Header("Depuracion")]
    [Tooltip("Escribe en la consola cada ataque y cada habilidad.")]
    [SerializeField] private bool logAttacks = true;

    private readonly List<EquippedWeapon> equipped = new List<EquippedWeapon>();
    private readonly List<ZoneSector> zones = new List<ZoneSector>();

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

    private void Update()
    {
        bool playerAlive = health == null || !health.IsDead;

        if (playerAlive)
        {
            foreach (EquippedWeapon weapon in equipped)
            {
                FillZones(weapon.Item);

                if (weapon.Tick(zones, enemyMargin, stats, logAttacks))
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

    // Calcula el sector de cada casilla que ocupa el objeto
    // (en el mismo orden en que se crearon las zonas visibles).
    private void FillZones(GridItem item)
    {
        zones.Clear();
        Vector3 origin = transform.position;

        for (int dr = 0; dr < item.Height; dr++)
        {
            for (int dc = 0; dc < item.Width; dc++)
                zones.Add(MakeZone(origin, item.Column + dc, item.Row + dr));
        }
    }

    private ZoneSector MakeZone(Vector3 origin, int column, int row)
    {
        // Las columnas van en sentido horario desde el Norte, de 45 en 45 grados.
        float yaw = column * SectorAngle * Mathf.Deg2Rad;
        GetRowBounds(row, out float inner, out float outer);

        return new ZoneSector
        {
            origin = origin,
            direction = new Vector2(Mathf.Sin(yaw), Mathf.Cos(yaw)),
            innerRadius = inner,
            outerRadius = outer,
            halfAngle = SectorAngle * 0.5f
        };
    }

    // Radios de una fila: la de ABAJO de la grilla es la mas cercana al jugador y la de ARRIBA (fila 0)
    // la mas lejana. Las filas son bandas pegadas una a otra, asi que las zonas no dejan huecos.
    private void GetRowBounds(int row, out float inner, out float outer)
    {
        int rowCount = inventoryGrid != null ? inventoryGrid.Rows : 4;
        int fromBottom = Mathf.Clamp(rowCount - 1 - row, 0, Mathf.Max(0, rowCount - 1));

        inner = innerRadius + fromBottom * rowWidth;
        outer = inner + rowWidth;
    }

    // ----- Zonas de ataque visibles en el piso (tambien en el juego final) -----

    private WeaponZoneVisual CreateZoneVisual(WeaponSO weapon)
    {
        if (zonesRoot == null)
            zonesRoot = new GameObject("WeaponZones").transform;

        GameObject zone = new GameObject($"Zone_{weapon.weaponName}");
        zone.transform.SetParent(zonesRoot, false);

        WeaponZoneVisual visual = zone.AddComponent<WeaponZoneVisual>();
        visual.Build();
        return visual;
    }

    private void UpdateZoneVisuals()
    {
        if (!showZones) return;

        float groundY = GetGroundY();

        foreach (EquippedWeapon weapon in equipped)
        {
            FillZones(weapon.Item);

            // 1 = recien disparado, 0 = destello terminado.
            float attackAmount = Mathf.Clamp01(1f - (Time.time - weapon.LastAttackTime) / flashDuration);
            float skillAmount = Mathf.Clamp01(1f - (Time.time - weapon.LastSkillTime) / flashDuration);

            Color color = idleColor;
            if (attackAmount > 0f) color = Color.Lerp(idleColor, attackColor, attackAmount);
            if (skillAmount > 0f) color = Color.Lerp(idleColor, skillColor, skillAmount); // la habilidad tiene prioridad

            for (int i = 0; i < weapon.Visuals.Count && i < zones.Count; i++)
            {
                ZoneSector zone = zones[i];
                float yaw = Mathf.Atan2(zone.direction.x, zone.direction.y) * Mathf.Rad2Deg;
                Vector3 position = new Vector3(zone.origin.x, groundY, zone.origin.z);

                weapon.Visuals[i].Apply(position, yaw, zone.innerRadius, zone.outerRadius, color);
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