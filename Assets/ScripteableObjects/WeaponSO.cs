using UnityEngine;

// Datos de un arma. Cada arma (daga, espada, etc.) es un asset distinto con sus propios valores.
// Crear con: clic derecho en Project > Create > Weapons > Weapon
[CreateAssetMenu(fileName = "NewWeapon", menuName = "Weapons/Weapon")]
public class WeaponSO : ScriptableObject
{
    [Header("General")]
    [Tooltip("Nombre que se muestra en la celda de la grilla.")]
    public string weaponName = "Daga";
    public Sprite icon;

    [Header("Tamano en la grilla")]
    [Tooltip("Celdas que ocupa a lo ancho (columnas de la grilla). Daga 1, espada 2, lanza 3, martillo 2, vara 1.")]
    [Min(1)] public int width = 1;
    [Tooltip("Celdas que ocupa a lo alto (filas de la grilla). Daga 1, espada 1, lanza 1, martillo 2, vara 1.")]
    [Min(1)] public int height = 1;

    [Header("Ataque normal")]
    [Min(0f)] public float baseDamage = 20f;
    [Tooltip("Segundos entre ataques.")]
    [Min(0.05f)] public float attackInterval = 1f;
    [Tooltip("Radio de la zona de ataque. La posicion de la zona depende de la celda donde este el arma.")]
    [Min(0.1f)] public float attackRadius = 2f;
    [Tooltip("Cuantos enemigos golpea cada ataque (los mas cercanos primero). 0 = todos los que esten en rango.")]
    [Min(0)] public int maxTargets = 1;

    [Header("Habilidad (ataque potenciado)")]
    [Tooltip("Ataques normales necesarios para cargar la habilidad.")]
    [Min(1)] public int attacksForSkill = 5;
    [Tooltip("Multiplicador sobre el dano base. 2 = el doble.")]
    [Min(0f)] public float skillDamageMultiplier = 2f;
    [Tooltip("Cuantos enemigos golpea la habilidad. 0 = todos los que esten en la zona (dano en area).")]
    [Min(0)] public int skillMaxTargets = 0;
}