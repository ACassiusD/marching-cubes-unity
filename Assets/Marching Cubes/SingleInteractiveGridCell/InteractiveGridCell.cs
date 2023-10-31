using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class InteractiveGridCell : MarchingCubes
{
    public GridCell interactiveGridCell;
    private GameObject[] cornerMarkers = new GameObject[8]; // Array to keep track of the corner spheres

    //Event to update the UI with latest gridcell information.
    public delegate void GridCellEvent(GridCell gridCell); 
    public static event GridCellEvent onGridUpdated;  

    protected override void Start()
    {
        CreateInteractiveGridCell(CreateGridCell(0, 0, 0));
    }

    // Draws a sphere at each vertex on the grid cell, subscribing to its OnToggledVertex event
    void CreateInteractiveGridCell(GridCell gridCell)
    {
        GameObject cube = new GameObject("DebugGridCell");

        //Update the noisevalue for each vertex in gridcell to 0
        for (int i = 0; i < gridCell.noiseValues.Length; i++)
        {
            gridCell.noiseValues[i] = 0f;
        }

        //Create the visual representation of the gridcell
        for (int i = 0; i < gridCell.verticies.Length; i++)
        {
            GameObject markerInstance = Instantiate(cornerGridVertexMarker, gridCell.verticies[i], Quaternion.identity, cube.transform);
            cornerMarkers[i] = markerInstance; // Store the sphere in the array
            InteractiveVertex interactionScript = markerInstance.AddComponent<InteractiveVertex>();
            interactionScript.vertexIndex = i;

            // Display vertext # above the sphere
            TextMeshPro textMeshPro = markerInstance.GetComponentInChildren<TextMeshPro>();
            if (textMeshPro == null){
                GameObject textObj = new GameObject("NoiseValueText");
                textObj.transform.SetParent(markerInstance.transform);
                textMeshPro = textObj.AddComponent<TextMeshPro>();
            }
            textMeshPro.text = i.ToString(); 
            textMeshPro.transform.localPosition = Vector3.up * 0.7f;
            textMeshPro.fontSize = 5;
            textMeshPro.color = Color.black;
            textMeshPro.alignment = TextAlignmentOptions.Center;

            // Subscribe to the OnToggledVertex event 
            interactionScript.OnToggledVertex += ToggleVertexValue;
        }
        interactiveGridCell = gridCell;
    }

    // Called when one of the individual vertices is toggled, updates the grid cell and raises the event for the ui to use.
    public void ToggleVertexValue(int vertexIndex, bool isOn)
    {
        interactiveGridCell.noiseValues[vertexIndex] = isOn ? 100f : 0f;

        // Change the color of the corresponding sphere based on isOn
        if (cornerMarkers[vertexIndex])
        {
            cornerMarkers[vertexIndex].GetComponent<Renderer>().material.color = isOn ? Color.black : Color.white;
        }

        // Raise the event after updating the vertex value
        onGridUpdated?.Invoke(interactiveGridCell);

        DrawMesh();
    }

    protected override void Update()
    {
    }


    public void DrawMesh()
    {
        List<Triangle> allTriangles = new List<Triangle>();
        PolygoniseGridCell(interactiveGridCell, isolevel, ref allTriangles);
        BuildMesh(allTriangles);
    }
}

