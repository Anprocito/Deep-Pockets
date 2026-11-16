using UnityEngine;
using UnityEngine.UI;

// Una opcion de una ventana de eleccion (una mejora o un objeto).
public struct ChoiceOption
{
    public string title;
    public string description;
    public Sprite icon;
    public int shapeWidth;  // si es mayor que 0, dibuja la forma del objeto (casillas que ocupa)
    public int shapeHeight;
}

// Ventanas del sistema de mejoras, construidas por codigo:
//   - ventana de eleccion (3 tarjetas con fondo oscuro que bloquea el resto de la pantalla)
//   - ventana de colocacion (el objeto elegido, para arrastrarlo a la grilla del inventario)
// Se coloca en un objeto vacio DENTRO del Canvas.
public class UpgradeUI : MonoBehaviour
{
    [Header("Colores")]
    [SerializeField] private Color dimColor = new Color(0f, 0f, 0f, 0.75f);
    [SerializeField] private Color panelColor = new Color32(24, 26, 40, 240);
    [SerializeField] private Color cardColor = new Color32(52, 58, 92, 255);
    [SerializeField] private Color itemColor = new Color32(160, 220, 205, 255);
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private Color itemTextColor = new Color32(40, 40, 40, 255);

    private Font font;
    private WeaponGridUI gridUI;
    private RectTransform content;
    private Text placementMessage;

    private void Awake()
    {
        RectTransform root = transform as RectTransform;
        if (root == null || GetComponentInParent<Canvas>() == null)
        {
            Debug.LogError("UpgradeUI: el objeto debe estar dentro de un Canvas (hijo del Canvas en la Hierarchy).", this);
            return;
        }

        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        gridUI = FindFirstObjectByType<WeaponGridUI>();

        // Ocupa toda la pantalla, pero no dibuja nada ni bloquea clics mientras no se muestre una ventana.
        Stretch(root, 0f);

        GameObject contentObject = new GameObject("Content", typeof(RectTransform));
        content = (RectTransform)contentObject.transform;
        content.SetParent(root, false);
        Stretch(content, 0f);
    }

    // Ventana con varias tarjetas. Al pulsar una, avisa con su posicion en la lista.
    public void ShowChoice(string title, ChoiceOption[] options, System.Action<int> onChosen)
    {
        if (content == null) return;

        Clear();
        transform.SetAsLastSibling(); // por encima del HUD y de la grilla

        // Fondo oscuro que bloquea los clics en todo lo que hay debajo.
        RectTransform dim = CreateRect("Dim", content, Vector2.zero, Vector2.zero);
        Stretch(dim, 0f);
        dim.gameObject.AddComponent<Image>().color = dimColor;

        CreateText("Title", content, title, 40, FontStyle.Bold, textColor, TextAnchor.MiddleCenter,
            new Vector2(0f, 250f), new Vector2(1400f, 70f));

        Vector2 cardSize = new Vector2(280f, 340f);
        float gap = 40f;
        float totalWidth = options.Length * cardSize.x + (options.Length - 1) * gap;

        for (int i = 0; i < options.Length; i++)
        {
            int chosenIndex = i;
            float x = -totalWidth * 0.5f + cardSize.x * 0.5f + i * (cardSize.x + gap);

            CreateCard(options[i], new Vector2(x, -20f), cardSize, () => onChosen(chosenIndex));
        }
    }

    // Ventana para colocar un objeto: el jugador lo arrastra hasta su inventario.
    // No tiene fondo oscuro, para que la grilla siga visible y se pueda usar.
    public void ShowPlacement(InventoryItemSO item, string message)
    {
        if (content == null) return;

        Clear();
        transform.SetAsLastSibling();

        float cell = gridUI != null ? gridUI.CellSize : 64f;
        float spacing = gridUI != null ? gridUI.Spacing : 6f;
        float itemWidth = item.Width * cell + (item.Width - 1) * spacing;
        float itemHeight = item.Height * cell + (item.Height - 1) * spacing;

        RectTransform panel = CreateRect("Panel", content, new Vector2(0f, 60f), new Vector2(700f, 420f));
        panel.gameObject.AddComponent<Image>().color = panelColor;

        CreateText("Title", panel, "Arrastra el objeto a tu inventario", 34, FontStyle.Bold, textColor,
            TextAnchor.MiddleCenter, new Vector2(0f, 165f), new Vector2(660f, 60f));

        // El objeto, centrado en el panel. Pivote arriba a la izquierda para calcular que casilla se agarra.
        RectTransform holder = CreateRect("Item", panel, Vector2.zero, new Vector2(itemWidth, itemHeight));
        holder.pivot = new Vector2(0f, 1f);
        holder.anchoredPosition = new Vector2(-itemWidth * 0.5f, itemHeight * 0.5f);

        holder.gameObject.AddComponent<Image>().color = itemColor;

        if (item.Icon != null)
        {
            RectTransform iconRect = CreateRect("Icon", holder, Vector2.zero, Vector2.zero);
            Stretch(iconRect, 6f);
            Image iconImage = iconRect.gameObject.AddComponent<Image>();
            iconImage.sprite = item.Icon;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
        }

        Text label = CreateText("Label", holder, item.DisplayName, 20, FontStyle.Bold, itemTextColor,
            TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero);
        Stretch(label.rectTransform, 4f);
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = 8;
        label.resizeTextMaxSize = 20;

        holder.gameObject.AddComponent<ItemHolder>().Setup(item, cell, spacing);

        placementMessage = CreateText("Message", panel, message, 24, FontStyle.Normal, textColor,
            TextAnchor.MiddleCenter, new Vector2(0f, -165f), new Vector2(660f, 70f));
    }

    public void SetPlacementMessage(string message)
    {
        if (placementMessage != null)
            placementMessage.text = message;
    }

    // Cierra la ventana que haya abierta.
    public void Hide()
    {
        if (content != null)
            Clear();
    }

    private void Clear()
    {
        placementMessage = null;

        for (int i = content.childCount - 1; i >= 0; i--)
        {
            // Se desactiva ya mismo (Destroy actua al final del frame) para que no reciba mas clics.
            GameObject child = content.GetChild(i).gameObject;
            child.SetActive(false);
            Destroy(child);
        }
    }

    private void CreateCard(ChoiceOption option, Vector2 position, Vector2 size, System.Action onClick)
    {
        RectTransform card = CreateRect("Card", content, position, size);

        Image background = card.gameObject.AddComponent<Image>();
        background.color = cardColor;

        Button button = card.gameObject.AddComponent<Button>();
        button.targetGraphic = background;

        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        colors.highlightedColor = Color.white;
        colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        colors.selectedColor = colors.normalColor;
        button.colors = colors;
        button.onClick.AddListener(() => onClick());

        Text nameText = CreateText("Name", card, option.title, 30, FontStyle.Bold, textColor,
            TextAnchor.MiddleCenter, new Vector2(0f, size.y * 0.5f - 50f), new Vector2(size.x - 24f, 70f));
        nameText.resizeTextForBestFit = true;
        nameText.resizeTextMinSize = 16;
        nameText.resizeTextMaxSize = 30;

        if (option.icon != null)
        {
            RectTransform iconRect = CreateRect("Icon", card, new Vector2(0f, 50f), new Vector2(100f, 100f));
            Image iconImage = iconRect.gameObject.AddComponent<Image>();
            iconImage.sprite = option.icon;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
        }

        if (option.shapeWidth > 0 && option.shapeHeight > 0)
            DrawShape(card, option.shapeWidth, option.shapeHeight, new Vector2(0f, -30f));

        CreateText("Description", card, option.description, 22, FontStyle.Normal, textColor,
            TextAnchor.MiddleCenter, new Vector2(0f, -size.y * 0.5f + 70f), new Vector2(size.x - 30f, 110f));
    }

    // Dibuja las casillas que ocupa un objeto (por ejemplo, 2x1 son dos cuadrados juntos).
    private void DrawShape(RectTransform parent, int width, int height, Vector2 center)
    {
        const float square = 30f;
        const float gap = 6f;

        float totalWidth = width * square + (width - 1) * gap;
        float totalHeight = height * square + (height - 1) * gap;

        for (int r = 0; r < height; r++)
        {
            for (int c = 0; c < width; c++)
            {
                Vector2 pos = center + new Vector2(
                    -totalWidth * 0.5f + square * 0.5f + c * (square + gap),
                    totalHeight * 0.5f - square * 0.5f - r * (square + gap));

                RectTransform cell = CreateRect("ShapeCell", parent, pos, new Vector2(square, square));
                Image image = cell.gameObject.AddComponent<Image>();
                image.color = itemColor;
                image.raycastTarget = false;
            }
        }
    }

    // Crea un RectTransform centrado en su padre (anclas y pivote en el centro).
    private static RectTransform CreateRect(string name, Transform parent, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = (RectTransform)go.transform;

        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        return rect;
    }

    // Hace que el rect llene a su padre, con un margen interior.
    private static void Stretch(RectTransform rect, float padding)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(padding, padding);
        rect.offsetMax = new Vector2(-padding, -padding);
    }

    private Text CreateText(string name, Transform parent, string value, int fontSize, FontStyle style,
                            Color color, TextAnchor alignment, Vector2 anchoredPosition, Vector2 size)
    {
        RectTransform rect = CreateRect(name, parent, anchoredPosition, size);

        Text text = rect.gameObject.AddComponent<Text>();
        text.font = font;
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.raycastTarget = false;
        return text;
    }
}
