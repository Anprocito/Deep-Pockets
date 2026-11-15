using UnityEngine;

// Zona de ataque de un arma dibujada en el piso: un disco translucido con un anillo.
// A diferencia de los Gizmos, se ve tambien en el juego final.
// La crea y la actualiza WeaponInventory.
public class WeaponZoneVisual : MonoBehaviour
{
    private const int Segments = 48;
    private const float DiscAlphaFactor = 0.35f; // el disco es mas transparente que el anillo

    private Material discMaterial;
    private Material ringMaterial;
    private Mesh discMesh;
    private LineRenderer ring;

    public void Build(float radius, float ringWidth)
    {
        Shader shader = Shader.Find("Sprites/Default"); // sin iluminacion, con transparencia
        if (shader == null)
        {
            Debug.LogError("WeaponZoneVisual: no se encontro el shader 'Sprites/Default'.", this);
            return;
        }

        discMaterial = new Material(shader);
        ringMaterial = new Material(shader);

        // Disco
        discMesh = BuildDiscMesh(radius);
        gameObject.AddComponent<MeshFilter>().sharedMesh = discMesh;

        MeshRenderer discRenderer = gameObject.AddComponent<MeshRenderer>();
        discRenderer.sharedMaterial = discMaterial;
        discRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        discRenderer.receiveShadows = false;

        // Anillo (borde del disco), un poco por encima para que no se mezcle con el disco
        GameObject ringObject = new GameObject("Ring");
        ringObject.transform.SetParent(transform, false);
        ringObject.transform.localPosition = Vector3.up * 0.005f;

        ring = ringObject.AddComponent<LineRenderer>();
        ring.useWorldSpace = false;
        ring.loop = true;
        ring.positionCount = Segments;
        ring.widthMultiplier = ringWidth;
        ring.sharedMaterial = ringMaterial;
        ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ring.receiveShadows = false;

        for (int i = 0; i < Segments; i++)
        {
            float angle = i / (float)Segments * Mathf.PI * 2f;
            ring.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
        }
    }

    // Coloca la zona y le pone el color del momento (en reposo o destello de ataque/habilidad).
    public void Apply(Vector3 position, Color color)
    {
        if (discMaterial == null || ring == null) return;

        transform.position = position;

        Color discColor = color;
        discColor.a *= DiscAlphaFactor;
        discMaterial.color = discColor;

        ring.startColor = color;
        ring.endColor = color;
    }

    private static Mesh BuildDiscMesh(float radius)
    {
        Vector3[] vertices = new Vector3[Segments + 1];
        Vector3[] normals = new Vector3[Segments + 1];
        int[] triangles = new int[Segments * 3];

        vertices[0] = Vector3.zero;
        normals[0] = Vector3.up;

        for (int i = 0; i < Segments; i++)
        {
            float angle = i / (float)Segments * Mathf.PI * 2f;
            vertices[i + 1] = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            normals[i + 1] = Vector3.up;

            // Abanico de triangulos desde el centro.
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = (i + 1) % Segments + 1;
            triangles[i * 3 + 2] = i + 1;
        }

        Mesh mesh = new Mesh { name = "WeaponZoneDisc" };
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        return mesh;
    }

    private void OnDestroy()
    {
        if (discMaterial != null) Destroy(discMaterial);
        if (ringMaterial != null) Destroy(ringMaterial);
        if (discMesh != null) Destroy(discMesh);
    }
}
