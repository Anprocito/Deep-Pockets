using UnityEngine;

// Zona de ataque de UNA casilla dibujada en el piso: un sector de anillo alrededor del jugador
// (45 grados de ancho, entre un radio interior y uno exterior). Se ve tambien en el juego final.
// La crea y la actualiza WeaponInventory.
public class WeaponZoneVisual : MonoBehaviour
{
    private const float SectorAngle = 45f;
    private const int Segments = 12;            // lados con los que se dibuja el arco
    private const float FillAlphaFactor = 0.6f; // el relleno es algo mas transparente que el color elegido

    private Material material;
    private Mesh mesh;
    private float builtInnerRadius = -1f;
    private float builtOuterRadius = -1f;

    public void Build()
    {
        Shader shader = Shader.Find("Sprites/Default"); // sin iluminacion, con transparencia
        if (shader == null)
        {
            Debug.LogError("WeaponZoneVisual: no se encontro el shader 'Sprites/Default'.", this);
            return;
        }

        material = new Material(shader);
        mesh = new Mesh { name = "WeaponZoneSector" };

        gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;

        MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = material;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
    }

    // Coloca la zona. "yaw" es la direccion central del sector (0 = Norte, 90 = Este...).
    // La forma solo se vuelve a calcular cuando cambian los radios (por ejemplo, al mover el arma de fila).
    public void Apply(Vector3 position, float yaw, float innerRadius, float outerRadius, Color color)
    {
        if (material == null) return;

        transform.position = position;
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        if (!Mathf.Approximately(innerRadius, builtInnerRadius) || !Mathf.Approximately(outerRadius, builtOuterRadius))
            RebuildMesh(innerRadius, outerRadius);

        color.a *= FillAlphaFactor;
        material.color = color;
    }

    // Sector centrado hacia +Z (Norte), en el plano XZ.
    private void RebuildMesh(float innerRadius, float outerRadius)
    {
        Vector3[] vertices = new Vector3[(Segments + 1) * 2];
        Vector3[] normals = new Vector3[vertices.Length];
        int[] triangles = new int[Segments * 6];

        float startAngle = -SectorAngle * 0.5f;

        for (int i = 0; i <= Segments; i++)
        {
            float angle = (startAngle + SectorAngle * i / Segments) * Mathf.Deg2Rad;
            Vector3 direction = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));

            vertices[i * 2] = direction * innerRadius;
            vertices[i * 2 + 1] = direction * outerRadius;
            normals[i * 2] = Vector3.up;
            normals[i * 2 + 1] = Vector3.up;
        }

        // Una franja de dos triangulos por cada lado del arco.
        for (int i = 0; i < Segments; i++)
        {
            int innerA = i * 2;
            int outerA = innerA + 1;
            int innerB = innerA + 2;
            int outerB = innerA + 3;
            int t = i * 6;

            triangles[t] = innerA;
            triangles[t + 1] = outerA;
            triangles[t + 2] = innerB;
            triangles[t + 3] = innerB;
            triangles[t + 4] = outerA;
            triangles[t + 5] = outerB;
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();

        builtInnerRadius = innerRadius;
        builtOuterRadius = outerRadius;
    }

    private void OnDestroy()
    {
        if (material != null) Destroy(material);
        if (mesh != null) Destroy(mesh);
    }
}