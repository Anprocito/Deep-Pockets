using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Fin de la partida: victoria (al eliminar cierta cantidad de enemigos) y derrota (si muere el jugador).
// Muestra un contador arriba a la izquierda y, al terminar, pausa el juego y muestra la pantalla de
// victoria o de derrota, las dos con un boton para reiniciar el nivel.
// Se puede colocar en cualquier objeto: si no esta dentro de un Canvas, crea su interfaz en el
// Canvas de la escena (o crea uno).
public class VictoryManager : MonoBehaviour
{
    // Estado de la partida. Otros sistemas lo consultan para no abrir mas ventanas al terminar.
    public static bool GameWon { get; private set; }
    public static bool GameLost { get; private set; }
    public static bool GameEnded => GameWon || GameLost;

    [Header("Victoria")]
    [Tooltip("Enemigos que hay que eliminar para ganar.")]
    [Min(1)][SerializeField] private int enemiesToWin = 450;
    [SerializeField] private string victoryTitle = "Ganaste!";

    [Header("Derrota")]
    [SerializeField] private string defeatTitle = "Perdiste!";

    [Header("Boton")]
    [SerializeField] private string restartLabel = "Reiniciar";

    [Header("Contador (esquina superior izquierda)")]
    [Tooltip("Separacion respecto al borde izquierdo (x) y al borde superior (y) de la pantalla.")]
    [SerializeField] private Vector2 margin = new Vector2(20f, 20f);
    [SerializeField] private int counterFontSize = 28;

    [Header("Colores")]
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private Color dimColor = new Color(0f, 0f, 0f, 0.8f);
    [SerializeField] private Color buttonColor = new Color32(52, 58, 92, 255);

    [Header("Referencias (se buscan solas)")]
    [SerializeField] private PlayerExperience experience;
    [SerializeField] private PlayerHealth health;
    [SerializeField] private WaveManager waveManager;

    private RectTransform root; // donde se construye la interfaz
    private Font font;
    private Text counterText;
    private Button restartButton;
    private int kills;
    private float elapsed; // tiempo de juego: no cuenta mientras el juego esta en pausa

    private void Awake()
    {
        GameWon = false;
        GameLost = false;

        root = PrepareRoot();
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Ocupa toda la pantalla, pero no dibuja nada ni bloquea clics hasta que termine la partida.
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

    private void Start()
    {
        if (experience == null) experience = FindFirstObjectByType<PlayerExperience>();
        if (health == null) health = FindFirstObjectByType<PlayerHealth>();
        if (waveManager == null) waveManager = FindFirstObjectByType<WaveManager>();

        // La derrota llega cuando el jugador avisa de que murio.
        if (health != null && health.onDied != null)
            health.onDied.AddListener(OnPlayerDied);
        else
            Debug.LogWarning("VictoryManager: no se encontro PlayerHealth; no habra pantalla de derrota.", this);
    }

    private void OnDestroy()
    {
        if (health != null && health.onDied != null)
            health.onDied.RemoveListener(OnPlayerDied);

        // Si la interfaz se creo en un objeto aparte, se borra junto con este script.
        if (root != null && root.gameObject != gameObject)
            Destroy(root.gameObject);
    }

    private void Update()
    {
        if (!GameEnded)
            elapsed += Time.deltaTime; // con el juego en pausa, deltaTime es 0
    }

    // Si este objeto ya es parte de la UI (tiene Rect Transform y un Canvas arriba), se usa tal cual.
    // Si no, se construye en un objeto nuevo dentro del Canvas de la escena (o de uno creado aqui).
    private RectTransform PrepareRoot()
    {
        RectTransform own = transform as RectTransform;
        if (own != null && GetComponentInParent<Canvas>() != null)
            return own;

        // Solo se usa un Canvas de la escena de este objeto. Al reiniciar el nivel, Unity crea la escena
        // nueva antes de borrar la vieja, y no hay que colgar la interfaz del Canvas de la vieja.
        Scene scene = gameObject.scene;
        Canvas canvas = FindOverlayCanvas(scene);
        if (canvas == null)
            canvas = CreateOverlayCanvas(scene);

        GameObject rootObject = new GameObject("VictoryManager (UI)", typeof(RectTransform));
        RectTransform rect = (RectTransform)rootObject.transform;
        rect.SetParent(canvas.transform, false);

        Debug.Log($"VictoryManager: este objeto no estaba dentro de un Canvas; la interfaz se creó dentro de '{canvas.name}'.", this);
        return rect;
    }

    private static Canvas FindOverlayCanvas(Scene scene)
    {
        foreach (Canvas candidate in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (candidate.gameObject.scene == scene && candidate.isRootCanvas &&
                candidate.renderMode == RenderMode.ScreenSpaceOverlay)
                return candidate;
        }

        return null;
    }

    private static Canvas CreateOverlayCanvas(Scene scene)
    {
        GameObject canvasObject = new GameObject("HUD Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        SceneManager.MoveGameObjectToScene(canvasObject, scene); // que nazca en la escena correcta

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
        if (GameEnded) return;

        kills++;
        UpdateCounter();

        if (kills >= enemiesToWin)
            Win();
    }

    private void OnPlayerDied()
    {
        if (GameEnded) return;

        GameLost = true;
        EndGame(defeatTitle);
    }

    private void UpdateCounter()
    {
        if (counterText == null) return;

        counterText.text = $"{Mathf.Max(0, enemiesToWin - kills)} restantes para ganar";
    }

    private void Win()
    {
        GameWon = true;
        EndGame(victoryTitle);
    }

    private void EndGame(string title)
    {
        Time.timeScale = 0f;
        ShowEndScreen(title);
    }

    // Pantalla final (victoria o derrota): titulo, tiempo, nivel, oleada y boton de reiniciar.
    private void ShowEndScreen(string title)
    {
        if (root == null)
        {
            Debug.LogError("VictoryManager: la interfaz ya no existe, no se puede mostrar la pantalla final.", this);
            return;
        }

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
            title, 90, FontStyle.Bold, TextAnchor.MiddleCenter);

        CreateText("Time", center, center, new Vector2(0f, 20f), new Vector2(1000f, 60f),
            $"Tiempo: {totalSeconds / 60:00}:{totalSeconds % 60:00}", 44, FontStyle.Normal, TextAnchor.MiddleCenter);

        CreateText("Level", center, center, new Vector2(0f, -50f), new Vector2(1000f, 60f),
            $"Nivel: {level}", 44, FontStyle.Normal, TextAnchor.MiddleCenter);

        CreateText("Wave", center, center, new Vector2(0f, -120f), new Vector2(1000f, 60f),
            $"Oleada: {wave}", 44, FontStyle.Normal, TextAnchor.MiddleCenter);

        CreateRestartButton(center);
    }

    private void CreateRestartButton(Vector2 center)
    {
        Vector2 position = new Vector2(0f, -240f);
        Vector2 size = new Vector2(360f, 90f);

        RectTransform buttonRect = CreateRect("RestartButton", center, center, position, size);

        Image background = buttonRect.gameObject.AddComponent<Image>();
        background.color = buttonColor;

        Button button = buttonRect.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        restartButton = button;

        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        colors.highlightedColor = Color.white;
        colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        colors.selectedColor = colors.normalColor;
        button.colors = colors;
        button.onClick.AddListener(RestartLevel);

        // El texto va como objeto aparte encima del boton (sin bloquear los clics).
        CreateText("RestartLabel", center, center, position, size,
            restartLabel, 40, FontStyle.Bold, TextAnchor.MiddleCenter);
    }

    // Reinicia el nivel (tanto desde la victoria como desde la derrota).
    private void RestartLevel()
    {
        // Evita un segundo clic mientras la escena se descarga.
        if (restartButton != null)
            restartButton.interactable = false;

        if (!LevelRestarter.Restart())
        {
            Debug.LogError("VictoryManager: la escena actual no esta en Build Profiles / Build Settings, " +
                           "asi que no se puede reiniciar. Agregala con \"Add Open Scenes\".", this);

            if (restartButton != null)
                restartButton.interactable = true;
        }
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

// Reinicia el nivel de forma segura, sin importar cuantas veces se haga.
// Recargar la escena directamente deja la escena vieja y la nueva vivas a la vez durante un instante:
// los scripts pueden confundir objetos viejos con nuevos. Aqui primero se descarta por completo la escena
// actual (pasando por una escena vacia temporal) y solo despues se carga el nivel de nuevo, asi que
// cuando el nivel arranca no queda ningun objeto de la partida anterior.
public static class LevelRestarter
{
    // Devuelve false si la escena actual no esta en Build Profiles / Build Settings.
    public static bool Restart()
    {
        Scene current = SceneManager.GetActiveScene();
        int sceneIndex = current.buildIndex;

        if (sceneIndex < 0) return false;

        Time.timeScale = 1f; // al terminar la partida el juego quedo en pausa

        // Una escena vacia pasa a ser la activa: asi se puede descargar la escena del juego.
        Scene temporary = SceneManager.CreateScene("RestartTemporary");
        SceneManager.SetActiveScene(temporary);

        AsyncOperation unload = SceneManager.UnloadSceneAsync(current);

        if (unload == null)
        {
            SceneManager.LoadScene(sceneIndex);
            return true;
        }

        // Cuando la escena vieja ya no existe, se carga el nivel limpio (reemplaza a la vacia).
        unload.completed += _ =>
        {
            Enemy.Active.Clear();
            SceneManager.LoadScene(sceneIndex);
        };

        return true;
    }
}