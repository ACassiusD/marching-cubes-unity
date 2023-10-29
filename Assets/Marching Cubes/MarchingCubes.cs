using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;


public class MarchingCubes : MonoBehaviour
{    
    //Debugging sphere markers
    public GameObject gridVertexMarker;
    public GameObject cornerGridVertexMarker;
    private List<GameObject> gridVertexMarkerInstances = new List<GameObject>();// Cached list of all grid markers exclusing current grid cells special highlighted vertices
    private GameObject previousCube = null; // Cached reference so we can destroy it later
    private List<GameObject> currentGridCellVertexMarkers = new List<GameObject>(); // Cached list of instances so we can destroy them later
    public float marchingSpeed = 0.5f;

    //Grid parameters
    public Vector3Int gridSize = new Vector3Int(10, 10, 10);

    //Perlin noise parameters
    public float isolevel = 0.5f; 
    public float noiseScale = 3.51337f;
    public Vector3 noiseOffset = new Vector3(0, 0, 0);

    void Start()
    {
        // Initialize the markers for the first time
        Draw3DGridMarkers(gridSize);
    }

    void Update()
    {
        // Update the visualization each frame
        UpdateGridVisualization(gridSize);

        if (Input.GetKeyDown(KeyCode.P)) // Checks if the "P" key is pressed
        {
            StartProcessingGrid();
        }

    }

    // Draw visual debugging sphere markers for the 3D grid
    void Draw3DGridMarkers(Vector3Int size)
    {
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                for (int z = 0; z < size.z; z++)
                {
                    Vector3 position = new Vector3(x, y, z);
                    GameObject markerInstance = Instantiate(gridVertexMarker, position, Quaternion.identity, this.transform);
                    gridVertexMarkerInstances.Add(markerInstance);
                }
            }
        }
    }

    // For a given vertex on the 3D grid,
    // Set a value based on the 3D perlin noise implementation
    float GetPerlinValueForVertex(float x, float y, float z)
    {
        float scaledX = (x + noiseOffset.x) / noiseScale;
        float scaledY = (y + noiseOffset.y) / noiseScale;
        float scaledZ = (z + noiseOffset.z) / noiseScale;

        float xy = Mathf.PerlinNoise(scaledX, scaledY);
        float xz = Mathf.PerlinNoise(scaledX, scaledZ);
        float yz = Mathf.PerlinNoise(scaledY, scaledZ);
        float yx = Mathf.PerlinNoise(scaledY, scaledX);
        float zx = Mathf.PerlinNoise(scaledZ, scaledX);
        float zy = Mathf.PerlinNoise(scaledZ, scaledY);

        // Average the noise to ensure returned number is between 0 and 1
        // If we dont do this we get values too small.
        return (xy + xz + yz + yx + zx + zy) / 6f;
    }

    // Update the Grids vertex markers to reflect the current noise values
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

                    GameObject markerInstance = gridVertexMarkerInstances[markerIndex];
                    Renderer markerRenderer = markerInstance.GetComponent<Renderer>();

                    //Update the color of the marker to reflect the noise value
                    if (markerRenderer != null)
                    {
                        markerRenderer.material.color = noiseColor;
                    }

                    // Display noise value above marker
                    TextMeshPro textMeshPro = markerInstance.GetComponentInChildren<TextMeshPro>();
                    if (textMeshPro == null)
                    {
                        GameObject textObj = new GameObject("NoiseValueText");
                        textObj.transform.SetParent(markerInstance.transform);
                        textMeshPro = textObj.AddComponent<TextMeshPro>();
                    }
                    textMeshPro.text = noiseValue.ToString("F2");  // Display 2 decimal places
                    textMeshPro.transform.localPosition = Vector3.up * 0.7f;  // Position above the sphere
                    textMeshPro.fontSize = 5;
                    textMeshPro.color = Color.black;
                    textMeshPro.alignment = TextAlignmentOptions.Center;

                    markerIndex++;
                }
            }
        }
    }

    // Given a single gridcell, and isolevel, return a triangle mesh
    int PolygoniseGridCell(GridCell gridcell, float isolevel, ref List<Triangle> triangles)
    {
        int[] edgeTable = MarchingCubesLookupTables.edgeTable;
        int[,] triTable = MarchingCubesLookupTables.triTable;

        int i, ntriang;
        int cubeindex;

        // Hold the positions where the isosurface intersects the edges of the grid cell.
        // We use these values to construct the triangles that create the polygonal mesh which is an approximation of the isosurface.
        Vector3[] vertlist = new Vector3[12];

        // Find the cube index.
        // Check the noise value at each vertex of the grid cell against the isolevel.
        // If noise value is less than the isolevel, the corresponding bit in cubeindex is set to 1.
        // This means that the vertex is inside the surface.
        cubeindex = 0;
        if (gridcell.noiseValues[0] < isolevel) cubeindex |= 1;
        if (gridcell.noiseValues[1] < isolevel) cubeindex |= 2;
        if (gridcell.noiseValues[2] < isolevel) cubeindex |= 4;
        if (gridcell.noiseValues[3] < isolevel) cubeindex |= 8;
        if (gridcell.noiseValues[4] < isolevel) cubeindex |= 16;
        if (gridcell.noiseValues[5] < isolevel) cubeindex |= 32;
        if (gridcell.noiseValues[6] < isolevel) cubeindex |= 64;
        if (gridcell.noiseValues[7] < isolevel) cubeindex |= 128;


        //Cube is entirely in/out of the surface
        if (edgeTable[cubeindex] == 0)
            return (0);

        //Find the vertices where the surface intersects the cube
        //Check if the bit is set 
        if ((edgeTable[cubeindex] & 1) != 0)
            vertlist[0] =
               VertexInterp(isolevel, gridcell.verticies[0], gridcell.verticies[1], gridcell.noiseValues[0], gridcell.noiseValues[1]);
        if ((edgeTable[cubeindex] & 2) != 0)
            vertlist[1] =
               VertexInterp(isolevel, gridcell.verticies[1], gridcell.verticies[2], gridcell.noiseValues[1], gridcell.noiseValues[2]);
        if ((edgeTable[cubeindex] & 4) != 0)
            vertlist[2] =
               VertexInterp(isolevel, gridcell.verticies[2], gridcell.verticies[3], gridcell.noiseValues[2], gridcell.noiseValues[3]);
        if ((edgeTable[cubeindex] & 8) != 0)
            vertlist[3] =
               VertexInterp(isolevel, gridcell.verticies[3], gridcell.verticies[0], gridcell.noiseValues[3], gridcell.noiseValues[0]);
        if ((edgeTable[cubeindex] & 16) != 0)
            vertlist[4] =
               VertexInterp(isolevel, gridcell.verticies[4], gridcell.verticies[5], gridcell.noiseValues[4], gridcell.noiseValues[5]);
        if ((edgeTable[cubeindex] & 32) != 0)
            vertlist[5] =
               VertexInterp(isolevel, gridcell.verticies[5], gridcell.verticies[6], gridcell.noiseValues[5], gridcell.noiseValues[6]);
        if ((edgeTable[cubeindex] & 64) != 0)
            vertlist[6] =
               VertexInterp(isolevel, gridcell.verticies[6], gridcell.verticies[7], gridcell.noiseValues[6], gridcell.noiseValues[7]);
        if ((edgeTable[cubeindex] & 128) != 0)
            vertlist[7] =
               VertexInterp(isolevel, gridcell.verticies[7], gridcell.verticies[4], gridcell.noiseValues[7], gridcell.noiseValues[4]);
        if ((edgeTable[cubeindex] & 256) != 0)
            vertlist[8] =
               VertexInterp(isolevel, gridcell.verticies[0], gridcell.verticies[4], gridcell.noiseValues[0], gridcell.noiseValues[4]);
        if ((edgeTable[cubeindex] & 512) != 0)
            vertlist[9] =
               VertexInterp(isolevel, gridcell.verticies[1], gridcell.verticies[5], gridcell.noiseValues[1], gridcell.noiseValues[5]);
        if ((edgeTable[cubeindex] & 1024) != 0)
            vertlist[10] =
               VertexInterp(isolevel, gridcell.verticies[2], gridcell.verticies[6], gridcell.noiseValues[2], gridcell.noiseValues[6]);
        if ((edgeTable[cubeindex] & 2048) != 0)
            vertlist[11] =
               VertexInterp(isolevel, gridcell.verticies[3], gridcell.verticies[7], gridcell.noiseValues[3], gridcell.noiseValues[7]);

        // Create the triangle
        ntriang = 0;

        for (i = 0; triTable[cubeindex,i] != -1; i += 3)
        {
            triangles[ntriang].p[0] = vertlist[triTable[cubeindex, i]];
            triangles[ntriang].p[1] = vertlist[triTable[cubeindex, i + 1]];
            triangles[ntriang].p[2] = vertlist[triTable[cubeindex, i + 2]];
            ntriang++;
        }

        return (ntriang);
    }


   // Linearly interpolate the position where an isosurface cuts
   // an edge between two vertices, each with their own scalar value
    Vector3 VertexInterp(float isolevel, Vector3 p1, Vector3 p2, float valp1, float valp2)
    {
        // Ensure there's a change in value between the two vertices to avoid division by zero
        if (Mathf.Abs(valp1 - valp2) > 0.00001f)
        {
            float t = (isolevel - valp1) / (valp2 - valp1);
            return Vector3.Lerp(p1, p2, t);
        }
        else
        {
            // If there's no change in value, just return the position of the first vertex
            return p1;
        }
    }

    //Starts the process of marching through the grid
    public void StartProcessingGrid()
    {
        StartCoroutine(ProcessGridCoroutine());
    }

    // Iterates or "Marches" through each GridCell in the 3D grid,
    // Creating a list of triangles we will use to build our mesh.
    public IEnumerator ProcessGridCoroutine()
    {
        List<Triangle> allTriangles = new List<Triangle>();

        for (int x = 0; x < gridSize.x - 1; x++)
        {
            for (int y = 0; y < gridSize.y - 1; y++)
            {
                for (int z = 0; z < gridSize.z - 1; z++)
                {
                    // Create a new grid cell
                    GridCell gridCell = CreateGridCell(x, y, z);
                    previousCube = DrawCurrentGridCell(gridCell);
                    int triangleCount = PolygoniseGridCell(gridCell, isolevel, ref allTriangles);
                    yield return new WaitForSeconds(marchingSpeed);
                }
            }
        }

        BuildMesh(allTriangles);
    }

    private GameObject DrawCurrentGridCell(GridCell gridCell)
    {
        Material redMaterial = new Material(Shader.Find("Standard"));
        redMaterial.color = Color.red;

        // Destroys/clears the last drawn gridcell cube and highlighted vertices
        if (previousCube != null)
            Destroy(previousCube);
        foreach (GameObject oldMarker in currentGridCellVertexMarkers)
            Destroy(oldMarker);
        currentGridCellVertexMarkers.Clear();

        // Calculate the average position of the vertices to get the center position of the grid cell
        Vector3 centerPosition = Vector3.zero;
        foreach (Vector3 vertex in gridCell.verticies)
        {
            centerPosition += vertex;
        }
        centerPosition /= gridCell.verticies.Length;

        // Create a new material with a transparent shader
        Material transparentMaterial = new Material(Shader.Find("Transparent/Diffuse"));
        transparentMaterial.color = new Color(1, 1, 1, 0.5f);  // Set the color to white with 50% transparency

        // Create and position a cube to visualize the grid cell
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.position = centerPosition;  // Position the cube at the center of the grid cell

        // Assume the grid cell is cubic and use the distance between two adjacent vertices to determine the size of the cube
        float size = Vector3.Distance(gridCell.verticies[0], gridCell.verticies[1]);
        cube.transform.localScale = new Vector3(size, size, size);  // Set the size of the cube to match the size of the grid cell

        // Apply the transparent material to the cube
        Renderer cubeRenderer = cube.GetComponent<Renderer>();
        cubeRenderer.material = transparentMaterial;

        //Draw the corners and make them red for debugging
        for (int i = 0; i < 8; i++)  // Looping through all 8 vertices of the grid cell
        {
            Vector3 position = gridCell.verticies[i];
            GameObject markerInstance = Instantiate(cornerGridVertexMarker, position, Quaternion.identity, this.transform);
            Renderer markerRenderer = markerInstance.GetComponent<Renderer>();
            if (markerRenderer != null)
            {
                markerRenderer.material = redMaterial;
            }
            currentGridCellVertexMarkers.Add(markerInstance);
        }

        return cube;  // Return the cube GameObject
    }


    public void BuildMesh(List<Triangle> triangles)
    {
        Mesh mesh = new Mesh();

        List<Vector3> vertices = new List<Vector3>();
        List<int> indices = new List<int>();

        foreach (Triangle triangle in triangles)
        {
            int currentVertexCount = vertices.Count;

            vertices.Add(triangle.p[0]);
            vertices.Add(triangle.p[1]);
            vertices.Add(triangle.p[2]);

            indices.Add(currentVertexCount + 0);
            indices.Add(currentVertexCount + 1);
            indices.Add(currentVertexCount + 2);
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = indices.ToArray();

        mesh.RecalculateNormals();

        // Assign the mesh to a MeshFilter or MeshCollider
        GetComponent<MeshFilter>().mesh = mesh;
    }

    // Create Grid Cell for a given cell coordinate.
    public GridCell CreateGridCell(int x, int y, int z)
    {
        // Arrays to hold the vertices and noise values for the grid cell.
        Vector3[] vertices = new Vector3[8];
        float[] noiseValues = new float[8];

        // Loop through each vertex of the grid cell.
        for (int i = 0; i < 8; i++)
        {
            // Determine the offset of the vertex from the cell coordinate.
            int offsetX = (i & 1) == 0 ? 0 : 1;
            int offsetY = (i & 2) == 0 ? 0 : 1;
            int offsetZ = (i & 4) == 0 ? 0 : 1;

            // Calculate the world position of the vertex.
            Vector3 vertexPosition = new Vector3(x + offsetX, y + offsetY, z + offsetZ);

            // Store the vertex position in the vertices array.
            vertices[i] = vertexPosition;

            // Get the Perlin noise value at the vertex position.
            float noiseValue = GetPerlinValueForVertex(vertexPosition.x, vertexPosition.y, vertexPosition.z);

            // Store the noise value in the noiseValues array.
            noiseValues[i] = noiseValue;
        }

        // Create a new GridCell instance using the vertices and noiseValues arrays.
        GridCell gridCell = new GridCell(vertices, noiseValues);

        return gridCell;
    }
}
