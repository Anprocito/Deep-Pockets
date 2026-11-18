using UnityEngine;
using UnityEngine.UI;

// Condicion de victoria: al eliminar cierta cantidad de enemigos, el jugador gana.
// Muestra un contador arriba a la izquierda y, al ganar, pausa el juego y muestra la pantalla de victoria.
// Se puede colocar en cualquier objeto: si no esta dentro de un Canvas, crea su interfaz en el
// Canvas de la escena (o crea uno).
public class VictoryManager : MonoBehaviour
{
    // True cuando el jugador ya gano. Otros sistemas lo consultan para no abrir mas ventanas.
    public static bool GameWon { get; private set; }

    [Header("Victoria")]
    [Tooltip("Enemigos que hay que eliminar para ganar.")]
    [Min(1)][SerializeField] private int enemiesToWin = 450;
    [SerializeField] private string victoryTitle = "Ganaste!";

    [Header("Contador (esquina superior izquierda)")]
    [Tooltip("Separacion respecto al borde izquierdo (x) y al borde superior (y) de la pantalla.")]
    [SerializeField] private Vector2 margin = new Vector2(20f, 20f);
    [SerializeField] private int counterFontSize = 28;

    [Header("Colores")]
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private Color dimColor = new Color(0f, 0f, 0f, 0.8f);

    [Header("Referencias (se buscan solas)")]
    [SerializeField] private PlayerExperience experience;
    [SerializeField] private WaveManager waveManager;

    private RectTransform root; // donde se construye la interfaz
    private Font font;
    private Text counterText;
    private int kills;
    private float elapsed; // tiempo de juego: no cuenta mientras el juego esta en pausa

    private void Awake()
    {
        GameWon = false;

        root = PrepareRoot();
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Ocupa toda la pantalla, pero no dibuja nada ni bloquea clics hasta que se gane.
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        counterText = CreateText("Counter", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(margin.x, -margin.y), new Vector2(700f, counterFontSize * 1.4f),
            string.Empty, counterFontSize, FontStyle.Bold, TextAnchor.MiddleLeft);

        UpdateCounter();
    }

    private void OnEnable()
    {
        Enemy.AnyEnemyDied += OnEnemyDied;
    }

    private void OnDisable()
    {
        Enemy.AnyEnemyDied -= OnEnemyDied;
    }

    private void OnDestroy()
    {
        // Si la interfaz se creo en un objeto aparte, se borra junto con este script.
        if (root != null && root.gameObject != gameObject)
            Destroy(root.gameObject);
    }

    private void Start()
    {
        if (experience == null) experience = FindFirstObjectByType<PlayerExperience>();
        if (waveManager == null) waveManager = FindFirstObjectByType<WaveManager>();
    }

    private void Update()
    {
        if (!GameWon)
            elapsed += Time.deltaTime; // con el juego en pausa, deltaTime es 0
    }

    // Si este objeto ya es parte de la UI (tiene Rect Transform y un Canvas arriba), se usa tal cual.
    // Si no, se construye en un objeto nuevo dentro del Canvas de la escena (o de uno creado aqui).
    private RectTransform PrepareRoot()
    {
        RectTransform own = transform as RectTransform;
        if (own != null && GetComponentInParent<Canvas>() != null)
            return own;

        Canvas canvas = FindOverlayCanvas();
        if (canvas == null)
            canvas = CreateOverlayCanvas();

        GameObject rootObject = new GameObject("VictoryManager (UI)", typeof(RectTransform));
        RectTransform rect = (RectTransform)rootObject.transform;
        rect.SetParent(canvas.transform, false);

        Debug.Log($"VictoryManager: este objeto no estaba dentro de un Canvas; la interfaz se creó dentro de '{canvas.name}'.", this);
        return rect;
    }

    private static Canvas FindOverlayCanvas()
    {
        foreach (Canvas candidate in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (candidate.isRootCanvas && candidate.renderMode == RenderMode.ScreenSpaceOverlay)
                return candidate;
        }

        return null;
    }

    private static Canvas CreateOverlayCanvas()
    {
        GameObject canvasObject = new GameObject("HUD Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    private void OnEnemyDied(Enemy enemy)
    {
        if (GameWon) return;

        kills++;
        UpdateCounter();

        if (kills >= enemiesToWin)
            Win();
    }

    private void UpdateCounter()
    {
        counterText.text = $"{Mathf.Max(0, enemiesToWin - kills)} restantes para ganar";
    }

    private void Win()
    {
        GameWon = true;
        Time.timeScale = 0f;

        ShowVictoryScreen();
    }

    private void ShowVictoryScreen()
    {
        root.SetAsLastSibling(); // por encima del resto de la interfaz

        // Fondo oscuro que bloquea los clics en lo que hay debajo.
        RectTransform dim = CreateRect("Dim", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        dim.anchorMin = Vector2.zero;
        dim.anchorMax = Vector2.one;
        dim.offsetMin = Vector2.zero;
        dim.offsetMax = Vector2.zero;
        dim.gameObject.AddComponent<Image>().color = dimColor;

        int level = experience != null ? experience.Level : 1;
        int wave = waveManager != null ? waveManager.CurrentWave : 1;
        int totalSeconds = Mathf.FloorToInt(elapsed);

        Vector2 center = new Vector2(0.5f, 0.5f);

        CreateText("Title", center, center, new Vector2(0f, 150f), new Vector2(1200f, 130f),
            victoryTitle, 90, FontStyle.Bold, TextAnchor.MiddleCenter);

        CreateText("Time", center, center, new Vector2(0f, 20f), new Vector2(1000f, 60f),
            $"Tiempo: {totalSeconds / 60:00}:{totalSeconds % 60:00}", 44, FontStyle.Normal, TextAnchor.MiddleCenter);

        CreateText("Level", center, center, new Vector2(0f, -50f), new Vector2(1000f, 60f),
            $"Nivel: {level}", 44, FontStyle.Normal, TextAnchor.MiddleCenter);

        CreateText("Wave", center, center, new Vector2(0f, -120f), new Vector2(1000f, 60f),
            $"Oleada: {wave}", 44, FontStyle.Normal, TextAnchor.MiddleCenter);
    }

    private RectTransform CreateRect(string name, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = (RectTransform)go.transform;

        rect.SetParent(root, false);
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private Text CreateText(string name, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size,
                            string value, int fontSize, FontStyle style, TextAnchor alignment)
    {
        RectTransform rect = CreateRect(name, anchor, pivot, position, size);

        Text text = rect.gameObject.AddComponent<Text>();
        text.font = font;
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = textColor;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;

        // Sombra suave para que se lea sobre cualquier fondo.
        rect.gameObject.AddComponent<Shadow>();

        return text;
    }
}