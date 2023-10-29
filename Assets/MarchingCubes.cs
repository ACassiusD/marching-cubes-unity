using System.Collections.Generic;
using UnityEngine;

public class MarchingCubes : MonoBehaviour
{
    public GameObject vertexMarker;
    public Vector3Int gridSize = new Vector3Int(10, 10, 10);
    private MarchingCubesTableData marchingCubesTableData;

    // Noise parameters
    public float scale = 3.51337f;
    public Vector3 offset = new Vector3(0, 0, 0);

    // List of all the markers
    private List<GameObject> markers = new List<GameObject>();

    void Start()
    {
        // Initialize the markers for the first time
        InitializeGridMarkers(gridSize);
    }

    void Update()
    {
        // Update the visualization each frame
        UpdateGridVisualization(gridSize);
    }



    float GetPerlinValueForVertex(float x, float y, float z)
    {
        float scaledX = (x + offset.x) / scale;
        float scaledY = (y + offset.y) / scale;
        float scaledZ = (z + offset.z) / scale;

        float xy = Mathf.PerlinNoise(scaledX, scaledY);
        float xz = Mathf.PerlinNoise(scaledX, scaledZ);
        float yz = Mathf.PerlinNoise(scaledY, scaledZ);
        float yx = Mathf.PerlinNoise(scaledY, scaledX);
        float zx = Mathf.PerlinNoise(scaledZ, scaledX);
        float zy = Mathf.PerlinNoise(scaledZ, scaledY);

        // Average the noise values to get a value between 0 and 1
        return (xy + xz + yz + yx + zx + zy) / 6f;
    }

    void InitializeGridMarkers(Vector3Int size)
    {
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                for (int z = 0; z < size.z; z++)
                {
                    Vector3 position = new Vector3(x, y, z);
                    GameObject markerInstance = Instantiate(vertexMarker, position, Quaternion.identity, this.transform);
                    markers.Add(markerInstance);
                }
            }
        }
    }

    void UpdateGridVisualization(Vector3Int size)
    {
        int markerIndex = 0;
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                for (int z = 0; z < size.z; z++)
                {
                    float noiseValue = GetPerlinValueForVertex(x, y, z);
                    Color noiseColor = Color.Lerp(Color.black, Color.white, noiseValue);

                    Renderer markerRenderer = markers[markerIndex].GetComponent<Renderer>();
                    if (markerRenderer != null)
                    {
                        markerRenderer.material.color = noiseColor;
                    }

                    markerIndex++;
                }
            }
        }
    }
}
