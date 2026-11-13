using System.Collections.Generic;
using UnityEngine;

// Genera enemigos en un punto aleatorio del plano XZ, a una distancia del jugador
// entre minRadius y maxRadius. Se coloca en un objeto vacio de la escena.
public class EnemySpawner : MonoBehaviour
{
    [System.Serializable]
    public class SpawnEntry
    {
        [Tooltip("Prefab de la capsula enemiga (con el script Enemy).")]
        public GameObject prefab;
        [Tooltip("Probabilidad relativa de que salga este enemigo.")]
        [Min(0f)] public float weight = 1f;
    }

    [Header("Referencias")]
    [SerializeField] private Transform player;

    [Header("Enemigos")]
    [SerializeField] private List<SpawnEntry> enemies = new List<SpawnEntry>();

    [Header("Zona de aparicion (plano XZ alrededor del jugador)")]
    [SerializeField] private float minRadius = 6f;
    [SerializeField] private float maxRadius = 10f;

    [Header("Ritmo")]
    [Tooltip("Segundos entre cada aparicion.")]
    [SerializeField] private float spawnInterval = 2f;
    [Tooltip("Maximo de enemigos vivos al mismo tiempo.")]
    [SerializeField] private int maxAlive = 30;

    private readonly List<GameObject> alive = new List<GameObject>();
    private float timer;

    private void OnValidate()
    {
        minRadius = Mathf.Max(0f, minRadius);
        maxRadius = Mathf.Max(minRadius, maxRadius);
    }

    private void Start()
    {
        if (player == null)
        {
            PlayerController found = FindFirstObjectByType<PlayerController>();
            if (found != null)
                player = found.transform;
            else
                Debug.LogError("EnemySpawner: no hay jugador asignado ni encontrado en la escena.", this);
        }
    }

    private void Update()
    {
        if (player == null) return;

        timer += Time.deltaTime;
        if (timer < spawnInterval) return;

        timer = 0f;
        TrySpawn();
    }

    private void TrySpawn()
    {
        // Limpia de la lista los enemigos que ya murieron.
        alive.RemoveAll(e => e == null);
        if (alive.Count >= maxAlive) return;

        GameObject prefab = PickPrefab();
        if (prefab == null) return;

        GameObject instance = Instantiate(prefab, GetSpawnPosition(), Quaternion.identity);
        alive.Add(instance);

        if (instance.TryGetComponent(out Enemy enemy))
            enemy.Initialize(player);
        else
            Debug.LogWarning($"El prefab {prefab.name} no tiene el script Enemy.", prefab);
    }

    // Punto aleatorio en el anillo entre minRadius y maxRadius.
    // Usar la raiz cuadrada reparte los puntos de forma uniforme (sin acumularse cerca del centro).
    private Vector3 GetSpawnPosition()
    {
        float angle = Random.Range(0f, Mathf.PI * 2f);
        float radius = Mathf.Sqrt(Random.Range(minRadius * minRadius, maxRadius * maxRadius));

        Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
        return player.position + offset; // misma altura (Y) que el jugador
    }

    // Elige un prefab segun los pesos configurados.
    private GameObject PickPrefab()
    {
        float total = 0f;
        foreach (SpawnEntry entry in enemies)
            if (entry.prefab != null) total += entry.weight;

        if (total <= 0f) return null;

        float roll = Random.Range(0f, total);
        foreach (SpawnEntry entry in enemies)
        {
            if (entry.prefab == null) continue;

            roll -= entry.weight;
            if (roll <= 0f) return entry.prefab;
        }

        return null;
    }

    // Dibuja los dos radios en la ventana Scene al seleccionar el spawner.
    private void OnDrawGizmosSelected()
    {
        if (player == null) return;

        Gizmos.color = Color.yellow;
        DrawCircle(player.position, minRadius);
        DrawCircle(player.position, maxRadius);
    }

    private static void DrawCircle(Vector3 center, float radius)
    {
        const int segments = 48;
        Vector3 prev = center + new Vector3(radius, 0f, 0f);

        for (int i = 1; i <= segments; i++)
        {
            float a = i / (float)segments * Mathf.PI * 2f;
            Vector3 next = center + new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius);
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
}
