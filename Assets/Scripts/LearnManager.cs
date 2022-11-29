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
    private void Awake()
    {
        instance = this;

        //motions = MovementList[(int)LearnType];
        //for (int i = 0; i < motions.Motions.Count; i++)
            //motions.Motions[i].TrueIndex = i;
    }
    public CurrentLearn LearnType;
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
    
    public float TrueAhead;
    public Vector2 EachAdd;

    //public int RightIndex;
    //public int WrongIndex;

    public delegate void EventHandlerTwo();
    public event EventHandlerTwo LearnReached;
    public float Timer;
    public List<Material> FalseTrue;

    [Header("Stats"), HideInInspector]
    public int VectorObvervationCount;

    public ControllerInfo Info;

    public SingleInfo RightControllerStats;
    public List<Vector2> RightWrongStats;
    public List<int> Rights;
    public List<int> Wrongs;
    [Header("OutputOnly")]
    public bool GetOutput;

    public delegate void NewMotion(int Motion, int Set);
    public static event NewMotion OnNewMotion;

    [Header("Frames")]
    //public int NEATinputFrames;
    public int FeedFrames;
    public int AgentsWaiting;

    public int Set;
    
    [Header("NEAT")]
    public float SpawnGap;
    public bool IsLearning;


    [Header("Rewards")]
    public float FalseMultiplier;
    public float RewardMultiplier;

    public int CurrentMotion;
    public int CurrentSet;
    public float GetReward(int Streak)
    {
        return Streak * RewardMultiplier;
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
        Set = Random.Range(0, MovementList[Motion].Motions.Count);
    }
    IEnumerator AgentCooldown()
    {
        yield return new WaitForEndOfFrame();
        //feed frame
        GetRandomMotion(out CurrentMotion, out CurrentSet);
        OnNewMotion(CurrentMotion, CurrentSet);
        AgentsWaiting = 0;
    }
   
    
    void Start()
    {
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
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.S))
        {
            if(IsLearning == false)
            {
                StartCoroutine(StartEvolutionSequence());
                IsLearning = true;
            }
        }
    }
    IEnumerator StartEvolutionSequence()
    {   
        gameObject.GetComponent<UnitySharpNEAT.NeatSupervisor>().StartEvolution();
        yield return new WaitForEndOfFrame();
        GetRandomMotion(out CurrentMotion, out CurrentSet);
        OnNewMotion(CurrentMotion, CurrentSet);
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
