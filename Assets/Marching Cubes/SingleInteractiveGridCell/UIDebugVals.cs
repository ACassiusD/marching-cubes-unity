using TMPro;
using UnityEngine;

public class UIDebugVals : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI debugText;

    private void OnEnable()
    {
        debugText = this.gameObject.GetComponent<TextMeshProUGUI>();
        SingleInteractiveGridCell.OnToggleVertex += UpdateDebugText;
    }

    private void OnDisable()
    {
        SingleInteractiveGridCell.OnToggleVertex -= UpdateDebugText;
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
