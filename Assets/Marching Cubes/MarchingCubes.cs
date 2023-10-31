using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

public class MarchingCubes : MonoBehaviour
{    
    //Debugging sphere markers
    public GameObject gridVertexMarker;
    public GameObject cornerGridVertexMarker;
    private List<GameObject> gridVertexMarkerInstances = new List<GameObject>();// Cached list of all grid markers exclusing current grid cells special highlighted vertices
    protected GameObject previousCube = null; // Cached reference so we can destroy it later
    public float marchingSpeed = 0.5f;
    public float gridCellOpacity = 0.2f;
    public Boolean showGradient = false;
    public Boolean hideAir = false;
    public Boolean instantMarch = false;
    public Boolean disableLinearInterpolation = false;

    //Grid parameters
    public Vector3Int gridSize = new Vector3Int(10, 10, 10);

    //Perlin noise parameters
    public float isolevel = 0.41424124f; 
    public float noiseScale = 4.41337f;
    public Vector3 noiseOffset = new Vector3(0, 0, 0);
    public Boolean drawCube = false;

    protected virtual void Start()
    {
        // Initialize the markers for the first time
        Draw3DGridMarkers(gridSize);
        //StartProcessingGrid();
    }

    protected virtual void Update()
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

                    // Set the color of the marker based on the noise value and isolevel
                    Color noiseColor = Color.white;
                    if (showGradient){
                        noiseColor = Color.Lerp(Color.black, Color.white, noiseValue);
                    }else{
                        if (noiseValue < isolevel){
                            noiseColor = Color.black;
                        }
                    }

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

                    // Hide the marker if the noise value is below the isolevel and hideAir is true
                    markerInstance.SetActive(!hideAir || noiseValue <= isolevel);

                    markerIndex++;
                }
            }
        }
    }

    // Given a single gridcell, and isolevel, return a triangle mesh
    protected int PolygoniseGridCell(GridCell gridcell, float isolevel, ref List<Triangle> triangles)
    {
        int[] edgeTable = MarchingCubesLookupTables.edgeTable;
        int[,] triTable = MarchingCubesLookupTables.triTable;
        int i, ntriang;
        int cubeindex;

        //Holds the positions of the vertices where the isosurface intersects the edges of the current grid cell.
        //These intersection points are the vertices of the triangles that will represent the isosurface within the current grid cell. 
        Vector3[] vertlist = new Vector3[12];

        // Find the cube index. based on the noise values at each vertex of the grid cell and the isolevel.
        // Points below the surface level / isolevel are considered outside the shape. Empty space / air.
        // Points at or on the surface level / isolevel are considered  on the surface or inside the shape.
        cubeindex = 0;
        if (gridcell.noiseValues[0] >= isolevel) cubeindex |= 1;
        if (gridcell.noiseValues[1] >= isolevel) cubeindex |= 2;
        if (gridcell.noiseValues[2] >= isolevel) cubeindex |= 4;
        if (gridcell.noiseValues[3] >= isolevel) cubeindex |= 8;
        if (gridcell.noiseValues[4] >= isolevel) cubeindex |= 16;
        if (gridcell.noiseValues[5] >= isolevel) cubeindex |= 32;
        if (gridcell.noiseValues[6] >= isolevel) cubeindex |= 64;
        if (gridcell.noiseValues[7] >= isolevel) cubeindex |= 128;


        //Cube is entirely in/out of the surface
        if (edgeTable[cubeindex] == 0)
            return (0);

        //Checks whether the isosurface intersects for all 12 edges of the cube,
        // if it does, it calculates the intersection point using linear interpolation and stores this point in vertlist[0]
        //for later use in constructing the triangles that approximate the isosurface within the cube.


        //Checks if the first bit is set in edgeTable Value
        // If true, populates an entry in the vertlist[] array with the interpolated vertex. 
        if ( (edgeTable[cubeindex] & 1) != 0 )
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
        
        //Point between 4 and 5 is is correct for cubeIndex = 32
        if ((edgeTable[cubeindex] & 16) != 0)
            vertlist[4] =
               VertexInterp(isolevel, gridcell.verticies[4], gridcell.verticies[5], gridcell.noiseValues[4], gridcell.noiseValues[5]);
        
        //Point between 5 and 5 is incorrect, should be  correct for cubeIndex = 32
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
       
        //Point between 1 and 5 is is correct for cubeIndex = 32
        if ((edgeTable[cubeindex] & 1024) != 0)
            vertlist[10] =
               VertexInterp(isolevel, gridcell.verticies[2], gridcell.verticies[6], gridcell.noiseValues[2], gridcell.noiseValues[6]);

        if ((edgeTable[cubeindex] & 2048) != 0)
            vertlist[11] =
               VertexInterp(isolevel, gridcell.verticies[3], gridcell.verticies[7], gridcell.noiseValues[3], gridcell.noiseValues[7]);

        // Create the triangle
        ntriang = 0;
        for (i = 0; triTable[cubeindex, i] != -1; i += 3)
        {
            // Create a new Triangle instance with the vertices from vertlist
            Triangle triangle = new Triangle(
                vertlist[triTable[cubeindex, i]],
                vertlist[triTable[cubeindex, i + 1]],
                vertlist[triTable[cubeindex, i + 2]]
            );

            // Add the new Triangle instance to the triangles list
            triangles.Add(triangle);

            // Increment the triangle count to move on to the next triangle for the next iteration.
            ntriang++;
        }

        return (ntriang);
    }


    // Linearly interpolate the position where an isosurface cuts
    // an edge between two vertices, each with their own scalar value
    // Linearly interpolate the position where an isosurface cuts
    // an edge between two vertices, each with their own scalar value
    Vector3 VertexInterp(float isolevel, Vector3 p1, Vector3 p2, float valp1, float valp2)
    {
        if (disableLinearInterpolation)
        {
            // Return the average position of the two vertices without interpolation
            return (p1 + p2) / 2;
        }
        else
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
                    PolygoniseGridCell(gridCell, isolevel, ref allTriangles);
                    if (instantMarch)
                        yield return null;
                    else
                        yield return new WaitForSeconds(marchingSpeed);

                    BuildMesh(allTriangles);
                }
            }
        }

        //BuildMesh(allTriangles);
    }

    protected GameObject DrawCurrentGridCell(GridCell gridCell)
    {
        Material redMaterial = new Material(Shader.Find("Standard"));
        redMaterial.color = Color.red;

        // Destroys/clears the last drawn gridcell cube and highlighted vertices
        if (previousCube != null)
            Destroy(previousCube);

        // Calculate the average position of the vertices to get the center position of the grid cell
        Vector3 centerPosition = Vector3.zero;
        foreach (Vector3 vertex in gridCell.verticies)
        {
            centerPosition += vertex;
        }
        centerPosition /= gridCell.verticies.Length;

        // Create a new material with a transparent shader
        Material transparentMaterial = new Material(Shader.Find("Transparent/Diffuse"));
        transparentMaterial.color = new Color(1, 1, 1, gridCellOpacity);  // Set the color to white with 50% transparency

        // Create and position a cube to visualize the grid cell
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);

        cube.transform.position = centerPosition;  // Position the cube at the center of the grid cell

        // Assume the grid cell is cubic and use the distance between two adjacent vertices to determine the size of the cube
        float size = Vector3.Distance(gridCell.verticies[0], gridCell.verticies[1]);
        cube.transform.localScale = new Vector3(size, size, size);  // Set the size of the cube to match the size of the grid cell
        if (!drawCube)
        {
            cube.SetActive(false);
        }

        // Apply the transparent material to the cube
        Renderer cubeRenderer = cube.GetComponent<Renderer>();
        cubeRenderer.material = transparentMaterial;

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
    // Populates the vertices and noiseValues
    public GridCell CreateGridCell(int x, int y, int z)
    {
        // Arrays to hold the vertices and noise values for the grid cell.
        Vector3[] vertices = new Vector3[8];
        float[] noiseValues = new float[8];

        // Offsets for vertices based on the marching cubes convention.
        Vector3[] offsets = {
            new Vector3(0, 0, 1), // 0
            new Vector3(1, 0, 1), // 1
            new Vector3(1, 0, 0), // 2
            new Vector3(0, 0, 0), // 3
            new Vector3(0, 1, 1), // 4
            new Vector3(1, 1, 1), // 5
            new Vector3(1, 1, 0), // 6
            new Vector3(0, 1, 0) // 7
        };

        // Loop through each vertex of the grid cell.
        for (int i = 0; i < 8; i++)
        {
            // Calculate the world position of the vertex using the correct offset.
            Vector3 vertexPosition = new Vector3(x + offsets[i].x, y + offsets[i].y, z + offsets[i].z);

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
