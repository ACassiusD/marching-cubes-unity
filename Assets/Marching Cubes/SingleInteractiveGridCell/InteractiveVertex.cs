using UnityEngine;

public class InteractiveVertex : MonoBehaviour
{
    public SingleInteractiveGridCell gridCellScript; // Reference to the SingleInteractiveGridCell script
    public GridCell gridCell; // Reference to the parent grid cell
    public int vertexIndex;   // Index of this vertex in the grid cell's verticies array

    private bool isOn = false; // Keep track of the toggle state

    private void OnMouseOver()
    {
        if (Input.GetMouseButtonDown(0)) // 0 represents the left mouse button
        {
            isOn = !isOn; // Toggle the state
            gridCellScript.ToggleVertexValue(vertexIndex, isOn);
        }
    }
}
