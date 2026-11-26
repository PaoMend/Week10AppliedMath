using UnityEngine;
using System.Collections.Generic;

public class SphereMesh : MonoBehaviour
{
    [SerializeField] private Material lineMat;
    [SerializeField] private int longitudeSegments = 12;
    [SerializeField] private int latitudeSegments = 8;
    [SerializeField] private float scale = 1;
    [SerializeField] private float focalLength = 20f;
    [SerializeField] private float xRotation = 0;
    [SerializeField] private float yRotation = 0;
    private Vector3[] verts;
    private int[][] faces;
    private Mesh mesh;
    private List<Vector3> actualPosition = new();
    private List<int> visibleEdges = new();

    void Awake()
    {
        GenerateSphere();

        mesh = new Mesh();

        gameObject.AddComponent<MeshFilter>().mesh = mesh;
        gameObject.AddComponent<MeshRenderer>().material = lineMat;
    }

    void GenerateSphere()
    {
        longitudeSegments = Mathf.Max(6, longitudeSegments);
        latitudeSegments = Mathf.Max(2, latitudeSegments);
        List<Vector3> vertices = new List<Vector3>();
        List<int[]> polygons = new List<int[]>();

        vertices.Add(new Vector3(0, 1, 0));

        for (int lat = 1; lat < latitudeSegments; lat++)
        {
            float theta = Mathf.PI * lat / latitudeSegments;

            for (int lon = 0; lon < longitudeSegments; lon++)
            {
                float phi = 2 * Mathf.PI * lon / longitudeSegments;
                float x = Mathf.Sin(theta) * Mathf.Cos(phi);
                float y = Mathf.Cos(theta);
                float z = Mathf.Sin(theta) * Mathf.Sin(phi);

                vertices.Add(new Vector3(x, y, z));
            }
        }

        int bottomIndex = vertices.Count;
        vertices.Add(new Vector3(0, -1, 0));

        for (int lon = 0; lon < longitudeSegments; lon++)
        {
            int next = (lon + 1) % longitudeSegments;

            polygons.Add(new int[]
            {
                0,
                1 + lon,
                1 + next
            });
        }

        for (int lat = 0; lat < latitudeSegments - 2; lat++)
        {
            for (int lon = 0; lon < longitudeSegments; lon++)
            {
                int next = (lon + 1) % longitudeSegments;

                int a = 1 + lat * longitudeSegments + lon;
                int b = 1 + lat * longitudeSegments + next;

                int c = b + longitudeSegments;
                int d = a + longitudeSegments;

                polygons.Add(new int[] { a, b, c, d });
            }
        }

        int lastRing = 1 + (latitudeSegments - 2) * longitudeSegments;

        for (int lon = 0; lon < longitudeSegments; lon++)
        {
            int next = (lon + 1) % longitudeSegments;

            polygons.Add(new int[]
            {
                lastRing + lon,
                bottomIndex,
                lastRing + next
            });
        }

        verts = vertices.ToArray();
        faces = polygons.ToArray();
    }

    void LateUpdate()
    {
        yRotation += 30f * Time.deltaTime;
        xRotation += 20f * Time.deltaTime;

        actualPosition.Clear();

        foreach (var point in verts)
        {
            actualPosition.Add(Project(point));
        }

        mesh.vertices = actualPosition.ToArray();

        visibleEdges.Clear();

        foreach (var f in faces)
        {
            if (!FacesCamera(
                actualPosition[f[0]],
                actualPosition[f[1]],
                actualPosition[f[2]]))
                continue;

            for (int i = 0; i < f.Length; i++)
            {
                visibleEdges.Add(f[i]);
                visibleEdges.Add(f[(i + 1) % f.Length]);
            }
        }
        mesh.SetIndices(visibleEdges, MeshTopology.Lines, 0);
        mesh.RecalculateBounds();
    }

    Vector3 Project(Vector3 point)
    {
        // Y-axis rotation
        float xRot = point.x * Mathf.Cos(yRotation * Mathf.Deg2Rad) - point.z * Mathf.Sin(yRotation * Mathf.Deg2Rad);
        float zRot = point.x * Mathf.Sin(yRotation * Mathf.Deg2Rad) + point.z * Mathf.Cos(yRotation * Mathf.Deg2Rad);
        point = new Vector3(xRot, point.y, zRot);

        // X rot
        float yRot = point.y * Mathf.Cos(xRotation * Mathf.Deg2Rad) - point.z * Mathf.Sin(xRotation * Mathf.Deg2Rad);
        zRot = point.y * Mathf.Sin(xRotation * Mathf.Deg2Rad) + point.z * Mathf.Cos(xRotation * Mathf.Deg2Rad);
        point = new Vector3(point.x, yRot, zRot);

        float p = focalLength / (focalLength + point.z);

        return new Vector3(point.x * scale * p, point.y * scale * p, 0);
    }

    bool FacesCamera(Vector3 a, Vector3 b, Vector3 c)
    {
        return (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x) > 0;
    }
}
