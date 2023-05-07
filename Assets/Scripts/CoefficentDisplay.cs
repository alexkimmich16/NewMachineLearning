using UnityEngine;

public class CoefficentDisplay : MonoBehaviour
{
    public int resolution = 100;
    public float minX = -5f;
    public float maxX = 5f;
    public float minY = -5f;
    public float maxY = 5f;
    public Color lineColor = Color.white;

    private LineRenderer lineRenderer;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = resolution + 1;
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;
        lineRenderer.startWidth = 0.05f;
        lineRenderer.endWidth = 0.05f;

        float step = (maxX - minX) / resolution;
        float x = minX;
        for (int i = 0; i <= resolution; i++)
        {
            float y = EvaluatePolynomial(x);
            Vector3 pos = new Vector3(x, y, 0f);
            lineRenderer.SetPosition(i, pos);
            x += step;
        }
    }

    float EvaluatePolynomial(float x)
    {
        // Replace this with your own polynomial expression
        float y = x * x + 2f * x + 1f;
        return y;
    }
}
