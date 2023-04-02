using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Mathematics;
using System.Linq;
using RestrictionSystem;
using TMPro;
public class Graph : MonoBehaviour
{
    public static Graph instance;
    private void Awake() { instance = this; }

    public int width = 256;
    public int height = 256;

    public float2 GraphMinMax;

    public static Image Display() { return instance.transform.GetChild(1).GetComponent<Image>(); }
    private Sprite mySprite;

    private Texture2D TextureMap;

    public Color[] Colors;
    public TextMeshProUGUI[] TextDisplay;
    public Image[] ColorDisplay;

    void Start()
    {
        TextureMap = new Texture2D(width, height);
        TextureMap.filterMode = FilterMode.Point;

        for (int i = 0; i < TextDisplay.Length; i++)
        {
            TextDisplay[i].text = RegressionSystem.instance.UploadRestrictions.Restrictions[i].Label;
            ColorDisplay[i].color = Colors[i];
        }

        UpdateGraph();

        MotionEditor.OnChangeMotion += UpdateGraph;
        RegressionSystem.OnPreformRegression += UpdateGraph;
    }

    public void UpdateGraph()
    {
        TextureMap = new Texture2D(width, height);
        TextureMap.filterMode = FilterMode.Point;

        List<SingleFrameRestrictionValues> FrameInfo = BruteForce.instance.GetRestrictionsForMotions((RestrictionSystem.CurrentLearn)((int)MotionEditor.instance.MotionType), RegressionSystem.instance.UploadRestrictions);
        List<List<int>> Overrides = new List<List<int>>();

        if((int)MotionEditor.instance.MotionType == 0)
        {
            TextureMap.Apply();
            mySprite = Sprite.Create(TextureMap, new Rect(0.0f, 0.0f, TextureMap.width, TextureMap.height), new Vector2(0.5f, 0.5f), 1000.0f);
            Display().sprite = mySprite;
            return;
        }

        for (int i = 0; i < width; i++)
        {
            Overrides.Add(new List<int>());
        }
        
        for (int m = 0; m < Colors.Length; m++)
        {
            //plotted on Y axis
            float[] AssortedList = FrameInfo.Select(x => x.OutputRestrictions[m]).ToArray();
            float2 MinMax = new float2(AssortedList.Min(), AssortedList.Max());
            RegressionInfo.DegreeList Degrees = RestrictionManager.instance.coefficents.RegressionStats[(int)MotionEditor.instance.MotionType - 1].Coefficents[m];
            //Debug.Log("Min: " + MinMax + "  MAx: " + MinMax.y);
            //Maximum output plotting on X axis

            ///buttom is always 0
            float[] XOfDir = XOfDerivitive(Degrees.Degrees.ToArray());
            float[] YOfDir = new float[] { Solve(XOfDir[0], Degrees.Degrees.ToArray()), Solve(XOfDir[1], Degrees.Degrees.ToArray()) };

            float AbsoluteMax = YOfDir.Max();

            //Debug.Log("MaxX1: " + XOfDir.Max() + "  MaxY2: " + AbsoluteMax);
            //Debug.Log("MaxX1: " + YOfDir.Max() + "  MaxX2: " + YOfDir.Min());
            //Debug.Log("Solve: " + Solve(6.585f, Degrees.Degrees.ToArray()));
            //Debug.Log("Degrees.Degrees: " + Degrees.Degrees[0]);
            //CAPS
            //left/right = 0/MinMax.y
            //up/down = AbsoluteMax/0
            for (int x = 0; x < width; x++)
            {
                float XInput = Mathf.Lerp(MinMax.x, MinMax.y, (float)x / width);
                float Value = Solve(XInput, Degrees.Degrees.ToArray());


                float Inverse = Mathf.InverseLerp(GraphMinMax.x, GraphMinMax.y, Value);
                int GridY = Mathf.RoundToInt(Inverse * height);

                if (!Overrides[x].Contains(GridY))
                {
                    TextureMap.SetPixel(x, GridY, Colors[m]);
                    Overrides[x].Add(GridY);
                }

            }
        }
            

        TextureMap.Apply();

        mySprite = Sprite.Create(TextureMap, new Rect(0.0f, 0.0f, TextureMap.width, TextureMap.height), new Vector2(0.5f, 0.5f), 1000.0f);
        Display().sprite = mySprite;

        

        float Solve(float Input, float[] Degrees)
        {
            float Value = 0f;
            for (int i = 0; i < Degrees.Length; i++)
                Value += Mathf.Pow(Input, i + 1) * Degrees[i];
            return Value;
        }
        float[] XOfDerivitive(float[] Degrees)
        {
            float[] Der = new float[] { 1f * Degrees[0], 2f * Degrees[1], 3f * Degrees[2] };///perhaps switch order
            return new float[] { 
                (-Der[1] - Mathf.Sqrt(Mathf.Pow(Der[1], 2) - 4f * Der[0] * Der[2])) / (2f * Der[2]), 
                (-Der[1] + Mathf.Sqrt(Mathf.Pow(Der[1], 2) - 4f * Der[0] * Der[2])) / (2f * Der[2]) };
        }
    }
}
