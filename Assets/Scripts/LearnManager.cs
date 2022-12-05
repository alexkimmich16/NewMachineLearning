using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
    public int FeedFrames;
    [HideInInspector] public int AgentsWaiting;
    [HideInInspector] public int Set;


    [Header("NEAT")]
    public float SpawnGap;
    private int InputCount, OutputCount;

    [Header("Rewards")]
    public float RewardMultiplier;

    //[Header("Motions")]
    [HideInInspector] public int CurrentMotion;
    [HideInInspector] public int CurrentSet;

    [Header("NeatCount")]
    

    public List<SingleInfo> RightInfo;
    public List<SingleInfo> LeftInfo;

    public int MaxStoreInfo;

    public bool ConvertToBytes;

    //public int TotalFrameTest;
    public int LastGeneration;

    public delegate void NewGen();
    public event NewGen OnNewGen;

    public List<bool> AllowMotions;

    public bool RewardOnFalse;

    private void Update()
    {
        int Generation = (int)GetComponent<UnitySharpNEAT.NeatSupervisor>().CurrentGeneration;
        if(Generation != LastGeneration)
        {
            LastGeneration = Generation;
            OnNewGen();
            ///OnNewGeneration()
        }
    }

    public void SetSupervisorStats()
    {
        InputCount = Inputs() * FeedFrames;
        OutputCount = MovementList.Count;

        //gameObject.GetComponent<UnitySharpNEAT.NeatSupervisor>()._networkInputCount = Inputs() * FeedFrames;
        //gameObject.GetComponent<UnitySharpNEAT.NeatSupervisor>()._networkOutputCount = MovementList.Count;
        int Inputs() { return Space(HeadPos) + Space(HeadRot) + Space(HandPos) + Space(HandRot); }
        int Space(bool Bool) { return System.Convert.ToInt32(Bool) * 3; }
    }
    
    public float GetReward(int Streak)
    {
        //return Streak * RewardMultiplier;
        return (Streak > 0) ? 100f : 0;
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
        yield return new WaitForEndOfFrame();
        //GetComponent<UnitySharpNEAT.NeatSupervisor>().RunBest();
        //feed frame
        GetRandomMotion(out CurrentMotion, out CurrentSet);
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

    void Start()
    {
        StartCoroutine(ManageLists(1/ 60));
        SetSupervisorStats();

        GetRandomMotion(out CurrentMotion, out CurrentSet);
        if(OnNewMotion != null)
            OnNewMotion(CurrentMotion, CurrentSet);


        /*
        for (int i = 0; i < motions.Motions.Count; i++)
        {
            RightWrongStats.Add(GetRightWrongAt(i));
            int TrueAhead = (int)(RightWrongStats[i].x - RightWrongStats[i].y);
            if (TrueAhead > 0)
                Rights.Add(i);
            else
                Wrongs.Add(i);
        }
        */
    }

    /*
    public Vector2 GetRightWrongTotal()
    {
        Vector2 RightWrongCount = Vector2.zero;
        for (int i = 0; i < RightWrongStats.Count; i++)
            RightWrongCount = RightWrongCount + RightWrongStats[i];
        return RightWrongCount;
    }
    public Vector2 GetRightWrongAt(int MotionNum)
    {
        int RightCount = 0;
        int WrongCount = 0;
        if (motions.Motions.Count == 0)
            return Vector2.zero;
        for (int j = 0; j < motions.Motions[MotionNum].Infos.Count; j++)
            if (motions.Motions[MotionNum].AtFrameState(j) == true)
                RightCount += 1;
            else if (motions.Motions[MotionNum].AtFrameState(j) == false)
                WrongCount += 1;
        return new Vector2(RightCount, WrongCount);
    }
    */
    public SingleInfo CurrentControllerInfo(EditSide side)
    {
        LearnManager LM = LearnManager.instance;
        SingleInfo newInfo = new SingleInfo();
        bool ConstantRot = true;
        HandActions controller = side == EditSide.right ? LM.Right : LM.Left;

        newInfo.HeadPos = LM.Cam.localPosition;
        newInfo.HeadRot = LM.Cam.rotation.eulerAngles;
        if (side == EditSide.right)
        {
            if (ConstantRot == false)
            {
                newInfo.HandPos = controller.transform.localPosition;
                newInfo.HandRot = controller.transform.localRotation.eulerAngles;
                newInfo.HandVel = controller.Velocity;

                newInfo.AdjustedHandPos = LM.Cam.position - controller.transform.position;

                return newInfo;
            }
            else
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

            newInfo.AdjustedHandPos = IntoComponents(Angle);
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
            newInfo.HandVel = new Vector3(FoundVelocity.x, controller.Velocity.y, FoundVelocity.y);

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

    HandActions MyHand(EditSide side)
    {
        if (side == EditSide.right)
            return LearnManager.instance.Right;
        else
            return LearnManager.instance.Left;
    }
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
        return ReturnTest();


        SingleInfo ReturnTest()
        {
            SingleInfo newInfo = new SingleInfo();
            newInfo.HeadPos = TestCam[(int)side].position;
            newInfo.HeadRot = TestCam[(int)side].rotation.eulerAngles;
            newInfo.HandPos = TestHand[(int)side].position;
            newInfo.HandRot = TestHand[(int)side].rotation.eulerAngles;
            return newInfo;
        }
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
