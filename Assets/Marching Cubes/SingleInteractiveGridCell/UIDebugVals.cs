using TMPro;
using UnityEngine;

// Represents the debug text on the screen
// Subscribe to InteractiveVertex's OnToggleVertex event and updates the text
public class UIDebugVals : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI debugText;

    private void OnEnable()
    {
        debugText = this.gameObject.GetComponent<TextMeshProUGUI>();
        InteractiveGridCell.onGridUpdated += UpdateDebugText;
    }

    private void OnDisable()
    {
        InteractiveGridCell.onGridUpdated -= UpdateDebugText;
    }

    private void UpdateDebugText(GridCell gridCell)
    {
        string output = "GridCell Info:\n";

        for (int i = 0; i < 8; i++)
        {
            output += $"Vertex {i}: {gridCell.verticies[i].ToString()} | Noise Value: {gridCell.noiseValues[i]}\n";
        }

        debugText.text = output;
    }
}
