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

    [Header("MatrixStats")]
    public MatrixDisplay currentDisplay;

    public int Width = 80;
    public int Height = 20;

    public Vector2 HeightMaxMin;
    public Vector2 MaxZDistance;
    public float STimeMultiplier;
    public bool DisplayActive;
    [ShowIf("DisplayActive")]public float DisplaySizeMultiplier = 1;

    //[BoxGroup("ReadOnly table")]
    //[TableMatrix(IsReadOnly = true)]
    [HideInInspector] public Vector2[,] GridStats;


    [FoldoutGroup("GridStats")] public List<MatrixStat> MatrixStats;
    [FoldoutGroup("GridStats")] public List<MatrixStat> AILastMatrixStats;
    [FoldoutGroup("GridStats")] public List<MatrixStat> DisplayLastMatrixStats;
    
    private Texture2D TextureMap;
    public Image Display;
    private Sprite mySprite;

    

    private Vector3 DefultHSV = new Vector3(0f, 0f, 0.5f);

    [PropertyRange(1, 200)]public int ResetFrames;
    public int FramesWaited;

    private int CalledCount;
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
        TextureMap = new Texture2D(Width, Height);
        TextureMap.filterMode = FilterMode.Point;
        ResetMatrix();
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

        //ResetMatrix();

        //LastMatrixStats = 
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

    public List<MatrixStat> MatrixChanges(bool IsAgent)
    {
        List<MatrixStat> Changes = new List<MatrixStat>();
        List<MatrixStat> LastMatrixAccess = IsAgent ? AILastMatrixStats : DisplayLastMatrixStats;

        //add new
        for (int i = 0; i < MatrixStats.Count; i++)
            Changes.Add(new MatrixStat(MatrixStats[i].X, MatrixStats[i].Y, MatrixStats[i].H, MatrixStats[i].S, MatrixStats[i].RotX, MatrixStats[i].RotY, MatrixStats[i].RotZ));

        //check for deleted
        for (int i = 0; i < LastMatrixAccess.Count; i++)
            if (LastMatrixGone(i))
                Changes.Add(new MatrixStat(LastMatrixAccess[i].X, LastMatrixAccess[i].Y, 0f, 0f, 0f, 0f, 0f));

        if(IsAgent == false)
            UpdateLastMatrixStats();

        CalledCount += IsAgent ? 1 : 0;
        if (CalledCount == LearnManager.instance.AICount())
        {
            CalledCount = 0;
            UpdateLastMatrixStats();
        }



        bool LastMatrixGone(int LastMatrixIndex)
        {
            for (int j = 0; j < MatrixStats.Count; j++)
                if (LastMatrixAccess[LastMatrixIndex].X == MatrixStats[j].X && LastMatrixAccess[LastMatrixIndex].Y == MatrixStats[j].Y)
                    return false;
            return true;
        }
        void UpdateLastMatrixStats()
        {
            LastMatrixAccess.Clear();
            for (int i = 0; i < MatrixStats.Count; i++)
            {
                MatrixStat stat = new MatrixStat(MatrixStats[i].X, MatrixStats[i].Y, MatrixStats[i].H, MatrixStats[i].S, MatrixStats[i].RotX, MatrixStats[i].RotY, MatrixStats[i].RotZ);
                LastMatrixAccess.Add(stat);
            }
        }
        
        return Changes;
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

            Display.rectTransform.sizeDelta = new Vector2(Width * DisplaySizeMultiplier, Height * DisplaySizeMultiplier);
            Display.enabled = DisplayActive;
        }
    }
    public void OnNewFrame()
    {
        if (currentDisplay == MatrixDisplay.ControllerTesting && LearnManager.instance.Info.MyHand(EditSide.right).TriggerPressed() == false)
            return;
        if (currentDisplay == MatrixDisplay.LearnManager && LearnManager.instance.CurrentSingleInfo() == null)
            return;
        AddToMatrixList(GetCorrospondingMatrixStat());
    }
    public void AddToMatrixList(MatrixStat Stat)
    {
        for (int i = 0; i < MatrixStats.Count; i++)
            if (MatrixStats[i].X == Stat.X && MatrixStats[i].Y == Stat.Y)
                MatrixStats.RemoveAt(i);

        GridStats[Stat.X, Stat.Y] = Vector2.zero;

        MatrixStats.Add(Stat);
    }
    public void UpdateGrid()
    {
        for (int i = 0; i < MatrixStats.Count; i++)
        {
            float SValueTime = (1f - (STimeMultiplier * MatrixStats[i].TimeSinceCreation()));
            MatrixStats[i].S = SValueTime;
            if (SValueTime < 0)
                MatrixStats.RemoveAt(i);
        }

        List<MatrixStat> ChangeStats = MatrixChanges(false);
        for (int i = 0; i < ChangeStats.Count; i++)
        {
            int X = ChangeStats[i].X;
            int Y = ChangeStats[i].Y;
            GridStats[X, Y] = new Vector2(ChangeStats[i].H, ChangeStats[i].S);
            TextureMap.SetPixel(X, Y, Color.HSVToRGB(GridStats[X, Y].x, GridStats[X, Y].y, DefultHSV.z));
            TextureMap.Apply();
        }
        mySprite = Sprite.Create(TextureMap, new Rect(0.0f, 0.0f, TextureMap.width, TextureMap.height), new Vector2(0.5f, 0.5f), 1000.0f);
        Display.sprite = mySprite;
    }

    public MatrixStat GetCorrospondingMatrixStat()
    {
        SingleInfo newInfo = GetCorrospondingInfo();
        float Angle = GetAngle() < 0f ? GetAngle() + 360f: GetAngle();
        int X = (int)(Angle / 360f * Width);
        if (X >= Width || X < 0)
            Debug.LogError("X Not In Range: " + X + "  Motion: " + LearnManager.instance.CurrentMotion + "  Set: " + LearnManager.instance.CurrentSet + "  Frame: " + LearnManager.instance.CurrentFrame());

        int Y = (int)Mathf.Abs(Remap(newInfo.HandPos.y, HeightMaxMin) * Height);
        if (Y > Height || Y < 0f)
            Debug.Log("X Not In Range: " + Y + "  Motion: " + LearnManager.instance.CurrentMotion + "  Set: " + LearnManager.instance.CurrentSet + "  Frame: " + LearnManager.instance.CurrentFrame());
        float H = Remap(newInfo.HandPos.z, MaxZDistance);
        float S = 1f;

        float XRot = newInfo.HandRot.x / 360f;
        float YRot = newInfo.HandRot.y / 360f;
        float ZRot = newInfo.HandRot.z / 360f;


        return new MatrixStat(X, Y, H, S, XRot, YRot, ZRot);
        //currentDisplay
        SingleInfo GetCorrospondingInfo()
        {
            LearnManager LM = LearnManager.instance;
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
    public float RotX, RotY, RotZ;
    public float CreationTime;
    public float TimeSinceCreation()
    {
        return Time.timeSinceLevelLoad - CreationTime;
    }
    public MatrixStat(int XStat, int YStat, float HStat, float SStat, float RotXStat, float RotYStat, float RotZStat)
    {
        X = XStat;
        Y = YStat;
        H = HStat;
        S = SStat;

        RotX = RotXStat;
        RotY = RotYStat;
        RotZ = RotZStat;
        CreationTime = Time.timeSinceLevelLoad;
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