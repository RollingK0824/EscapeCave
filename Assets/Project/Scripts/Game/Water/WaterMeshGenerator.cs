using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class WaterMeshGenerator : MonoBehaviour
{
    [SerializeField, Min(2)] private int _vertexColumns = 20;
    [SerializeField] private float _meshWidth = 14.6f;
    [SerializeField] private float _meshHeight = 5.6f;

    private void Awake()
    {
        GenerateMesh();
    }

    private void GenerateMesh()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        Mesh mesh = new Mesh { name = "WaterMesh" };

        Vector3[] vertices = new Vector3[_vertexColumns * 2];
        Vector2[] uvs = new Vector2[_vertexColumns * 2];
        int[] triangles = new int[(_vertexColumns - 1) * 6];

        float startX = -_meshWidth / 2f;
        float stepX = _meshWidth / (_vertexColumns - 1);

        for (int i = 0; i < _vertexColumns; i++)
        {
            float x = startX + stepX * i;
            vertices[i] = new Vector3(x, 0f, 0f);
            vertices[i + _vertexColumns] = new Vector3(x, -_meshHeight, 0f);

            float u = (float)i / (_vertexColumns - 1);
            uvs[i] = new Vector2(u, 1f);
            uvs[i + _vertexColumns] = new Vector2(u, 0f);

            if (i < _vertexColumns - 1)
            {
                int t = i * 6;
                triangles[t] = i;
                triangles[t + 1] = i + 1;
                triangles[t + 2] = i + _vertexColumns;

                triangles[t + 3] = i + _vertexColumns;
                triangles[t + 4] = i + 1;
                triangles[t + 5] = i + _vertexColumns + 1;
            }
        }

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();

        meshFilter.mesh = mesh;
    }
}