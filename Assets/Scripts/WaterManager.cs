using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class WaterManager : MonoBehaviour
{
    private MeshFilter meshFilter;

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
    }

    private void Update()
    {
        Mesh mesh = meshFilter.mesh;
        Vector3[] vertices = mesh.vertices;
        for (int i = 0; i < vertices.Length; i++)
        {
            // Berechne die globalen X- und Z-Koordinaten für jeden einzelnen Punkt (Vertex) des Wassers
            float worldX = transform.position.x + vertices[i].x;
            float worldZ = transform.position.z + vertices[i].z; // NEU: Z-Achse hinzugefügt

            // Übergebe X und Z an den WaveManager, damit das visuelle Wasser exakt mit der Physik übereinstimmt
            vertices[i].y = WaveManager.instance.GetWaveHeight(worldX, worldZ);
        }
        mesh.vertices = vertices;
        mesh.RecalculateNormals();
    }
}