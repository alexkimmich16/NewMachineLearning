using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;
public class MatrixManager : MonoBehaviour
{
    public static MatrixManager instance;
    private void Awake() { instance = this; }

    LearnManager LM;
    [Header("MatrixStats")]

    public int Width = 80;
    public int Height = 20;

    public Vector2 HeightMaxMin;
    public Vector2 MaxZDistance;
    public float STimeMultiplier;

    //[BoxGroup("ReadOnly table")]
    //[TableMatrix(IsReadOnly = true)]
    public Vector2[,] GridStats;

    public List<MatrixStat> MatrixStats;

    private Texture2D TextureMap;
    public Image Display;
    private Sprite mySprite;
    public Color DisplayColor;

    public float UpdateInterval;

    public bool TestingWithControllers;

    private Vector3 DefultHSV = new Vector3(0f, 0f, 0.5f);

    [Header("Interpolation")]
    public int InterprolateFrames;

    public List<float> GridToFloats()
    {
        List<float> AllFloats = new List<float>;
        for (int i = 0; i < Width; i++)
            for (int j = 0; j < Height; j++)
            {
                AllFloats.Add(GridStats[i, j].x);
                AllFloats.Add(GridStats[i, j].y);
            }
        return AllFloats;
    }

    void Start()
    {
        GridStats = new Vector2[Width, Height];
        TextureMap = new Texture2D(Width, Height);
        TextureMap.filterMode = FilterMode.Point;

        LM = LearnManager.instance;
        MotionPlayback.OnNewFrame += OnNewFrame;
        //LM.Info.MyHand(EditSide.right).OnTriggerRelease += ResetMatrix;
        MotionEditor.OnChangeMotion += ResetMatrix;

        StartCoroutine(UpdateGraphic());

        ResetMatrix();
    }

    public List<SingleInfo> InterpolatePositions(Vector3 from, Vector3 to)
    {
        List<SingleInfo> LerpList = new List<SingleInfo>();
        float EachChange = 1f / (InterprolateFrames + 1f);
        float Current = EachChange;
        for (int i = 0; i < InterprolateFrames; i++)
        {
            LerpList.Add(new SingleInfo(Vector3.Lerp(from, to, Current), Vector3.zero, Vector3.zero, Vector3.zero));
            Current += EachChange;
        }
        return LerpList;
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
        //DataTracker.instance.LogGuess(CurrentMotion, CurrentSet);
        while (true)
        {
            yield return new WaitForSeconds(UpdateInterval);
            UpdateGrid();
        }
    }
    public void OnNewFrame()
    {
        if(TestingWithControllers == false || LM.Info.MyHand(EditSide.right).TriggerPressed() == true)
        {
            AddToMatrixList(CorrospondingMatrixStat());
        }
    }
    public void AddToMatrixList(MatrixStat Stat)
    {
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

    public MatrixStat CorrospondingMatrixStat()
    {
        SingleInfo newInfo = GetCorrospondingInfo();

        int X = (int)Mathf.Abs((GetAngle() / 360f) * Width);
        int Y = (int)Mathf.Abs(Remap(newInfo.HandPos.y, HeightMaxMin) * Height);
        float H = Remap(newInfo.HandPos.z, MaxZDistance);
        float S = 1f;

        return new MatrixStat(X, Y, H, S);

        SingleInfo GetCorrospondingInfo() { return TestingWithControllers ?
            LM.Info.GetControllerInfo(EditSide.right) :
            LM.MovementList[(int)MotionEditor.instance.MotionType].Motions[MotionEditor.instance.MotionNum].Infos[MotionEditor.instance.display.Frame];}
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