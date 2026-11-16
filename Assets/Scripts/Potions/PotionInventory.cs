using System.Collections.Generic;
using UnityEngine;

// Pociones iniciales: tipo de pocion y cuantas.
[System.Serializable]
public class PotionStack
{
    public PotionSO potion;
    [Min(0)] public int count = 1;
}

// Pociones del jugador. Cada pocion ocupa casillas de la grilla del inventario.
// Se usan solas: cada frame se revisa si alguna cumple su condicion de activacion. Al usarse,
// la pocion desaparece y libera sus casillas. Una pocion no se usa mientras otra del mismo tipo
// siga haciendo efecto (asi no se gastan de mas).
[DefaultExecutionOrder(10)] // su Start corre despues del de WeaponInventory: las armas ocupan su casilla primero
[RequireComponent(typeof(PlayerStats))]
[RequireComponent(typeof(PlayerHealth))]
[RequireComponent(typeof(InventoryGrid))]
public class PotionInventory : MonoBehaviour
{
    [Header("Pociones iniciales")]
    [Tooltip("Se colocan en las primeras casillas libres de la grilla.")]
    [SerializeField] private List<PotionStack> startingPotions = new List<PotionStack>();

    [Header("Depuracion")]
    [Tooltip("Escribe en la consola cuando se usa una pocion y cuando termina su efecto.")]
    [SerializeField] private bool logUsage = true;

    private class ActiveEffect
    {
        public PotionSO potion;
        public float elapsed;
    }

    private readonly List<GridItem> potionItems = new List<GridItem>();
    private readonly List<ActiveEffect> activeEffects = new List<ActiveEffect>();

    private PlayerStats stats;
    private PlayerHealth health;
    private InventoryGrid inventoryGrid;

    private void Awake()
    {
        stats = GetComponent<PlayerStats>();
        health = GetComponent<PlayerHealth>();
        inventoryGrid = GetComponent<InventoryGrid>();

        if (inventoryGrid == null)
        {
            Debug.LogError("PotionInventory: falta el componente InventoryGrid en el jugador.", this);
            enabled = false;
            return;
        }

        inventoryGrid.ItemAdded += OnItemAdded;
    }

    private void Start()
    {
        foreach (PotionStack start in startingPotions)
        {
            if (start.potion == null) continue;

            for (int i = 0; i < start.count; i++)
            {
                if (!inventoryGrid.TryFindSpace(start.potion.Width, start.potion.Height, out int column, out int row))
                {
                    Debug.LogWarning($"PotionInventory: no hay espacio para {start.potion.potionName}.", this);
                    break;
                }

                inventoryGrid.Place(start.potion, column, row);
            }
        }
    }

    private void OnDestroy()
    {
        if (inventoryGrid != null)
            inventoryGrid.ItemAdded -= OnItemAdded;
    }

    private void Update()
    {
        if (health.IsDead) return;

        TickActiveEffects();
        TryUsePotions();
    }

    // Se llama cada vez que se coloca un objeto en la grilla.
    private void OnItemAdded(GridItem item)
    {
        if (item.Data is PotionSO)
            potionItems.Add(item);
    }

    private void TryUsePotions()
    {
        for (int i = potionItems.Count - 1; i >= 0; i--)
        {
            GridItem item = potionItems[i];
            PotionSO potion = (PotionSO)item.Data;

            if (IsActive(potion)) continue;
            if (!potion.ShouldTrigger(health)) continue;

            // La pocion se gasta y libera sus casillas.
            potionItems.RemoveAt(i);
            inventoryGrid.Remove(item);

            potion.Apply(stats, health);
            activeEffects.Add(new ActiveEffect { potion = potion, elapsed = 0f });

            if (logUsage)
                Debug.Log($"Pocion usada: {potion.potionName} ({potion.duration}s).");
        }
    }

    private void TickActiveEffects()
    {
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            ActiveEffect effect = activeEffects[i];
            PotionSO potion = effect.potion;

            // El ultimo paso se recorta para que el efecto total sea exacto (por ejemplo, justo 25% de curacion).
            float step = Mathf.Min(Time.deltaTime, potion.duration - effect.elapsed);
            potion.Tick(stats, health, step);
            effect.elapsed += step;

            if (effect.elapsed >= potion.duration - 0.0001f)
            {
                potion.End(stats, health);
                activeEffects.RemoveAt(i);

                if (logUsage)
                    Debug.Log($"Efecto terminado: {potion.potionName}");
            }
        }
    }

    private bool IsActive(PotionSO potion)
    {
        foreach (ActiveEffect effect in activeEffects)
            if (effect.potion == potion)
                return true;

        return false;
    }
}