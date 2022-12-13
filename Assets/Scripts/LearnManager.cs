using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public enum CurrentLearn
{
    Nothing = 0,
    Fireball = 1,
    Flames = 2,
    FlameBlock = 3,
}


public class LearnManager : MonoBehaviour
{
    public static LearnManager instance;
    public static List<Vector2> ListNegitives = new List<Vector2>() { new Vector2(1, 1), new Vector2(-1, 1), new Vector2(-1, -1), new Vector2(1, -1) };
    public static List<bool> InvertAngles = new List<bool>() { false, true, false, true };
    private void Awake() { instance = this; }
    //public CurrentLearn LearnType;
    public learningState state;
    [HideInInspector]
    public AllMotions motions;

    public List<AllMotions> MovementList;
    [Header("Using")]

    public bool HeadPos;
    public bool HeadRot;
    public bool HandPos;
    public bool HandRot;
    public bool HandVel;
    public bool AdjustedHandPos;

    [Header("References")]

    public HandActions Left;
    public HandActions Right;
    public Transform Cam;

    public delegate void EventHandlerTwo();
    public event EventHandlerTwo LearnReached;
    public List<Material> FalseTrue;

    [Header("Stats"), HideInInspector]

    public ControllerInfo Info;

    //public SingleInfo RightControllerStats;
    //[Header("OutputOnly")]
    //public bool GetOutput;

    public delegate void NewMotion(int Motion, int Set);
    public static event NewMotion OnNewMotion;

    [Header("Frames")]

    [HideInInspector] public int AgentsWaiting;
    [HideInInspector] public int Set;


    [Header("NEAT")]
    public float SpawnGap = 0.38f;
    private int InputCount, OutputCount;

    //[Header("Motions")]
    [HideInInspector] public int CurrentMotion;
    [HideInInspector] public int CurrentSet;

    [Header("NeatCount")]
    public List<SingleInfo> RightInfo;
    public List<SingleInfo> LeftInfo;

    [HideInInspector] public int MaxStoreInfo;
    [Header("NEAT Settings")]
    public bool ConvertToBytes;
    public bool ShouldRewardMultiplier;
    public bool ShouldPunishStreakGuess;
    public bool MultiplyByHighestGuess;
    public int MaxStreakPunish;

    public bool RewardOnFalse;

    public List<bool> AllowMotions;

    public int FramesToFeedAI;

    [Header("NEAT Stats")]
    private int LastGeneration;
    public List<float> MotionRewardMultiplier;

    public delegate void NewGen();
    public event NewGen OnNewGen;

    public Vector2 TotalValues;

    [HideInInspector] public bool FinishedAndWaiting;

    

    void Start()
    {
        StartCoroutine(ManageLists(1 / 60));
        SetSupervisorStats();
        UpdateRewardMultiplier();
        GetRandomMotion(out CurrentMotion, out CurrentSet);

        if (OnNewMotion != null)
            OnNewMotion(CurrentMotion, CurrentSet);
    }
    
    public List<CurrentLearn> GetAllMotions(int MotionNum, int SetNum)
    {
        List<CurrentLearn> LearnTypes = new List<CurrentLearn>();
        for (int i = 0; i < MovementList[MotionNum].Motions[SetNum].Infos.Count; i++)
        {
            LearnTypes.Add(MovementList[MotionNum].Motions[SetNum].AtFrameState(i) == true ? (CurrentLearn)MotionNum : CurrentLearn.Nothing);
        }
        return LearnTypes;
    }
    public bool ShouldPunish(int Streak) { return Streak >= MaxStreakPunish && ShouldPunishStreakGuess == true; }

    public void UpdateRewardMultiplier()
    {
        for (int i = 0; i < MovementList.Count; ++i)
            MotionRewardMultiplier.Add(0f);

        for (int i = 0; i < MotionRewardMultiplier.Count; ++i)
        {
            for (int j = 0; j < MovementList[i].Motions.Count; ++j)
            {
                MovementList[i].Motions[j].PlayCount = 0;


                List<int> FramesTotal = TotalOfMotion(i, j);
                float Weight = (1f/MovementList.Count) * (1f/MovementList[i].Motions.Count);
                float FrameCount = MovementList[i].Motions[j].Infos.Count;
                for (int k = 0; k < FramesTotal.Count; ++k)
                {
                    float FrameValue = Weight * (FramesTotal[k] / FrameCount);
                    //Debug.Log(" Weight: " + Weight + " FrameCount: " + FrameCount + " FramesTotal: " + FramesTotal[k] + " FrameValue: " + FrameValue + " K: " + k);
                    MotionRewardMultiplier[k] += FrameValue;
                }
            }
            
        }
        for (int i = 0; i < MotionRewardMultiplier.Count; ++i)
        {
            TotalValues = new Vector2(TotalValues.x + MotionRewardMultiplier[i], TotalValues.y);
            MotionRewardMultiplier[i] = 1f - MotionRewardMultiplier[i];
            TotalValues = new Vector2(TotalValues.x + MotionRewardMultiplier[i], TotalValues.y);
        }
            
        List<int> TotalOfMotion(int Motion, int Set)
        {
            List<int> Total = new List<int>();
            for (int i = 0; i < MovementList.Count; ++i)
                Total.Add(0);
            for (int i = 0; i < MovementList[Motion].Motions[Set].Infos.Count; ++i)
            {
                int ListAdd = MovementList[Motion].Motions[Set].AtFrameState(i) ? Motion : 0;
                Total[ListAdd] += 1;
            }
            return Total;
        }
    }
    private void Update()
    {
        int Generation = (int)GetComponent<UnitySharpNEAT.NeatSupervisor>().CurrentGeneration;
        if(Generation != LastGeneration)
        {
            LastGeneration = Generation;
            if(OnNewGen != null)
                OnNewGen();
        }
    }

    public void SetSupervisorStats()
    {
        InputCount = Inputs() * FramesToFeedAI;
        OutputCount = MovementList.Count;

        int Inputs() { return Space(HeadPos) + Space(HeadRot) + Space(HandPos) + Space(HandRot); }
        int Space(bool Bool) { return System.Convert.ToInt32(Bool) * 3; }
    }
    public void AgentWaiting()
    {
        AgentsWaiting += 1;
        if (AgentsWaiting == gameObject.GetComponent<UnitySharpNEAT.NeatSupervisor>()._spawnParent.childCount)
        {
            StartCoroutine(AgentCooldown());
        }
    }
    public void GetRandomMotion(out int Motion, out int Set)
    {
        Motion = Random.Range(0, MovementList.Count);
        while (AllowMotions[Motion] == false)
        {
            Motion = Random.Range(0, MovementList.Count);
        }
        Set = Random.Range(0, MovementList[Motion].Motions.Count);
        
    }
    public SingleInfo PastFrame(EditSide side, int FramesAgo)
    {
        List<SingleInfo> SideList = (side == EditSide.right) ? RightInfo : LeftInfo;
        return SideList[SideList.Count - FramesAgo];
    }
    IEnumerator AgentCooldown()
    {
        //DataTracker.instance.LogGuess(CurrentMotion, CurrentSet);
        FinishedAndWaiting = true;
        yield return new WaitForEndOfFrame();
        if (OnNewGen != null)
            OnNewGen();
        //feed frame
        GetRandomMotion(out CurrentMotion, out CurrentSet);
        MovementList[CurrentMotion].Motions[CurrentSet].PlayCount += 1;
        OnNewMotion(CurrentMotion, CurrentSet);
        AgentsWaiting = 0;
    }
    IEnumerator ManageLists(float Interval)
    {
        while (true)
        {
            RightInfo.Add(CurrentControllerInfo(EditSide.right));
            if (RightInfo.Count > MaxStoreInfo)
                RightInfo.RemoveAt(0);

            LeftInfo.Add(CurrentControllerInfo(EditSide.left));
            if (LeftInfo.Count > MaxStoreInfo)
                LeftInfo.RemoveAt(0);

            yield return new WaitForSeconds(Interval);
        }
    }

    

    public SingleInfo CurrentControllerInfo(EditSide side)
    {
        LearnManager LM = LearnManager.instance;
        SingleInfo newInfo = new SingleInfo(Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero);
        HandActions controller = side == EditSide.right ? LM.Right : LM.Left;

        newInfo.HeadPos = LM.Cam.localPosition;
        newInfo.HeadRot = LM.Cam.rotation.eulerAngles;
        if (side == EditSide.right)
        {
            float CamRot = LM.Cam.rotation.eulerAngles.y;
            Vector3 handPos = controller.transform.localPosition;
            newInfo.HeadPos = new Vector3(LM.Cam.localPosition.x, 0, LM.Cam.localPosition.z);
            Vector3 LevelCamPos = new Vector3(newInfo.HeadPos.x, 0, newInfo.HeadPos.z);
            Vector3 LevelHandPos = new Vector3(handPos.x, 0, handPos.z);
            Vector3 targetDir = LevelCamPos - LevelHandPos;

            Vector3 StartEulerAngles = LM.Cam.eulerAngles;
            LM.Cam.eulerAngles = new Vector3(0, CamRot, 0);

            Vector3 forwardDir = LM.Cam.rotation * Vector3.forward;
            LM.Cam.eulerAngles = StartEulerAngles;

            float NewHandCamAngle = Vector3.SignedAngle(targetDir, forwardDir, Vector3.up) + 180;

            return newInfo;
        }
        else
        {
            //CamRot
            float CamRot = LM.Cam.rotation.eulerAngles.y;
            Vector3 handPos = controller.transform.localPosition;
            float Distance = Vector3.Distance(new Vector3(handPos.x, 0, handPos.z), new Vector3(newInfo.HeadPos.x, 0, newInfo.HeadPos.z));
            //pos
            Vector3 LevelCamPos = new Vector3(newInfo.HeadPos.x, 0, newInfo.HeadPos.z);
            Vector3 LevelHandPos = new Vector3(handPos.x, 0, handPos.z);
            Vector3 targetDir = LevelCamPos - LevelHandPos;

            Vector3 StartEulerAngles = LM.Cam.eulerAngles;
            LM.Cam.eulerAngles = new Vector3(0, CamRot, 0);

            Vector3 forwardDir = LM.Cam.rotation * Vector3.forward;
            LM.Cam.eulerAngles = StartEulerAngles;

            float Angle = GetAngle(CamRot);

            //newInfo.AdjustedHandPos = IntoComponents(Angle);
            Vector2 XYForce = IntoComponents(Angle);
            Vector3 AdjustedCamPos = new Vector3(XYForce.x, 0, XYForce.y);

            Vector3 Point = (AdjustedCamPos * Distance) + new Vector3(newInfo.HeadPos.x, 0, newInfo.HeadPos.z);
            Point = new Vector3(Point.x, handPos.y, Point.z);
            newInfo.HandPos = Point;

            ///additional rotation
            Vector3 Rotation = controller.transform.rotation.eulerAngles;
            newInfo.HandRot = new Vector3(Rotation.x, Rotation.y, -Rotation.z);

            ///velocity
            Vector2 InputVelocity = new Vector2(controller.Velocity.x, controller.Velocity.z);
            Vector2 FoundVelocity = mirrorImage(IntoComponents(CamRot), InputVelocity);

            //newInfo.HandVel = new Vector3(FoundVelocity.x, controller.Velocity.y, FoundVelocity.y);

            //newInfo.Works = controller.TriggerPressed();
            return newInfo;
            Vector2 IntoComponents(float Angle)
            {
                int Quad = GetQuad(Angle, out float RemainingAngle);
                if (InvertAngles[Quad])
                    RemainingAngle = 90 - RemainingAngle;

                //Negitives
                float Radians = RemainingAngle * Mathf.Deg2Rad;
                //Debug.Log("remainAngle: " + Radians);

                float XForce = Mathf.Cos(Radians) * ListNegitives[Quad].x;
                float YForce = Mathf.Sin(Radians) * ListNegitives[Quad].y;

                return new Vector2(XForce, YForce);

                int GetQuad(float Angle, out float RemainAngle)
                {
                    //0-360
                    int TotalQuads = (int)(Angle / 90);
                    RemainAngle = Angle - TotalQuads * 90;
                    //int Quad = StartQuad;
                    if (TotalQuads < 0)
                    {
                        TotalQuads += 4;
                    }

                    int RemoveExcessQuads = (int)(TotalQuads / 4);
                    //RemainAngle = Angle;
                    return TotalQuads - RemoveExcessQuads * 4;
                }
            }
            static Vector2 mirrorImage(Vector2 Line, Vector2 Pos)
            {
                float temp = (-2 * (Line.x * Pos.x + Line.y * Pos.y)) / (Line.x * Line.x + Line.y * Line.y);
                float x = (temp * Line.x) + Pos.x;
                float y = (temp * Line.y) + Pos.y;
                return new Vector2(x, y);
            }
            float GetAngle(float CamRot)
            {
                float Angle = 360 - (CamRot + Vector3.SignedAngle(targetDir, forwardDir, Vector3.up) + 180 + 270);
                //Offset
                if (Angle > 360)
                    Angle -= 360;
                else if (Angle < -360)
                    Angle += 360;
                return Angle;
            }
        }

    }
}
[System.Serializable]
public class ControllerInfo
{
    
    public List<Transform> TestMain;
    public List<Transform> TestCam;
    public List<Transform> TestHand;

    public HandActions MyHand(EditSide side) { return (side == EditSide.right) ? LearnManager.instance.Right : LearnManager.instance.Left; }
    public SingleInfo GetControllerInfo(EditSide side)
    {
        Transform Cam = LearnManager.instance.Cam;
        SetReferences();
        Vector3 CamPos = Cam.localPosition;
        TestCam[(int)side].position = Vector3.zero;
        TestHand[(int)side].position = TestHand[(int)side].position - CamPos;

        float YDifference = -Cam.localRotation.eulerAngles.y;
        
        //invert main to y distance
        if (side == EditSide.left)
        {
            TestMain[(int)side].localScale = new Vector3(-1, 1, 1);
            Vector3 Rot = TestCam[(int)side].eulerAngles;
            TestCam[(int)side].eulerAngles = new Vector3(Rot.x, -Rot.y, -Rot.z);
        }
            
        TestMain[(int)side].rotation = Quaternion.Euler(0, YDifference, 0);
        //TestCam[(int)side].localRotation = Cam.localRotation;
        return new SingleInfo(TestHand[(int)side].position, TestHand[(int)side].rotation.eulerAngles, TestCam[(int)side].position, TestCam[(int)side].rotation.eulerAngles); ;

        void SetReferences()
        {
            TestMain[(int)side].position = Vector3.zero;
            TestMain[(int)side].rotation = Quaternion.identity;
            TestMain[(int)side].localScale = new Vector3(1, 1, 1);
            SetEqual(Cam, TestCam[(int)side]);
            SetEqual(MyHand(side).transform, TestHand[(int)side]);
            void SetEqual(Transform Info, Transform Set)
            {
                Set.localPosition = Info.localPosition;
                Set.localRotation = Info.localRotation;
            }
        }
    }
}
