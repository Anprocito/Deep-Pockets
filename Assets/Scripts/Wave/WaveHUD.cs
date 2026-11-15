using UnityEngine;
using UnityEngine.UI;

// Texto de oleada en la esquina inferior izquierda. Se construye por codigo.
// Se coloca en un objeto vacio que este DENTRO de un Canvas.
public class WaveHUD : MonoBehaviour
{
    [Header("Posicion (esquina inferior izquierda)")]
    [Tooltip("Separacion respecto al borde izquierdo (x) y al borde inferior (y) de la pantalla.")]
    [SerializeField] private Vector2 margin = new Vector2(20f, 20f);

    [Header("Texto")]
    [SerializeField] private int titleFontSize = 32;
    [SerializeField] private int subtitleFontSize = 18;
    [SerializeField] private Color textColor = Color.white;

    [Header("Referencias (se busca sola)")]
    [SerializeField] private WaveManager waveManager;

    private Text titleText;
    private Text subtitleText;
    private int shownWave = -1;
    private int shownRemaining = -1;

    private void Awake()
    {
        Build();
    }

    private void Start()
    {
        if (waveManager == null)
            waveManager = FindFirstObjectByType<WaveManager>();

        if (waveManager == null)
            Debug.LogWarning("WaveHUD: no hay WaveManager en la escena.", this);
    }

    private void Update()
    {
        if (waveManager == null || titleText == null) return;

        int wave = waveManager.CurrentWave;
        int remaining = waveManager.EnemiesRemaining;

        // Solo reescribe los textos cuando cambia algo.
        if (wave == shownWave && remaining == shownRemaining) return;

        shownWave = wave;
        shownRemaining = remaining;

        titleText.text = $"Oleada: {wave}";
        subtitleText.text = $"{remaining} Enemigos restantes para la oleada {wave + 1}";
    }

    private void Build()
    {
        RectTransform root = transform as RectTransform;
        if (root == null || GetComponentInParent<Canvas>() == null)
        {
            Debug.LogError("WaveHUD: el objeto debe estar dentro de un Canvas (hijo del Canvas en la Hierarchy).", this);
            return;
        }

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        float titleHeight = titleFontSize * 1.3f;
        float subtitleHeight = subtitleFontSize * 1.3f;

        // Ancla el bloque en la esquina inferior izquierda de la pantalla.
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.zero;
        root.pivot = Vector2.zero;
        root.anchoredPosition = margin;
        root.sizeDelta = new Vector2(600f, titleHeight + subtitleHeight);

        titleText = CreateText("Title", root, font, titleFontSize, FontStyle.Bold, 0f, titleHeight);
        subtitleText = CreateText("Subtitle", root, font, subtitleFontSize, FontStyle.Normal, titleHeight, subtitleHeight);
    }

    // Crea una linea de texto pegada arriba a la izquierda, a "top" pixeles del borde superior.
    private Text CreateText(string name, RectTransform parent, Font font, int fontSize, FontStyle style,
                            float top, float height)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = (RectTransform)go.transform;

        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(0f, -top);
        rect.sizeDelta = new Vector2(0f, height);

        Text text = go.AddComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = textColor;
        text.alignment = TextAnchor.MiddleLeft;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;

        // Sombra suave para que se lea sobre cualquier fondo.
        go.AddComponent<Shadow>();

        return text;
    }
}
