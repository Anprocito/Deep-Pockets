using System;
using System.Collections.Generic;
using UnityEngine;

// Modelo del inventario del jugador: que objeto ocupa cada casilla de la grilla.
// Se coloca en el jugador. Lo usan WeaponInventory, PotionInventory y el sistema de mejoras.
public class InventoryGrid : MonoBehaviour
{
    [Tooltip("Grilla de la UI. Si se deja vacia, se busca en la escena.")]
    [SerializeField] private WeaponGridUI gridUI;

    private readonly List<GridItem> items = new List<GridItem>();
    private GridItem[,] occupancy; // [fila, columna]

    // Se lanza cuando se coloca un objeto nuevo en la grilla.
    public event Action<GridItem> ItemAdded;

    // Se lanza al soltar sobre la grilla un objeto nuevo (que todavia no esta en el inventario):
    // el objeto y la casilla donde quedaria su esquina superior izquierda.
    public event Action<InventoryItemSO, int, int> NewItemDropped;

    public int Rows => gridUI != null ? gridUI.Rows : 0;
    public int Columns => WeaponGridUI.Columns;

    private void Awake()
    {
        if (gridUI == null)
            gridUI = FindFirstObjectByType<WeaponGridUI>();

        if (gridUI == null)
            Debug.LogError("InventoryGrid: no hay WeaponGridUI en la escena.", this);
    }

    private void Start()
    {
        if (gridUI != null)
            gridUI.DropRequested += OnDropRequested;
    }

    private void OnDestroy()
    {
        if (gridUI != null)
            gridUI.DropRequested -= OnDropRequested;
    }

    private void EnsureOccupancy()
    {
        if (occupancy == null && gridUI != null)
            occupancy = new GridItem[gridUI.Rows, Columns];
    }

    // Indica si un objeto de ese tamano cabe con su esquina superior izquierda en esa casilla.
    // "ignore" sirve para mover un objeto: sus propias casillas cuentan como libres.
    public bool CanPlace(int column, int row, int width, int height, GridItem ignore = null)
    {
        EnsureOccupancy();
        if (occupancy == null || width < 1 || height < 1) return false;

        // Un objeto no puede salirse de la grilla (tampoco dar la vuelta de Noroeste a Norte).
        if (column < 0 || row < 0 || column + width > Columns || row + height > Rows) return false;

        for (int r = row; r < row + height; r++)
        {
            for (int c = column; c < column + width; c++)
            {
                GridItem occupant = occupancy[r, c];
                if (occupant != null && occupant != ignore) return false;
            }
        }

        return true;
    }

    // Busca el primer hueco libre (de arriba abajo y de izquierda a derecha) para un objeto de ese tamano.
    public bool TryFindSpace(int width, int height, out int column, out int row)
    {
        for (int r = 0; r < Rows; r++)
        {
            for (int c = 0; c < Columns; c++)
            {
                if (CanPlace(c, r, width, height))
                {
                    column = c;
                    row = r;
                    return true;
                }
            }
        }

        column = -1;
        row = -1;
        return false;
    }

    public bool HasSpaceFor(int width, int height)
    {
        return TryFindSpace(width, height, out _, out _);
    }

    // Coloca un objeto nuevo. Devuelve null si no cabe.
    public GridItem Place(InventoryItemSO data, int column, int row)
    {
        if (data == null || !CanPlace(column, row, data.Width, data.Height)) return null;

        GridItem item = new GridItem(data, column, row);
        items.Add(item);
        Occupy(item);
        RefreshCells(item);

        ItemAdded?.Invoke(item);
        return item;
    }

    // Saca un objeto de la grilla y libera sus casillas.
    public void Remove(GridItem item)
    {
        if (item == null || !items.Remove(item)) return;

        Vacate(item);
    }

    // Mueve un objeto ya colocado. Devuelve false si en ese lugar no cabe.
    public bool TryMove(GridItem item, int column, int row)
    {
        if (item == null || !CanPlace(column, row, item.Width, item.Height, item)) return false;

        Vacate(item);
        item.Column = column;
        item.Row = row;
        Occupy(item);
        RefreshCells(item);
        return true;
    }

    // Cambia el texto pequeno de la casilla principal del objeto.
    public void SetCounter(GridItem item, string text)
    {
        item.CounterText = text;

        WeaponGridCell cell = gridUI.GetCell(item.Row, item.Column);
        if (cell != null)
            cell.SetCounter(text);
    }

    private void Occupy(GridItem item)
    {
        for (int r = item.Row; r < item.Row + item.Height; r++)
            for (int c = item.Column; c < item.Column + item.Width; c++)
                occupancy[r, c] = item;
    }

    private void Vacate(GridItem item)
    {
        for (int r = item.Row; r < item.Row + item.Height; r++)
        {
            for (int c = item.Column; c < item.Column + item.Width; c++)
            {
                if (occupancy[r, c] == item)
                    occupancy[r, c] = null;

                WeaponGridCell cell = gridUI.GetCell(r, c);
                if (cell != null)
                    cell.Clear();
            }
        }
    }

    // Pinta en la UI todas las casillas que ocupa el objeto.
    private void RefreshCells(GridItem item)
    {
        for (int dr = 0; dr < item.Height; dr++)
        {
            for (int dc = 0; dc < item.Width; dc++)
            {
                WeaponGridCell cell = gridUI.GetCell(item.Row + dr, item.Column + dc);
                if (cell != null)
                    cell.SetItemPart(item, dc, dr);
            }
        }
    }

    // Se suelta algo sobre una casilla de la grilla.
    private void OnDropRequested(IGridDragSource source, WeaponGridCell target)
    {
        // La esquina superior izquierda es la casilla soltada menos la casilla que se agarro.
        int column = target.Column - source.GrabColumn;
        int row = target.Row - source.GrabRow;

        if (source.PlacedItem != null)
            TryMove(source.PlacedItem, column, row); // si no cabe, el objeto se queda donde estaba
        else
            NewItemDropped?.Invoke(source.ItemData, column, row);
    }
}
