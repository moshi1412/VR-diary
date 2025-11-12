using UnityEngine;

[ExecuteAlways] // 
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ProceduralTube : MonoBehaviour
{
    [Header("Tube Parameters")]
    public float innerRadius = 0.4f;
    public float outerRadius = 0.5f;
    public float height = 2f;
    [Range(3, 256)] public int segments = 64;

    [Header("Options")]
    public bool addBottomCap = true;
    public bool addTopCap = false; 
    void OnValidate() 
    {
        GetComponent<MeshFilter>().mesh = Generate();
    }

    Mesh Generate()
    {
        Mesh mesh = new Mesh();
        mesh.name = "Procedural Tube";

        int ringVertexCount = segments * 4;
        int capVertexCount = (addBottomCap ? segments * 2 : 0) + (addTopCap ? segments * 2 : 0);
        Vector3[] vertices = new Vector3[ringVertexCount + capVertexCount];
        int triCount = segments * 12;
        if (addBottomCap) triCount += segments * 6;
        if (addTopCap) triCount += segments * 6;
        int[] triangles = new int[triCount];

        // --- Generate side vertices ---
        for (int i = 0; i < segments; i++)
        {
            float angle0 = i * Mathf.PI * 2f / segments;
            float cos = Mathf.Cos(angle0);
            float sin = Mathf.Sin(angle0);
            int vi = i * 4;
            vertices[vi + 0] = new Vector3(cos * innerRadius, -height / 2f, sin * innerRadius);
            vertices[vi + 1] = new Vector3(cos * outerRadius, -height / 2f, sin * outerRadius);
            vertices[vi + 2] = new Vector3(cos * innerRadius, height / 2f, sin * innerRadius);
            vertices[vi + 3] = new Vector3(cos * outerRadius, height / 2f, sin * outerRadius);
        }

        // --- Generate side triangles (outer + inner surfaces) ---
        for (int i = 0; i < segments; i++)
        {
            int ni = (i + 1) % segments;
            int vi = i * 4;
            int ni4 = ni * 4;
            int ti = i * 12;

            // Outer surface (make sure winding faces OUTSIDE)
            triangles[ti + 0] = vi + 1;   // outer bottom (i)
            triangles[ti + 1] = vi + 3;   // outer top    (i)
            triangles[ti + 2] = ni4 + 1;  // outer bottom (i+1)

            triangles[ti + 3] = vi + 3;   // outer top    (i)
            triangles[ti + 4] = ni4 + 3;  // outer top    (i+1)
            triangles[ti + 5] = ni4 + 1;  // outer bottom (i+1)

            // Inner surface (faces INSIDE, so use opposite winding)
            triangles[ti + 6] = vi + 2;   // inner top    (i)
            triangles[ti + 7] = ni4 + 0;  // inner bottom (i+1)
            triangles[ti + 8] = vi + 0;   // inner bottom (i)

            triangles[ti + 9] = vi + 2;   // inner top    (i)
            triangles[ti + 10] = ni4 + 2;  // inner top    (i+1)
            triangles[ti + 11] = ni4 + 0;  // inner bottom (i+1)
        }

        int vOffset = ringVertexCount;
        int tOffset = segments * 12;

        GetComponent<MeshCollider>().sharedMesh = mesh;



        // --- Bottom Cap ---
        if (addBottomCap)
        {
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);
                vertices[vOffset + i * 2 + 0] = new Vector3(cos * innerRadius, -height / 2f, sin * innerRadius);
                vertices[vOffset + i * 2 + 1] = new Vector3(cos * outerRadius, -height / 2f, sin * outerRadius);
            }
            for (int i = 0; i < segments; i++)
            {
                int ni = (i + 1) % segments;
                int bi = vOffset + i * 2;
                int ni2 = vOffset + ni * 2;
                int ti = tOffset + i * 6;
                triangles[ti + 0] = bi + 0;
                triangles[ti + 1] = bi + 1;
                triangles[ti + 2] = ni2 + 1;
                triangles[ti + 3] = bi + 0;
                triangles[ti + 4] = ni2 + 1;
                triangles[ti + 5] = ni2 + 0;
            }
            vOffset += segments * 2;
            tOffset += segments * 6;
        }

        // --- Top Cap (optional) ---
        if (addTopCap)
        {
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);
                vertices[vOffset + i * 2 + 0] = new Vector3(cos * innerRadius, height / 2f, sin * innerRadius);
                vertices[vOffset + i * 2 + 1] = new Vector3(cos * outerRadius, height / 2f, sin * outerRadius);
            }
            for (int i = 0; i < segments; i++)
            {
                int ni = (i + 1) % segments;
                int bi = vOffset + i * 2;
                int ni2 = vOffset + ni * 2;
                int ti = tOffset + i * 6;
                triangles[ti + 0] = bi + 1;
                triangles[ti + 1] = bi + 0;
                triangles[ti + 2] = ni2 + 0;
                triangles[ti + 3] = bi + 1;
                triangles[ti + 4] = ni2 + 0;
                triangles[ti + 5] = ni2 + 1;
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        // --- Apply mesh to collider if present ---
        var mc = GetComponent<MeshCollider>();
        if (mc != null)
        {
            mc.sharedMesh = null;      
            mc.sharedMesh = mesh;      
        }

        return mesh;
    }
}
