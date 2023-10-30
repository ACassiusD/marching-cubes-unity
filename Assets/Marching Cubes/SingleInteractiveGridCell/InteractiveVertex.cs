using UnityEngine;

public class InteractiveVertex : MonoBehaviour
{
    public int vertexIndex;   // Index of this vertex in the grid cell's verticies array
    private bool isOn = false; // Keep track of the toggle state

    // Delegate and event cell grid subscribes to, raised when vertex is toggled
    public delegate void VertexToggledHandler(int vertexIndex, bool isOn);
    public event VertexToggledHandler OnToggledVertex;

    private void OnMouseOver()
    {
        if (Input.GetMouseButtonDown(0))
        {
            isOn = !isOn;
            OnToggledVertex?.Invoke(vertexIndex, isOn);
        }
    }
}
