using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Las 8 direcciones, en sentido horario. Cada columna de la grilla es una direccion.
public enum CardinalDirection
{
    North,      // 0
    NorthEast,  // 1
    East,       // 2
    SouthEast,  // 3
    South,      // 4
    SouthWest,  // 5
    West,       // 6
    NorthWest   // 7
}

// Construye por codigo la grilla de armas: titulos de columna + 8 columnas x N filas de celdas,
// anclada en la esquina inferior derecha de la pantalla.
// Se coloca en un objeto vacio que este DENTRO de un Canvas. No necesita prefabs.
public class WeaponGridUI : MonoBehaviour
{
    public const int Columns = 8;

    [Header("Grilla")]
    [Min(1)]
    [SerializeField] private int rows = 4;
    [SerializeField] private float cellSize = 64f;
    [SerializeField] private float spacing = 6f;

    [Header("Posicion (esquina inferior derecha)")]
    [Tooltip("Separacion respecto al borde derecho (x) y al borde inferior (y) de la pantalla.")]
    [SerializeField] private Vector2 margin = new Vector2(20f, 20f);

    [Header("Titulos de columna")]
    [SerializeField] private float headerHeight = 28f;
    [SerializeField] private string[] columnLabels = { "N", "NE", "E", "SE", "S", "SO", "O", "NO" };

    [Header("Colores")]
    [SerializeField] private Color emptyCellColor = new Color32(28, 30, 45, 255);
    [SerializeField] private Color filledCellColor = new Color32(160, 220, 205, 255);
    [SerializeField] private Color headerTextColor = new Color32(190, 190, 190, 255);
    [SerializeField] private Color cellTextColor = new Color32(40, 40, 40, 255);

    private WeaponGridCell[,] cells;
    private Font font;

    public int Rows => rows;

    private void Awake()
    {
        Build();
    }

    private void Build()
    {
        RectTransform root = transform as RectTransform;
        if (root == null || GetComponentInParent<Canvas>() == null)
        {
            Debug.LogError("WeaponGridUI: el objeto debe estar dentro de un Canvas (hijo del Canvas en la Hierarchy).", this);
            return;
        }

        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Si el objeto tiene un Layout Group (por ejemplo, el Grid Layout Group de antes),
        // se desactiva: aqui las posiciones se calculan por codigo.
        if (TryGetComponent(out LayoutGroup layout))
            layout.enabled = false;

        // Borra lo que hubiera dentro (celdas de prueba puestas a mano, etc.).
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);

        float step = cellSize + spacing;
        float totalWidth = Columns * cellSize + (Columns - 1) * spacing;
        float totalHeight = headerHeight + spacing + rows * cellSize + (rows - 1) * spacing;

        // Ancla la grilla en la esquina inferior derecha de la pantalla.
        root.anchorMin = new Vector2(1f, 0f);
        root.anchorMax = new Vector2(1f, 0f);
        root.pivot = new Vector2(1f, 0f);
        root.anchoredPosition = new Vector2(-margin.x, margin.y);
        root.sizeDelta = new Vector2(totalWidth, totalHeight);

        // Titulos de columna
        for (int col = 0; col < Columns; col++)
        {
            string text = col < columnLabels.Length ? columnLabels[col] : ((CardinalDirection)col).ToString();

            CreateText($"Header_{(CardinalDirection)col}", transform,
                new Vector2(col * step, 0f), new Vector2(cellSize, headerHeight),
                TextAnchor.MiddleCenter, 18, headerTextColor).text = text;
        }

        // Celdas (filas de arriba abajo, columnas de izquierda a derecha)
        cells = new WeaponGridCell[rows, Columns];
        float gridTop = headerHeight + spacing;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < Columns; col++)
            {
                Vector2 pos = new Vector2(col * step, -(gridTop + row * step));
                cells[row, col] = CreateCell(row, col, pos);
            }
        }
    }

    private WeaponGridCell CreateCell(int row, int col, Vector2 position)
    {
        RectTransform cellRect = CreateRect($"Cell_R{row}_{(CardinalDirection)col}", transform,
            position, new Vector2(cellSize, cellSize));

        Image background = cellRect.gameObject.AddComponent<Image>();

        // Icono (oculto hasta que un arma tenga sprite)
        RectTransform iconRect = CreateRect("Icon", cellRect, Vector2.zero, Vector2.zero);
        Stretch(iconRect, 6f);
        Image icon = iconRect.gameObject.AddComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        // Nombre del arma, centrado
        Text label = CreateText("Label", cellRect, Vector2.zero, Vector2.zero,
            TextAnchor.MiddleCenter, 14, cellTextColor);
        Stretch(label.rectTransform, 4f);
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = 8;
        label.resizeTextMaxSize = 14;

        // Contador, abajo a la derecha
        Text counter = CreateText("Counter", cellRect, Vector2.zero, Vector2.zero,
            TextAnchor.LowerRight, 11, cellTextColor);
        Stretch(counter.rectTransform, 3f);

        WeaponGridCell cell = cellRect.gameObject.AddComponent<WeaponGridCell>();
        cell.Setup(this, row, col, background, icon, label, counter, emptyCellColor, filledCellColor);
        return cell;
    }

    // Crea un RectTransform anclado arriba a la izquierda de su padre.
    private static RectTransform CreateRect(string name, Transform parent, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = (RectTransform)go.transform;

        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
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

    private Text CreateText(string name, Transform parent, Vector2 anchoredPosition, Vector2 size,
                            TextAnchor alignment, int fontSize, Color color)
    {
        RectTransform rect = CreateRect(name, parent, anchoredPosition, size);

        Text text = rect.gameObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.raycastTarget = false;
        return text;
    }

    // ----- Arrastrar y soltar con el mouse -----

    // Se lanza al soltar un arma sobre otra celda: (celda de origen, celda de destino).
    // Lo escucha WeaponInventory, que decide si el movimiento es valido.
    public event System.Action<WeaponGridCell, WeaponGridCell> DropRequested;

    private WeaponGridCell dragSource;
    private RectTransform dragGhost;

    public void BeginDrag(WeaponGridCell source, PointerEventData eventData)
    {
        if (source.IsEmpty) return; // las celdas vacias no se arrastran

        dragSource = source;
        source.SetDragging(true);

        // "Fantasma" del arma que sigue al mouse, dibujado encima de todo el Canvas.
        Canvas canvas = GetComponentInParent<Canvas>().rootCanvas;
        RectTransform ghost = CreateRect("DragGhost", canvas.transform, Vector2.zero, new Vector2(cellSize, cellSize));
        ghost.pivot = new Vector2(0.5f, 0.5f);

        Image ghostImage = ghost.gameObject.AddComponent<Image>();
        Color ghostColor = filledCellColor;
        ghostColor.a = 0.85f;
        ghostImage.color = ghostColor;
        ghostImage.raycastTarget = false; // para que no tape a la celda sobre la que se suelta

        if (source.WeaponIcon != null)
        {
            RectTransform iconRect = CreateRect("Icon", ghost, Vector2.zero, Vector2.zero);
            Stretch(iconRect, 6f);
            Image iconImage = iconRect.gameObject.AddComponent<Image>();
            iconImage.sprite = source.WeaponIcon;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
        }

        Text label = CreateText("Label", ghost, Vector2.zero, Vector2.zero,
            TextAnchor.MiddleCenter, 14, cellTextColor);
        Stretch(label.rectTransform, 4f);
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = 8;
        label.resizeTextMaxSize = 14;
        label.text = source.WeaponName;

        dragGhost = ghost;
        ghost.position = eventData.position;
    }

    public void Drag(PointerEventData eventData)
    {
        if (dragGhost != null)
            dragGhost.position = eventData.position;
    }

    public void Drop(WeaponGridCell source, WeaponGridCell target)
    {
        if (source == null || target == null || source == target) return;

        DropRequested?.Invoke(source, target);
    }

    // Se llama siempre al terminar el arrastre, se haya soltado sobre una celda o no.
    public void EndDrag()
    {
        if (dragSource != null)
        {
            dragSource.SetDragging(false);
            dragSource = null;
        }

        if (dragGhost != null)
        {
            Destroy(dragGhost.gameObject);
            dragGhost = null;
        }
    }

    // Celda por fila (0 = fila de arriba) y columna (0 a 7).
    public WeaponGridCell GetCell(int row, int column)
    {
        if (cells == null || row < 0 || row >= rows || column < 0 || column >= Columns)
        {
            Debug.LogWarning($"WeaponGridUI: la celda ({row}, {column}) no existe.", this);
            return null;
        }

        return cells[row, column];
    }

    // Celda por direccion y fila. Ejemplo: GetCell(CardinalDirection.East, 2)
    public WeaponGridCell GetCell(CardinalDirection direction, int row)
    {
        return GetCell(row, (int)direction);
    }

    // Todas las celdas de una direccion (una columna), de arriba abajo.
    public WeaponGridCell[] GetColumn(CardinalDirection direction)
    {
        WeaponGridCell[] column = new WeaponGridCell[rows];
        for (int row = 0; row < rows; row++)
            column[row] = cells[row, (int)direction];

        return column;
    }
}