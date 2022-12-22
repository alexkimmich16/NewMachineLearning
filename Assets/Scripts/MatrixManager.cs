using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;
public enum MatrixDisplay
{
    MotionPlayback = 0,
    LearnManager = 1,
    ControllerTesting = 2,
}
public class MatrixManager : SerializedMonoBehaviour
{
    public static MatrixManager instance;
    private void Awake() { instance = this; }

    LearnManager LM;
    [Header("MatrixStats")]
    public MatrixDisplay currentDisplay;

    public int Width = 80;
    public int Height = 20;

    public Vector2 HeightMaxMin;
    public Vector2 MaxZDistance;
    public float STimeMultiplier;

    //[BoxGroup("ReadOnly table")]
    //[TableMatrix(IsReadOnly = true)]
    [HideInInspector] public Vector2[,] GridStats;


    [HideInInspector]public List<MatrixStat> MatrixStats;

    private Texture2D TextureMap;
    public Image Display;
    private Sprite mySprite;

    private Vector3 DefultHSV = new Vector3(0f, 0f, 0.5f);

    [PropertyRange(1, 200)]public int ResetFrames;
    public int FramesWaited;
    public List<float> GridToFloats()
    {
        List<float> AllFloats = new List<float>();
        for (int i = 0; i < Width; i++)
            for (int j = 0; j < Height; j++)
            {
                AllFloats.Add(GridStats[i, j].x);
                AllFloats.Add(GridStats[i, j].y);
            }
        return AllFloats;
    }
    public void OnStartLearn()
    {
        LearnManager.instance.LoggingAI().OnNewFrame += OnNewFrame;
    }
    void Start()
    {
        GridStats = new Vector2[Width, Height];
        TextureMap = new Texture2D(Width, Height);
        TextureMap.filterMode = FilterMode.Point;

        LM = LearnManager.instance;

        if (currentDisplay == MatrixDisplay.MotionPlayback)
        {
            MotionPlayback.OnNewFrame += OnNewFrame;
            MotionEditor.OnChangeMotion += ResetMatrix;
        }
        else if (currentDisplay == MatrixDisplay.ControllerTesting)
        {

        }
        else if (currentDisplay == MatrixDisplay.LearnManager)
        {
            LearnManager.OnAlgorithmStart += OnStartLearn;
        }

        StartCoroutine(UpdateGraphic());

        ResetMatrix();
    }
    public void ResetMatrix()
    {
        MatrixStats.Clear();
        GridStats = new Vector2[Width, Height];

        for (int i = 0; i < Width; i++)
            for (int j = 0; j < Height; j++)
            {
                TextureMap.SetPixel(i, j, Color.HSVToRGB(DefultHSV.x, DefultHSV.y, DefultHSV.z));
                TextureMap.Apply();
            }
                
        mySprite = Sprite.Create(TextureMap, new Rect(0.0f, 0.0f, TextureMap.width, TextureMap.height), new Vector2(0.5f, 0.5f), 1000.0f);
        Display.sprite = mySprite;
    }
    private void Update()
    {
        if (Input.GetKey(KeyCode.KeypadEnter))
            ResetMatrix();
    }
    IEnumerator UpdateGraphic()
    {
        while (true)
        {
            while (ResetFrames > FramesWaited)
            {
                yield return new WaitForEndOfFrame();
                FramesWaited += 1;
            }
                
            UpdateGrid();
            FramesWaited = 0;
        }
    }
    public void OnNewFrame()
    {
        if (currentDisplay == MatrixDisplay.ControllerTesting && LM.Info.MyHand(EditSide.right).TriggerPressed() == false)
            return;
        if (currentDisplay == MatrixDisplay.LearnManager && LM.CurrentSingleInfo() == null || LearnManager.instance == null)
            return;
        AddToMatrixList(GetCorrospondingMatrixStat());
    }
    public void AddToMatrixList(MatrixStat Stat)
    {
        if (Stat.X > Width || Stat.X < 0 || Stat.Y > Height || Stat.Y < 0)
            Debug.LogError("larger than array at  Motion: " + LearnManager.instance.CurrentMotion + "  Set: " + LearnManager.instance.CurrentSet);
        for (int i = 0; i < MatrixStats.Count; i++)
            if (MatrixStats[i].X == Stat.X && MatrixStats[i].Y == Stat.Y)
                MatrixStats.RemoveAt(i);

        GridStats[Stat.X, Stat.Y] = Vector2.zero;

        MatrixStats.Add(Stat);
    }
    public void UpdateMatrixTimes()
    {
        for (int i = 0; i < MatrixStats.Count; i++)
        {
            float SValueTime = (1f - (STimeMultiplier * MatrixStats[i].TimeSinceCreation()));
            MatrixStats[i].S = SValueTime;
            if (SValueTime < 0)
            {
                TextureMap.SetPixel(MatrixStats[i].X, MatrixStats[i].Y, Color.HSVToRGB(0f, 0f, DefultHSV.z));
                TextureMap.Apply();

                GridStats[MatrixStats[i].X, MatrixStats[i].Y] = Vector2.zero;
                MatrixStats.RemoveAt(i);
                
            }
            else
            {
                GridStats[MatrixStats[i].X, MatrixStats[i].Y] = new Vector2(MatrixStats[i].H, MatrixStats[i].S);
            }
        }
    }

    public void UpdateGrid()
    {
        UpdateMatrixTimes();
        for (int i = 0; i < MatrixStats.Count; i++)
        {
            int X = MatrixStats[i].X;
            int Y = MatrixStats[i].Y;
            GridStats[X, Y] = new Vector2(MatrixStats[i].H, MatrixStats[i].S);
            TextureMap.SetPixel(X, Y, Color.HSVToRGB(GridStats[X, Y].x, GridStats[X, Y].y, DefultHSV.z));
            TextureMap.Apply();
        }
            
        mySprite = Sprite.Create(TextureMap, new Rect(0.0f, 0.0f, TextureMap.width, TextureMap.height), new Vector2(0.5f, 0.5f), 1000.0f);
        Display.sprite = mySprite;
    }

    public MatrixStat GetCorrospondingMatrixStat()
    {
        SingleInfo newInfo = GetCorrospondingInfo();

        int X = (int)Mathf.Abs((GetAngle() / 360f) * Width);
        int Y = (int)Mathf.Abs(Remap(newInfo.HandPos.y, HeightMaxMin) * Height);
        float H = Remap(newInfo.HandPos.z, MaxZDistance);
        float S = 1f;
        if (Y > Height || Y < 0)
            Debug.Log("Motion: " + LM.CurrentMotion + "  Set: " + LM.CurrentSet + "  Frame: " + LM.CurrentFrame());
        return new MatrixStat(X, Y, H, S);
        //currentDisplay
        SingleInfo GetCorrospondingInfo()
        {
            if (currentDisplay == MatrixDisplay.MotionPlayback)
                return LM.MovementList[(int)MotionEditor.instance.MotionType].Motions[MotionEditor.instance.MotionNum].Infos[MotionEditor.instance.display.Frame];
            else if (currentDisplay == MatrixDisplay.ControllerTesting)
                return LM.Info.GetControllerInfo(EditSide.right);
            else if (currentDisplay == MatrixDisplay.LearnManager)
                return LM.CurrentSingleInfo();

            return null;
        }
            
        float GetAngle()
        {
            Vector3 LevelCamPos = new Vector3(newInfo.HeadPos.x, 0, newInfo.HeadPos.z);
            Vector3 LevelHandPos = new Vector3(newInfo.HandPos.x, 0, newInfo.HandPos.z);

            Quaternion rotation = Quaternion.Euler(0, newInfo.HeadRot.y, 0);
            Vector3 forwardDir = rotation * Vector3.forward;

            float Angle = 360 - (newInfo.HeadRot.y + Vector3.SignedAngle(LevelCamPos - LevelHandPos, forwardDir, Vector3.up) + 0);
            //Offset
            if (Angle > 360)
                Angle -= 360;
            else if (Angle < -360)
                Angle += 360;
            return Angle;
        }
        float Remap(float Input, Vector2 MaxMin) { return (Input - MaxMin.x) / (MaxMin.y - MaxMin.x); }
    }
}
[System.Serializable]
public class MatrixStat
{
    public int X, Y;
    public float H, S;
    public float CreationTime;
    public float TimeSinceCreation()
    {
        return Time.timeSinceLevelLoad - CreationTime;
    }
    public MatrixStat(int XStat, int YStat, float HStat, float SStat)
    {
        X = XStat;
        Y = YStat;
        H = HStat;
        S = SStat;
        CreationTime = Time.timeSinceLevelLoad;
    }
}