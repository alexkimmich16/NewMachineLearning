using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Mathematics;
using System.Linq;
using RestrictionSystem;
public class Graph : MonoBehaviour
{
    /*
    public static Graph instance;
    private void Awake() { instance = this; }

    public int width = 256;
    public int height = 256;

    public float2 GraphMinMax;

    public static Image Display() { return instance.transform.GetChild(1).GetComponent<Image>(); }
    private Sprite mySprite;

    private Texture2D TextureMap;

    public Color[] Colors;
    //public TextMeshProUGUI[] TextDisplay;
    //public Image[] ColorDisplay;
    public List<float2> MaxMins;
    public List<List<int>> GridHeights;
    [System.Serializable]
    public class ColorChange
    {
        public Color ActiveColor;

        public Side side;
        
        public List<int> PreviousGridColumns;
        
        public List<float> CurrentValues;
        public List<float> lerp;
    }

    public ColorChange[] ColorChanges;

    public Toggle ShowHandPoints;

    public bool[] ActiveGraphHands;

    private void Update()
    {
        if (ShowHandPoints.isOn && PastFrameRecorder.IsReady())
            for (int i = 0; i < ActiveGraphHands.Length; i++)
                if(ActiveGraphHands[i] == true)
                    UpdateCurrentDisplay(ColorChanges[i]);
    }
    void Start()
    {
        TextureMap = new Texture2D(width, height);
        TextureMap.filterMode = FilterMode.Point;
        
        for (int i = 0; i < RestrictionManager.instance.RestrictionSettings.MotionRestrictions[(int)MotionEditor.instance.MotionType - 1].Restrictions.Count; i++)
        {
            TextDisplay[i].text = RestrictionManager.instance.RestrictionSettings.MotionRestrictions[(int)MotionEditor.instance.MotionType - 1].Restrictions[i].Label;
            ColorDisplay[i].color = Colors[i];
        }
        
        UpdateGraph();
        MotionEditor.OnChangeMotion += UpdateGraph;
        RegressionSystem.OnPreformRegression += UpdateGraph;
    }
    public void UpdateGraph()
    {
        for (int i = 0; i < ColorChanges.Length; i++)
        {
            ColorChanges[i].PreviousGridColumns.Clear();
            ColorChanges[i].CurrentValues.Clear();
            ColorChanges[i].lerp.Clear();
        }



        TextureMap = new Texture2D(width, height);
        TextureMap.filterMode = FilterMode.Point;

        Spell motion = MotionEditor.instance.MotionType;

        if (motion == Spell.Nothing)
        {
            TextureMap.Apply();
            mySprite = Sprite.Create(TextureMap, new Rect(0.0f, 0.0f, TextureMap.width, TextureMap.height), new Vector2(0.5f, 0.5f), 1000.0f);
            Display().sprite = mySprite;
            return;
        }

        List<SingleFrameRestrictionValues> FrameInfo = RestrictionStatManager.instance.GetRestrictionsForMotions((Spell)((int)motion), RestrictionManager.instance.RestrictionSettings.MotionRestrictions[(int)motion - 1]);
        List<List<int>> Overrides = new List<List<int>>();

        for (int i = 0; i < width; i++)
        {
            Overrides.Add(new List<int>());
        }
        GridHeights = new List<List<int>>();
        MaxMins = new List<float2>();

        //set motion stat
        
        for (int m = 0; m < FrameInfo[0].OutputRestrictions.Count; m++)
        {
            GridHeights.Add(new List<int>());

            //plotted on Y axis
            float[] AssortedList = FrameInfo.Select(x => x.OutputRestrictions[m]).ToArray();
            float2 MinMax = new float2(AssortedList.Min(), AssortedList.Max());
            
            MaxMins.Add(MinMax);
            RegressionInfo.DegreeList Degrees = RestrictionManager.instance.RestrictionSettings.Coefficents[(int)motion - 1].Coefficents[m];
            //Maximum output plotting on X axis

            ///buttom is always 0
            float[] XOfDir = XOfDerivitive(Degrees.Degrees.ToArray());
            float[] YOfDir = new float[] { Solve(XOfDir[0], Degrees.Degrees.ToArray()), Solve(XOfDir[1], Degrees.Degrees.ToArray()) };

            float AbsoluteMax = YOfDir.Max();

            for (int x = 0; x < width; x++)
            {
                float XInput = Mathf.Lerp(MinMax.x, MinMax.y, (float)x / width);
                float Value = Solve(XInput, Degrees.Degrees.ToArray());

                float Inverse = Mathf.InverseLerp(GraphMinMax.x, GraphMinMax.y, Value);
                int GridY = Mathf.RoundToInt(Inverse * height);
                GridY = Mathf.Clamp(GridY, 0, height);

                GridHeights[m].Add(GridY);
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
    public void UpdateCurrentDisplay(ColorChange ColorSide)
    {
        Side side = ColorSide.side;
        AthenaFrame Frame1 = PastFrameRecorder.instance.PastFrame(side);
        AthenaFrame Frame2 = PastFrameRecorder.instance.GetControllerInfo(side);

        if (ColorSide.PreviousGridColumns.Count != 0)
            for (int i = 0; i < ColorSide.PreviousGridColumns.Count; i++)
                TextureMap.SetPixel(ColorSide.PreviousGridColumns[i], GridHeights[i][ColorSide.PreviousGridColumns[i]], Colors[i]);

        ColorSide.PreviousGridColumns = new List<int>();
        ColorSide.CurrentValues = new List<float>();
        ColorSide.lerp = new List<float>();

        if (MotionEditor.instance.MotionType == Spell.Nothing)
            return;

        MotionRestriction restriction = RestrictionManager.instance.RestrictionSettings.MotionRestrictions[(int)MotionEditor.instance.MotionType - 1];
        for (int i = 0; i < restriction.Restrictions.Count; i++)
        {
            float Value = RestrictionManager.RestrictionDictionary[restriction.Restrictions[i].restriction].Invoke(restriction.Restrictions[i], Frame1, Frame2);
            ColorSide.CurrentValues.Add(Value);
            float LerpValue = (Value - MaxMins[i].x) / (MaxMins[i].y - MaxMins[i].x);
            //Mathf.Lerp(0f, 1f, Mathf.InverseLerp(MaxMins[i].x, MaxMins[i].y, Value));
            ColorSide.lerp.Add(LerpValue);
            int CurrentColumn = Mathf.RoundToInt(LerpValue * width);

            CurrentColumn = Mathf.Clamp(CurrentColumn, 0, width - 1);
            TextureMap.SetPixel(CurrentColumn, GridHeights[i][CurrentColumn], ColorSide.ActiveColor);
            ColorSide.PreviousGridColumns.Add(CurrentColumn);
        }
        TextureMap.Apply();
        mySprite = Sprite.Create(TextureMap, new Rect(0.0f, 0.0f, TextureMap.width, TextureMap.height), new Vector2(0.5f, 0.5f), 1000.0f);
        Display().sprite = mySprite;
    }
    */
    /*
    private void OldUpdateGraph()
    {
        TextureMap = new Texture2D(width, height);
        TextureMap.filterMode = FilterMode.Point;

        List<SingleFrameRestrictionValues> FrameInfo = BruteForce.instance.GetRestrictionsForMotions((RestrictionSystem.CurrentLearn)((int)MotionEditor.instance.MotionType), RegressionSystem.instance.UploadRestrictions);
        List<List<int>> Overrides = new List<List<int>>();

        int Max = RestrictionManager.instance.RestrictionSettings.Coefficents[(int)MotionEditor.instance.MotionType - 1].Coefficents.Select(x => x.Degrees.Count).Max();
        if (Max)
            if ((int)MotionEditor.instance.MotionType == 0)
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
            RegressionInfo.DegreeList Degrees = RestrictionManager.instance.RestrictionSettings.Coefficents[(int)MotionEditor.instance.MotionType - 1].Coefficents[m];
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
    */
}
