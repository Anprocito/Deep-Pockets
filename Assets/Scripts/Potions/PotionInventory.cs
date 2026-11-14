using System.Collections.Generic;
using UnityEngine;

// Pociones que tiene el jugador y cantidad de cada una.
[System.Serializable]
public class PotionStack
{
    public PotionSO potion;
    [Min(0)] public int count = 1;
}

// Inventario de pociones del jugador. Se coloca en el jugador.
// Las pociones se usan solas: cada frame se revisa si alguna cumple su condicion de activacion.
// Una pocion no se vuelve a usar mientras su efecto anterior siga activo (asi no se gastan de mas).
[RequireComponent(typeof(PlayerStats))]
[RequireComponent(typeof(PlayerHealth))]
public class PotionInventory : MonoBehaviour
{
    [Header("Pociones iniciales")]
    [SerializeField] private List<PotionStack> startingPotions = new List<PotionStack>();

    [Header("Depuracion")]
    [Tooltip("Escribe en la consola cuando se usa una pocion y cuando termina su efecto.")]
    [SerializeField] private bool logUsage = true;

    private class ActiveEffect
    {
        public PotionSO potion;
        public float elapsed;
    }

    private readonly List<PotionStack> stacks = new List<PotionStack>();
    private readonly List<ActiveEffect> activeEffects = new List<ActiveEffect>();

    private PlayerStats stats;
    private PlayerHealth health;

    private void Awake()
    {
        stats = GetComponent<PlayerStats>();
        health = GetComponent<PlayerHealth>();

        foreach (PotionStack start in startingPotions)
            Add(start.potion, start.count);
    }

    private void Update()
    {
        if (health.IsDead) return;

        TickActiveEffects();
        TryUsePotions();
    }

    // Agrega pociones al inventario (se usara, por ejemplo, al recoger una).
    public void Add(PotionSO potion, int amount = 1)
    {
        if (potion == null || amount <= 0) return;

        foreach (PotionStack stack in stacks)
        {
            if (stack.potion == potion)
            {
                stack.count += amount;
                return;
            }
        }

        stacks.Add(new PotionStack { potion = potion, count = amount });
    }

    public int GetCount(PotionSO potion)
    {
        foreach (PotionStack stack in stacks)
            if (stack.potion == potion)
                return stack.count;

        return 0;
    }

    private void TryUsePotions()
    {
        foreach (PotionStack stack in stacks)
        {
            PotionSO potion = stack.potion;

            if (potion == null || stack.count <= 0) continue;
            if (IsActive(potion)) continue;
            if (!potion.ShouldTrigger(health)) continue;

            stack.count--;
            potion.Apply(stats, health);
            activeEffects.Add(new ActiveEffect { potion = potion, elapsed = 0f });

            if (logUsage)
                Debug.Log($"Pocion usada: {potion.potionName} ({potion.duration}s). Quedan {stack.count}.");
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
