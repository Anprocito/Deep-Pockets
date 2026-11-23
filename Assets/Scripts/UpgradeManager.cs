using System.Collections.Generic;
using UnityEngine;

// Sistema de mejoras al subir de nivel. Se coloca en un objeto vacio de la escena.
// Flujo (con el juego en pausa):
//   1) eleccion de una mejora de estadisticas (3 opciones)
//   2) eleccion de un objeto (3 opciones al azar, siempre con al menos uno de 1x1)
//   3) el jugador arrastra el objeto a su inventario; al colocarlo, el juego se reanuda
// Si el inventario no tiene espacio, en el paso 2 se da una segunda mejora de estadisticas.
public class UpgradeManager : MonoBehaviour
{
    private const int ItemChoiceCount = 3;

    [Header("Mejoras de estadisticas")]
    [Tooltip("Vida maxima que suma (tambien cura esa cantidad).")]
    [SerializeField] private float maxHealthUpgrade = 20f;
    [Tooltip("Porcentaje de velocidad de movimiento que suma.")]
    [SerializeField] private float moveSpeedUpgradePercent = 8f;
    [Tooltip("Porcentaje que suma al multiplicador de dano.")]
    [SerializeField] private float damageUpgradePercent = 15f;

    [Header("Objetos que pueden aparecer")]
    [Tooltip("Armas y pociones que se pueden ofrecer. Tiene que haber al menos una de 1x1.")]
    [SerializeField] private List<InventoryItemSO> itemPool = new List<InventoryItemSO>();

    [Header("Referencias (se buscan solas)")]
    [SerializeField] private PlayerExperience experience;
    [SerializeField] private PlayerStats stats;
    [SerializeField] private PlayerHealth health;
    [SerializeField] private InventoryGrid inventoryGrid;
    [SerializeField] private UpgradeUI ui;

    private int pendingLevelUps;   // subidas de nivel que esperan su turno
    private bool flowActive;
    private bool paused;
    private float previousTimeScale = 1f;
    private InventoryItemSO itemBeingPlaced;

    private void Start()
    {
        if (experience == null) experience = FindFirstObjectByType<PlayerExperience>();
        if (stats == null) stats = FindFirstObjectByType<PlayerStats>();
        if (health == null) health = FindFirstObjectByType<PlayerHealth>();
        if (inventoryGrid == null) inventoryGrid = FindFirstObjectByType<InventoryGrid>();
        if (ui == null) ui = FindFirstObjectByType<UpgradeUI>();

        List<string> missing = new List<string>();
        if (experience == null) missing.Add("PlayerExperience (en el jugador)");
        if (stats == null) missing.Add("PlayerStats (en el jugador)");
        if (health == null) missing.Add("PlayerHealth (en el jugador)");
        if (inventoryGrid == null) missing.Add("InventoryGrid (en el jugador)");
        if (ui == null) missing.Add("UpgradeUI (en un objeto ACTIVO dentro del Canvas)");
        else if (!ui.IsReady) missing.Add("UpgradeUI esta fuera de un Canvas");

        if (missing.Count > 0)
        {
            Debug.LogError("UpgradeManager: no puede funcionar. Falta: " + string.Join(", ", missing), this);
            enabled = false;
            return;
        }

        experience.LeveledUp += OnLeveledUp;
    }

    private void OnDestroy()
    {
        if (experience != null)
            experience.LeveledUp -= OnLeveledUp;

        if (inventoryGrid != null)
            inventoryGrid.NewItemDropped -= OnNewItemDropped;

        Resume(); // por si se destruye con el juego en pausa
    }

    private void OnLeveledUp(int newLevel)
    {
        if (VictoryManager.GameEnded) return; // la partida ya termino (victoria o derrota)

        // Si sube varios niveles de golpe, o sube mientras hay una ventana abierta, se hacen en cola.
        pendingLevelUps++;

        if (!flowActive)
            StartNextFlow();
    }

    private void StartNextFlow()
    {
        if (pendingLevelUps <= 0) return;

        pendingLevelUps--;
        flowActive = true;

        Pause();
        ShowStatChoice(false);
    }

    private void FinishFlow()
    {
        ui.Hide();
        flowActive = false;

        if (pendingLevelUps > 0)
            StartNextFlow(); // sigue en pausa y encadena la siguiente subida de nivel
        else
            Resume();
    }

    private void Pause()
    {
        if (paused) return;

        paused = true;
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
    }

    private void Resume()
    {
        if (!paused) return;

        paused = false;
        Time.timeScale = previousTimeScale;
    }

    // ----- Paso 1 (y paso 2 alternativo): mejora de estadisticas -----

    private void ShowStatChoice(bool isSecondUpgrade)
    {
        string title = isSecondUpgrade
            ? "Tu inventario está lleno: ¡elige otra mejora!"
            : "¡Subiste de nivel! Elige una mejora";

        ChoiceOption[] options =
        {
            new ChoiceOption { title = "Vida máxima", description = $"+{maxHealthUpgrade:0.#} de vida máxima" },
            new ChoiceOption { title = "Velocidad", description = $"+{moveSpeedUpgradePercent:0.#}% de velocidad de movimiento" },
            new ChoiceOption { title = "Daño", description = $"+{damageUpgradePercent:0.#}% de daño" }
        };

        ui.ShowChoice(title, options, index =>
        {
            ApplyStatUpgrade(index);

            if (isSecondUpgrade)
                FinishFlow();
            else
                ShowItemChoice();
        });
    }

    private void ApplyStatUpgrade(int index)
    {
        switch (index)
        {
            case 0: health.IncreaseMaxHealth(maxHealthUpgrade); break;
            case 1: stats.AddMoveSpeedPercent(moveSpeedUpgradePercent); break;
            case 2: stats.AddDamagePercent(damageUpgradePercent); break;
        }
    }

    // ----- Paso 2: eleccion de objeto -----

    private void ShowItemChoice()
    {
        List<InventoryItemSO> choices = PickItemChoices();

        // Sin espacio en el inventario: en lugar de un objeto, otra mejora de estadisticas.
        if (choices.Count == 0)
        {
            ShowStatChoice(true);
            return;
        }

        ChoiceOption[] options = new ChoiceOption[choices.Count];
        for (int i = 0; i < choices.Count; i++)
            options[i] = Describe(choices[i]);

        ui.ShowChoice("Elige un objeto", options, index => ShowPlacement(choices[index]));
    }

    // Elige hasta 3 objetos distintos del pool que quepan en el inventario,
    // siempre con al menos uno de 1x1.
    private List<InventoryItemSO> PickItemChoices()
    {
        List<InventoryItemSO> candidates = new List<InventoryItemSO>();
        foreach (InventoryItemSO item in itemPool)
        {
            if (item != null && !candidates.Contains(item) && inventoryGrid.HasSpaceFor(item.Width, item.Height))
                candidates.Add(item);
        }

        List<InventoryItemSO> choices = new List<InventoryItemSO>();

        // Primero uno de 1x1 al azar.
        List<InventoryItemSO> small = candidates.FindAll(item => item.Width == 1 && item.Height == 1);
        if (small.Count > 0)
        {
            InventoryItemSO pick = small[Random.Range(0, small.Count)];
            choices.Add(pick);
            candidates.Remove(pick);
        }
        else if (candidates.Count > 0)
        {
            Debug.LogWarning("UpgradeManager: no hay ningun objeto de 1x1 en el pool que quepa. " +
                             "Agrega al menos una arma o pocion de 1x1.", this);
        }

        // El resto, al azar entre los que quedan.
        while (choices.Count < ItemChoiceCount && candidates.Count > 0)
        {
            int index = Random.Range(0, candidates.Count);
            choices.Add(candidates[index]);
            candidates.RemoveAt(index);
        }

        // Se mezclan para que el de 1x1 no salga siempre en la primera tarjeta.
        for (int i = choices.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            InventoryItemSO temp = choices[i];
            choices[i] = choices[j];
            choices[j] = temp;
        }

        return choices;
    }

    private static ChoiceOption Describe(InventoryItemSO item)
    {
        string kind = item is WeaponSO ? "Arma" : "Poción";
        string details = $"{kind} · {item.Width}x{item.Height}";

        if (item is WeaponSO weapon)
            details += $"\nDaño {weapon.baseDamage:0.#} · cada {weapon.attackInterval:0.##} s";
        else if (item is PotionSO potion)
            details += $"\nDura {potion.duration:0.#} s";

        return new ChoiceOption
        {
            title = item.DisplayName,
            description = details,
            icon = item.Icon,
            shapeWidth = item.Width,
            shapeHeight = item.Height
        };
    }

    // ----- Paso 3: colocar el objeto en el inventario -----

    private void ShowPlacement(InventoryItemSO item)
    {
        itemBeingPlaced = item;
        inventoryGrid.NewItemDropped += OnNewItemDropped;

        ui.ShowPlacement(item, "Suéltalo en casillas libres de tu inventario");
    }

    // El jugador soltó el objeto sobre la grilla.
    private void OnNewItemDropped(InventoryItemSO item, int column, int row)
    {
        if (item != itemBeingPlaced) return;

        GridItem placed = inventoryGrid.Place(item, column, row);

        if (placed == null)
        {
            ui.SetPlacementMessage("Ahí no cabe. Prueba en otro lugar.");
            return;
        }

        inventoryGrid.NewItemDropped -= OnNewItemDropped;
        itemBeingPlaced = null;

        FinishFlow(); // cierra la ventana y reanuda el juego
    }
}