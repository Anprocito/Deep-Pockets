using UnityEngine;

// Base de todo lo que ocupa casillas en el inventario del jugador (armas y pociones).
// Sirve para que el sistema de mejoras trate igual a cualquier objeto: nombre, icono y tamano.
public abstract class InventoryItemSO : ScriptableObject
{
    public abstract string DisplayName { get; }
    public abstract Sprite Icon { get; }

    // Casillas que ocupa a lo ancho (columnas de la grilla) y a lo alto (filas de la grilla).
    public abstract int Width { get; }
    public abstract int Height { get; }
}
