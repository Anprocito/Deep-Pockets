using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Una casilla de la grilla del inventario. La crea y configura WeaponGridUI; no hace falta ponerla a mano.
// Un objeto de varias casillas pinta todas; el nombre y el contador van en la casilla principal
// (la de arriba a la izquierda). Se puede arrastrar el objeto desde cualquiera de sus casillas.
public class WeaponGridCell : MonoBehaviour, IGridDragSource,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    private WeaponGridUI owner;
    private Image background;
    private Image icon;
    private Text label;
    private Text counter;
    private Color emptyColor;
    private Color filledColor;

    private int offsetColumn;
    private int offsetRow;

    public int Row { get; private set; }
    public int Column { get; private set; }

    // La columna indica la direccion: 0 = Norte, 1 = Noreste, ... 7 = Noroeste.
    public CardinalDirection Direction => (CardinalDirection)Column;

    // Objeto que ocupa esta casilla (null si esta vacia).
    public GridItem Item { get; private set; }
    public bool IsEmpty => Item == null;

    // IGridDragSource
    public InventoryItemSO ItemData => Item != null ? Item.Data : null;
    public GridItem PlacedItem => Item;
    public int GrabColumn => offsetColumn;
    public int GrabRow => offsetRow;

    // Lo llama WeaponGridUI al crear la casilla.
    public void Setup(WeaponGridUI owner, int row, int column, Image background, Image icon, Text label,
                      Text counter, Color emptyColor, Color filledColor)
    {
        this.owner = owner;
        Row = row;
        Column = column;
        this.background = background;
        this.icon = icon;
        this.label = label;
        this.counter = counter;
        this.emptyColor = emptyColor;
        this.filledColor = filledColor;

        Clear();
    }

    // Marca la casilla como parte de un objeto. (offsetColumn, offsetRow) es su posicion dentro del
    // objeto: (0, 0) es la casilla principal.
    public void SetItemPart(GridItem item, int offsetColumn, int offsetRow)
    {
        Item = item;
        this.offsetColumn = offsetColumn;
        this.offsetRow = offsetRow;

        bool isMain = offsetColumn == 0 && offsetRow == 0;

        background.color = filledColor;
        // Si el objeto tiene icono, el nombre sobra: se ve solo el icono (el contador se mantiene).
        label.text = isMain && item.Data.Icon == null ? item.Data.DisplayName : string.Empty;
        counter.text = isMain ? item.CounterText : string.Empty;

        Sprite sprite = isMain ? item.Data.Icon : null;
        icon.sprite = sprite;
        icon.enabled = sprite != null;
    }

    // Texto pequeno de la esquina inferior derecha (por ejemplo "0/5").
    public void SetCounter(string text)
    {
        counter.text = text;
    }

    // Deja la casilla vacia.
    public void Clear()
    {
        Item = null;
        offsetColumn = 0;
        offsetRow = 0;

        background.color = emptyColor;
        label.text = string.Empty;
        counter.text = string.Empty;
        icon.sprite = null;
        icon.enabled = false;
    }

    // Atenua la casilla mientras se arrastra su objeto.
    public void SetDragging(bool dragging)
    {
        Color color = IsEmpty ? emptyColor : filledColor;
        if (dragging) color.a *= 0.35f;
        background.color = color;
    }

    // ----- Arrastrar y soltar -----

    public void OnBeginDrag(PointerEventData eventData)
    {
        owner.BeginDrag(this, eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        owner.Drag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        owner.EndDrag();
    }

    // Se llama en la casilla sobre la que se suelta el mouse.
    public void OnDrop(PointerEventData eventData)
    {
        owner.Drop(eventData.pointerDrag, this);
    }
}