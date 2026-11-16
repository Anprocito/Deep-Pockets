using UnityEngine;
using UnityEngine.EventSystems;

// Objeto que el jugador arrastra desde el panel de colocacion hasta su inventario.
// Lo crea UpgradeUI. Se puede agarrar desde cualquiera de sus casillas.
public class ItemHolder : MonoBehaviour, IGridDragSource, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private InventoryItemSO item;
    private float step;
    private RectTransform rect;
    private CanvasGroup canvasGroup;
    private Vector2 startPosition;
    private Vector2 dragOffset;
    private int grabColumn;
    private int grabRow;

    public InventoryItemSO ItemData => item;
    public GridItem PlacedItem => null; // todavia no esta en la grilla
    public int GrabColumn => grabColumn;
    public int GrabRow => grabRow;

    public void Setup(InventoryItemSO item, float cellSize, float spacing)
    {
        this.item = item;
        step = cellSize + spacing;
        rect = (RectTransform)transform;
        canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        startPosition = rect.anchoredPosition;
        dragOffset = (Vector2)rect.position - eventData.position;

        // Que casilla del objeto se agarro (el pivote esta arriba a la izquierda).
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rect, eventData.pressPosition, eventData.pressEventCamera, out Vector2 local);

        grabColumn = Mathf.Clamp(Mathf.FloorToInt(local.x / step), 0, item.Width - 1);
        grabRow = Mathf.Clamp(Mathf.FloorToInt(-local.y / step), 0, item.Height - 1);

        // Sin esto, el propio objeto taparia a las casillas y no recibirian el "soltar".
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        rect.position = eventData.position + dragOffset;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Vuelve a su sitio. Si se coloco bien, el panel se cierra enseguida.
        canvasGroup.blocksRaycasts = true;
        rect.anchoredPosition = startPosition;
    }
}
