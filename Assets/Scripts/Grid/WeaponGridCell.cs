using UnityEngine;
using UnityEngine.UI;

// Una celda de la grilla de armas. La crea y configura WeaponGridUI; no hace falta ponerla a mano.
public class WeaponGridCell : MonoBehaviour
{
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

    // Lo llama WeaponGridUI al crear la celda.
    public void Setup(int row, int column, Image background, Image icon, Text label, Text counter,
                      Color emptyColor, Color filledColor)
    {
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
        background.color = emptyColor;
        label.text = string.Empty;
        counter.text = string.Empty;
        icon.sprite = null;
        icon.enabled = false;
    }
}