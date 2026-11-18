using UnityEngine;
using UnityEngine.UI;

// HUD del jugador, arriba en el centro de la pantalla: barra de vida, barra de experiencia
// (con el nivel) y numero de bajas. Se construye por codigo.
// Se puede colocar en cualquier objeto: si no esta dentro de un Canvas, crea su interfaz en el
// Canvas de la escena (o crea uno).
public class PlayerHUD : MonoBehaviour
{
    [Header("Posicion (arriba en el centro)")]
    [Tooltip("Separacion respecto al borde superior de la pantalla.")]
    [SerializeField] private float topMargin = 20f;
    [SerializeField] private float barWidth = 520f;

    [Header("Barras")]
    [SerializeField] private float healthBarHeight = 30f;
    [SerializeField] private float experienceBarHeight = 20f;
    [SerializeField] private float spacing = 8f;
    [SerializeField] private Color barBackgroundColor = new Color32(20, 20, 30, 220);
    [SerializeField] private Color healthColor = new Color32(205, 55, 55, 255);
    [SerializeField] private Color experienceColor = new Color32(70, 165, 255, 255);

    [Header("Texto")]
    [SerializeField] private int killsFontSize = 28;
    [SerializeField] private Color textColor = Color.white;

    [Header("Referencias (se buscan solas)")]
    [SerializeField] private PlayerHealth health;
    [SerializeField] private PlayerExperience experience;

    private RectTransform root; // donde se construye la interfaz
    private Font font;
    private RectTransform healthFill;
    private RectTransform experienceFill;
    private Text healthText;
    private Text experienceText;
    private Text killsText;

    private int kills;

    // Ultimos valores mostrados: solo se reescribe lo que cambia.
    private float shownHealthFraction = -1f;
    private int shownHealth = -1;
    private int shownMaxHealth = -1;
    private float shownExperienceFraction = -1f;
    private int shownExperience = -1;
    private int shownRequired = -1;
    private int shownLevel = -1;

    private void Awake()
    {
        root = PrepareRoot();
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        float killsHeight = killsFontSize * 1.4f;
        float totalHeight = healthBarHeight + spacing + experienceBarHeight + spacing + killsHeight;

        // Ancla el bloque arriba en el centro de la pantalla.
        root.anchorMin = new Vector2(0.5f, 1f);
        root.anchorMax = new Vector2(0.5f, 1f);
        root.pivot = new Vector2(0.5f, 1f);
        root.anchoredPosition = new Vector2(0f, -topMargin);
        root.sizeDelta = new Vector2(barWidth, totalHeight);

        CreateBar("HealthBar", 0f, healthBarHeight, healthColor, out healthFill, out healthText);
        CreateBar("ExperienceBar", healthBarHeight + spacing, experienceBarHeight, experienceColor,
            out experienceFill, out experienceText);

        float killsTop = healthBarHeight + spacing + experienceBarHeight + spacing;
        RectTransform killsRect = CreateTopRect("Kills", killsTop, killsHeight);
        killsText = CreateText("Text", killsRect, killsFontSize, FontStyle.Bold);
        Stretch(killsText.rectTransform);
        killsText.text = "Bajas: 0";
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
        if (health == null) health = FindFirstObjectByType<PlayerHealth>();
        if (experience == null) experience = FindFirstObjectByType<PlayerExperience>();

        if (health == null)
            Debug.LogWarning("PlayerHUD: no hay PlayerHealth en la escena; la barra de vida no se actualizara.", this);
        if (experience == null)
            Debug.LogWarning("PlayerHUD: no hay PlayerExperience en la escena; la barra de experiencia no se actualizara.", this);
    }

    private void Update()
    {
        UpdateHealth();
        UpdateExperience();
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

        GameObject rootObject = new GameObject("PlayerHUD (UI)", typeof(RectTransform));
        RectTransform rect = (RectTransform)rootObject.transform;
        rect.SetParent(canvas.transform, false);

        Debug.Log($"PlayerHUD: este objeto no estaba dentro de un Canvas; la interfaz se creó dentro de '{canvas.name}'.", this);
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
        kills++;
        killsText.text = $"Bajas: {kills}";
    }

    private void UpdateHealth()
    {
        if (health == null) return;

        float current = health.CurrentHealth;
        float max = health.MaxHealth;
        float fraction = max > 0f ? Mathf.Clamp01(current / max) : 0f;

        if (!Mathf.Approximately(fraction, shownHealthFraction))
        {
            shownHealthFraction = fraction;
            healthFill.anchorMax = new Vector2(fraction, 1f);
        }

        int currentInt = Mathf.CeilToInt(current);
        int maxInt = Mathf.CeilToInt(max);

        if (currentInt != shownHealth || maxInt != shownMaxHealth)
        {
            shownHealth = currentInt;
            shownMaxHealth = maxInt;
            healthText.text = $"{currentInt} / {maxInt}";
        }
    }

    private void UpdateExperience()
    {
        if (experience == null) return;

        float current = experience.CurrentExperience;
        int required = experience.ExperienceRequired;
        int level = experience.Level;
        float fraction = required > 0 ? Mathf.Clamp01(current / required) : 0f;

        if (!Mathf.Approximately(fraction, shownExperienceFraction))
        {
            shownExperienceFraction = fraction;
            experienceFill.anchorMax = new Vector2(fraction, 1f);
        }

        int currentInt = Mathf.FloorToInt(current);

        if (currentInt != shownExperience || required != shownRequired || level != shownLevel)
        {
            shownExperience = currentInt;
            shownRequired = required;
            shownLevel = level;
            experienceText.text = $"Nivel {level}  ·  {currentInt} / {required}";
        }
    }

    // Barra: fondo oscuro + relleno que crece de izquierda a derecha + texto centrado.
    private void CreateBar(string name, float top, float height, Color fillColor,
                           out RectTransform fill, out Text label)
    {
        RectTransform background = CreateTopRect(name, top, height);
        Image backgroundImage = background.gameObject.AddComponent<Image>();
        backgroundImage.color = barBackgroundColor;
        backgroundImage.raycastTarget = false;

        // El relleno ocupa todo el fondo; se acorta moviendo su ancla derecha (de 0 a 1).
        GameObject fillObject = new GameObject("Fill", typeof(RectTransform));
        fill = (RectTransform)fillObject.transform;
        fill.SetParent(background, false);
        Stretch(fill);

        Image fillImage = fillObject.AddComponent<Image>();
        fillImage.color = fillColor;
        fillImage.raycastTarget = false;

        label = CreateText("Label", background, Mathf.RoundToInt(height * 0.7f), FontStyle.Bold);
        Stretch(label.rectTransform);
    }

    // Rectangulo pegado arriba, que ocupa todo el ancho del bloque, a "top" pixeles del borde superior.
    private RectTransform CreateTopRect(string name, float top, float height)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = (RectTransform)go.transform;

        rect.SetParent(root, false);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -top);
        rect.sizeDelta = new Vector2(0f, height);
        return rect;
    }

    // Hace que el rect llene a su padre.
    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    // Crea un texto como hijo nuevo de "parent".
    private Text CreateText(string name, Transform parent, int fontSize, FontStyle style)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        Text text = go.AddComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = textColor;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;

        // Sombra suave para que se lea sobre cualquier fondo.
        go.AddComponent<Shadow>();

        return text;
    }
}