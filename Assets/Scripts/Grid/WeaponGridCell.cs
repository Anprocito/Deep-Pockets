using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Una celda de la grilla de armas. La crea y configura WeaponGridUI; no hace falta ponerla a mano.
// Permite arrastrar su arma con el mouse hacia otra celda (necesita un EventSystem en la escena).
public class WeaponGridCell : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    private WeaponGridUI owner;
    private Image background;
    private Image icon;
    private Text label;
    private Text counter;
    private Color emptyColor;
    private Color filledColor;

    public int Row { get; private set; }
    public int Column { get; private set; }

    // La columna indica la direccion: 0 = Norte, 1 = Noreste, ... 7 = Noroeste.
    public CardinalDirection Direction => (CardinalDirection)Column;

    public bool IsEmpty { get; private set; } = true;

    // Datos del arma que se muestra (los usa el "fantasma" que sigue al mouse al arrastrar).
    public string WeaponName { get; private set; } = string.Empty;
    public Sprite WeaponIcon { get; private set; }

    // Lo llama WeaponGridUI al crear la celda.
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

    // Ocupa la celda con un arma: pinta el fondo y muestra su nombre (y su icono, si tiene).
    public void SetWeapon(string weaponName, Sprite sprite = null)
    {
        IsEmpty = false;
        WeaponName = weaponName;
        WeaponIcon = sprite;

        background.color = filledColor;
        label.text = weaponName;

        icon.sprite = sprite;
        icon.enabled = sprite != null;
    }

    // Texto pequeno de la esquina inferior derecha (por ejemplo "0/5").
    public void SetCounter(string text)
    {
        counter.text = text;
    }

    // Deja la celda vacia.
    public void Clear()
    {
        IsEmpty = true;
        WeaponName = string.Empty;
        WeaponIcon = null;

        background.color = emptyColor;
        label.text = string.Empty;
        counter.text = string.Empty;
        icon.sprite = null;
        icon.enabled = false;
    }

    // Atenua la celda de origen mientras se arrastra su arma.
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

    // Se llama en la celda sobre la que se suelta el mouse.
    public void OnDrop(PointerEventData eventData)
    {
        WeaponGridCell source = eventData.pointerDrag != null
            ? eventData.pointerDrag.GetComponent<WeaponGridCell>()
            : null;

        owner.Drop(source, this);
    }
}