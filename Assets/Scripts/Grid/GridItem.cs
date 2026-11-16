// Un objeto que ya esta colocado en la grilla del inventario (arma o pocion).
public class GridItem
{
    public InventoryItemSO Data { get; }

    // Casilla de la esquina superior izquierda del objeto.
    public int Column { get; set; }
    public int Row { get; set; }

    public int Width => Data.Width;
    public int Height => Data.Height;

    // Texto pequeno que se muestra en la casilla principal (por ejemplo "3/5" o "8s").
    public string CounterText { get; set; } = string.Empty;

    // Estado en tiempo de ejecucion, solo si el objeto es un arma.
    public EquippedWeapon Weapon { get; set; }

    public GridItem(InventoryItemSO data, int column, int row)
    {
        Data = data;
        Column = column;
        Row = row;
    }
}

// Algo que se puede arrastrar sobre la grilla: una casilla ocupada o un objeto nuevo
// que todavia no esta en el inventario.
public interface IGridDragSource
{
    InventoryItemSO ItemData { get; }

    // Objeto ya colocado en la grilla, o null si es un objeto nuevo.
    GridItem PlacedItem { get; }

    // Casilla del objeto (contada desde su esquina superior izquierda) que se agarro con el mouse.
    int GrabColumn { get; }
    int GrabRow { get; }
}
