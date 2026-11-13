using UnityEngine;
using UnityEngine.UI;

// Una celda de la grilla de armas. La crea WeaponGridUI; no hace falta ponerla a mano.
// Si el prefab de la celda tiene un hijo llamado "Icon" (con un componente Image),
// se usa automaticamente para mostrar el icono del arma.
public class WeaponGridCell : MonoBehaviour
{
    [SerializeField] private Image icon;

    public int Row { get; private set; }
    public int Column { get; private set; }

    // La columna indica la direccion: 0 = Norte, 1 = Noreste, ... 7 = Noroeste.
    public CardinalDirection Direction => (CardinalDirection)Column;

    private void Awake()
    {
        if (icon == null)
        {
            Transform child = transform.Find("Icon");
            if (child != null)
                icon = child.GetComponent<Image>();
        }

        // La celda empieza vacia.
        if (icon != null)
            icon.enabled = false;
    }

    public void Initialize(int row, int column)
    {
        Row = row;
        Column = column;
    }

    // Muestra el icono de un arma en la celda. Con null, la celda queda vacia.
    public void SetIcon(Sprite sprite)
    {
        if (icon == null) return;

        icon.sprite = sprite;
        icon.enabled = sprite != null;
    }
}
