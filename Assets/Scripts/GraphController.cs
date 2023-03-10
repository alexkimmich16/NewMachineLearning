using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
public struct GraphElement
{
    public float X1 { get; set; }
    public float X2 { get; set; }
    public float X3 { get; set; }
    public bool Flag { get; set; }
}

public class GraphController : SerializedMonoBehaviour
{
    float[] xValues = new float[] { 0.0f, 0.1f, 0.2f, 0.3f, 0.4f, 0.5f, 0.6f, 0.7f, 0.8f, 0.9f, 1.0f };
    float[][] yValues = new float[11][];
    private void Start()
    {
        for (int i = 0; i <= 10; i++)
        {
            yValues[i] = new float[3];

            float x = (float)i / 10.0f;
            yValues[i][0] = 1.0f - x;
            yValues[i][1] = x;
            yValues[i][2] = x;

            // Normalize the y-values to ensure they add up to 1
            float sum = yValues[i][0] + yValues[i][1] + yValues[i][2];
            yValues[i][0] /= sum;
            yValues[i][1] /= sum;
            yValues[i][2] /= sum;
        }
    }

}