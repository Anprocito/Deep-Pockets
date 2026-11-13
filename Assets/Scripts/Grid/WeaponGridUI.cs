using UnityEngine;
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

// Genera la grilla de armas en la UI: 8 columnas (una por direccion) x N filas (4 por defecto).
// Se coloca en el objeto "WeaponGrid" que esta dentro del Canvas.
// Al agregarlo, Unity anade solo el Grid Layout Group.
[RequireComponent(typeof(GridLayoutGroup))]
public class WeaponGridUI : MonoBehaviour
{
    public const int Columns = 8;

    [Header("Configuracion")]
    [Tooltip("Prefab de una celda (una Image, con un hijo opcional llamado 'Icon').")]
    [SerializeField] private GameObject cellPrefab;
    [Min(1)]
    [SerializeField] private int rows = 4;

    private WeaponGridCell[,] cells;

    public int Rows => rows;

    private void Awake()
    {
        Generate();
    }

    private void Generate()
    {
        if (cellPrefab == null)
        {
            Debug.LogError("WeaponGridUI: falta asignar el prefab de la celda.", this);
            return;
        }

        // Deja el Grid Layout Group listo para 8 columnas, de izquierda a derecha y de arriba abajo.
        GridLayoutGroup grid = GetComponent<GridLayoutGroup>();
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = Columns;

        // Borra lo que hubiera dentro (por ejemplo, una celda de prueba puesta a mano).
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);

        cells = new WeaponGridCell[rows, Columns];

        // Se crean por filas, de izquierda a derecha: asi el orden coincide con el del Grid Layout Group.
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < Columns; col++)
            {
                GameObject instance = Instantiate(cellPrefab, transform);
                instance.name = $"Cell_R{row}_{(CardinalDirection)col}";

                if (!instance.TryGetComponent(out WeaponGridCell cell))
                    cell = instance.AddComponent<WeaponGridCell>();

                cell.Initialize(row, col);
                cells[row, col] = cell;
            }
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
