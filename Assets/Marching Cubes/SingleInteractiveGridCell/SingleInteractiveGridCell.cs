using UnityEngine;

public class SingleInteractiveGridCell : MarchingCubes
{
    public GridCell interactiveGridCell;
    private GameObject[] cornerMarkers = new GameObject[8]; // Array to keep track of the corner spheres

    public delegate void GridCellEvent(GridCell gridCell);  // Delegate for the event
    public static event GridCellEvent OnToggleVertex;       // The event

    protected override void Start()
    {
        interactiveGridCell = CreateGridCell(0, 0, 0);
        
        //Update each noisevalue in gridcell to 0
        for (int i = 0; i < interactiveGridCell.noiseValues.Length; i++)
        {
            interactiveGridCell.noiseValues[i] = 0f;
        }

        Debug.Log(interactiveGridCell.noiseValues);
        DrawDebugGridCell(interactiveGridCell);
        isolevel = 0.5f;
    }

    protected override void Update()
    {
    }

    void DrawDebugGridCell(GridCell gridCell)
    {
        GameObject cube = new GameObject("DebugGridCell");

        for (int i = 0; i < gridCell.verticies.Length; i++)
        {
            GameObject markerInstance = Instantiate(cornerGridVertexMarker, gridCell.verticies[i], Quaternion.identity, cube.transform);
            cornerMarkers[i] = markerInstance; // Store the sphere in the array
            InteractiveVertex interactionScript = markerInstance.AddComponent<InteractiveVertex>();
            interactionScript.gridCellScript = this;
            interactionScript.gridCell = gridCell;
            interactionScript.vertexIndex = i;
        }
    }

    public void ToggleVertexValue(int vertexIndex, bool isOn)
    {
        interactiveGridCell.noiseValues[vertexIndex] = isOn ? 100f : 0f;

        // Change the color of the corresponding sphere based on isOn
        if (cornerMarkers[vertexIndex])
        {
            cornerMarkers[vertexIndex].GetComponent<Renderer>().material.color = isOn ? Color.black : Color.white;
        }

        // Raise the event after updating the vertex value
        OnToggleVertex?.Invoke(interactiveGridCell);
    }
}

