using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;
public enum CurrentLearn
{
    Nothing = 0,
    Fireball = 1,
    Flames = 2,
    FlameBlock = 3,
}


public class LearnManager : SerializedMonoBehaviour
{
    public static LearnManager instance;
    public static List<Vector2> ListNegitives = new List<Vector2>() { new Vector2(1, 1), new Vector2(-1, 1), new Vector2(-1, -1), new Vector2(1, -1) };
    public static List<bool> InvertAngles = new List<bool>() { false, true, false, true };
    private void Awake() { instance = this; }
    //public CurrentLearn LearnType;
    [HideInInspector] public learningState state;
    [HideInInspector] public AllMotions motions;
    public bool LearnOnStart;

    [FoldoutGroup("References")] public List<AllMotions> MovementList;

    [FoldoutGroup("References")] public HandActions Left;
    [FoldoutGroup("References")] public HandActions Right;
    [FoldoutGroup("References")] public Transform Cam;
    [FoldoutGroup("References")] public List<Material> FalseTrue;

    [FoldoutGroup("NeatDisplay"), ReadOnly] public ControllerInfo Info;
    [FoldoutGroup("NeatDisplay"), ReadOnly] public int CurrentMotion;
    [FoldoutGroup("NeatDisplay"), ReadOnly] public int CurrentSet;

    [FoldoutGroup("NeatDisplay"), ReadOnly] public int NextMotion;
    [FoldoutGroup("NeatDisplay"), ReadOnly] public int NextSet;
    #region InfoMethods
    public int CurrentFrame() { return LoggingAI().Frame; }
    public int CurrentInterpolate() { return LoggingAI().Frame; }
    public UnitySharpNEAT.LearningAgent LoggingAI() { return (NeatSupervisor()._spawnParent.childCount == 0) ? null : NeatSupervisor()._spawnParent.GetChild(0).GetComponent<UnitySharpNEAT.LearningAgent>(); }
    public UnitySharpNEAT.NeatSupervisor NeatSupervisor() { return (GetComponent<UnitySharpNEAT.NeatSupervisor>() == null) ? null: gameObject.GetComponent<UnitySharpNEAT.NeatSupervisor>(); }
    public SingleInfo CurrentSingleInfo()
    {
        if (LoggingAI().Active() && LoggingAI().IsInterpolating() == false) //main counting
            return MovementList[CurrentMotion].Motions[CurrentSet].Infos[CurrentFrame()];
        else if (LoggingAI().IsInterpolating())// interpolate counting
            return interpolateFrames[interpolateFrames.Count - (LoggingAI().InterpolateFrames.Count)];

        return null;
    }
    #endregion
    [FoldoutGroup("NeatDisplay"), ReadOnly] public List<SingleInfo> RightInfo;
    [FoldoutGroup("NeatDisplay"), ReadOnly] public List<SingleInfo> LeftInfo;
    [FoldoutGroup("NeatDisplay"), ReadOnly] public int AgentsWaiting;
    [FoldoutGroup("NeatDisplay"), ReadOnly] public List<float> WeightedRewardMultiplier;
    [HideInInspector] public int MaxStoreInfo = 10;

    [FoldoutGroup("NEAT Settings")] public bool ConvertToBytes;
    [FoldoutGroup("NEAT Settings")] public bool UseWeightedRewardMultiplier;
    [FoldoutGroup("NEAT Settings")] public bool MultiplyByHighestGuess;
    [FoldoutGroup("NEAT Settings")] public bool RewardOnInterpolation = false;

    [FoldoutGroup("NEAT Settings")] public bool ShouldPunishStreakGuess;
    [FoldoutGroup("NEAT Settings"), ShowIf("ShouldPunishStreakGuess")] public int MaxStreakPunish;

    [FoldoutGroup("NEAT Settings")] public bool RewardNothingGuess;
    [FoldoutGroup("NEAT Settings")] public bool AtFrameStateAlwaysTrue;

    [FoldoutGroup("NEAT Settings")] public bool WeightedMotionPick;
    [FoldoutGroup("NEAT Settings"), ShowIf("WeightedMotionPick")] public bool AutoPickChance;
    [FoldoutGroup("NEAT Settings"), HideIf("AutoPickChance")] public List<float> OverridePickChances;

    [FoldoutGroup("NEAT Settings")] public bool DisallowMotions;
    [FoldoutGroup("NEAT Settings"), ShowIf("DisallowMotions")] public List<bool> UnallowedMotions;

    private int LastGeneration;
    public float SpawnGap = 0.38f;
    public bool ShouldDebug;
    public float SecondsPerAiAction;

    [FoldoutGroup("Interpolation")] public int TotalInterprolateFrames;
    [FoldoutGroup("Interpolation"), ReadOnly] public List<SingleInfo> interpolateFrames;

    [FoldoutGroup("FrameData"), ReadOnly] public List<MotionFrames> EachMotionFrameCount;
    [FoldoutGroup("FrameData"), ReadOnly] public List<int> TotalFrameCount;
    [FoldoutGroup("FrameData"), ReadOnly] public List<float> PickChances;
    [FoldoutGroup("FrameData"), ReadOnly] public List<int> TotalPicks;
    [FoldoutGroup("FrameData"), ReadOnly] public List<float> AverageFramesInMotions;
    #region Events
    public delegate void NoInfo();
    public static event NoInfo OnIntervalReached;
    public static event NoInfo OnAlgorithmStart;

    
    public void StartAlgorithmSequence() { StartCoroutine(AlgorithmSequence()); }
    public IEnumerator AlgorithmSequence()
    {
        while (OnNewMotion == null)
            yield return new WaitForEndOfFrame();

        if (OnAlgorithmStart != null)
            OnAlgorithmStart();
        //Debug.Log("start");
        //GetRandomMotionAndEvent
        GetRandomMotionAndEvent();
        StartCoroutine(IntervalMotion());
    }
    public event NoInfo OnNewGen;

    public delegate void NewMotion(int Motion, int Set, List<SingleInfo> InterpolateFrames);
    public static event NewMotion OnNewMotion;
    #endregion

    public bool Multiplier;


    [System.Serializable]
    public class MotionFrames
    {
        public List<int> MotionCounts = new List<int>();
        public MotionFrames(int Count)
        {
            for (int i = 0; i < Count; ++i)
                MotionCounts.Add(0);
        }
    }
    public List<MotionFrames> GetEachMotionFrameCount()
    {
        List<MotionFrames> FrameCountList = new List<MotionFrames>();
        for (int i = 0; i < MovementList.Count; ++i)
            FrameCountList.Add(new MotionFrames(MovementList.Count));
        for (int i = 0; i < MovementList.Count; ++i)
        {
            for (int j = 0; j < MovementList[i].Motions.Count; ++j)
            {
                for (int k = 0; k < MovementList[i].Motions[j].Infos.Count; ++k)
                {
                    int ListAdd = (AtFrameStateAlwaysTrue) ? i : (MovementList[i].Motions[j].AtFrameState(k) ? i : 0);
                    FrameCountList[i].MotionCounts[ListAdd] += 1;
                }
            }
        }
        return FrameCountList;
    }
    public List<float> GetPickChances()
    {
        List<float> Chances = new List<float>();
        AverageFramesInMotions = new List<float>();
        //AtFrameStateAlwaysTrue
        float CombinedAverageFrames = 0;
        float TotalFramesInAllMotions = 0;
        //TotalFrameCount = GetTotalFrames();
        for (int i = 0; i < EachMotionFrameCount.Count; ++i)
        {
            Chances.Add(0f);
            TotalFramesInAllMotions += GetTotalMotionFrames(i);

            TotalFrameCount.Add(GetTotalMotionFrames(i));
            
            float AverageFramesInMotion = (float)GetTotalMotionFrames(i) / (float)MovementList[i].Motions.Count;
            Debug.Log(AverageFramesInMotion);
            AverageFramesInMotions.Add(AverageFramesInMotion);
            CombinedAverageFrames += AverageFramesInMotion;
        }


        if (AtFrameStateAlwaysTrue)
        {
            
            for (int i = 0; i < EachMotionFrameCount.Count; ++i)
            {
                //Chances[i] = 1f/ (GetTotalMotionFrames(i) / TotalFramesInMotion);
                Debug.Log(GetTotalMotionFrames(i));
                float FramesPerMotionAverage = GetTotalMotionFrames(i) / TotalFramesInAllMotions;
                float Inverse = 1f / AverageFramesInMotions[i];
                float Value = Inverse * MovementList.Count;
                Chances[i] = (Value);
            }
            

            /*
            for (int i = 0; i < EachMotionFrameCount.Count; ++i)
            {
                //Chances[i] = 1f/ (GetTotalMotionFrames(i) / TotalFramesInMotion);
                float FramesPerMotionAverage = GetTotalMotionFrames(i) / MovementList[i].Motions.Count;
                float Inverse = 1f / FramesPerMotionAverage;
                float Value = Inverse;
                Chances[i] = (Value);
            }
            */
        }
        else
        {
            for (int i = 0; i < EachMotionFrameCount.Count; ++i)
                for (int j = 0; j < EachMotionFrameCount[i].MotionCounts.Count; ++j)
                {
                    float ThisSubFrameCount = EachMotionFrameCount[i].MotionCounts[j]; // current frames in parent     //10 sets
                    float PercentOfAverageFramesPerMotion = ThisSubFrameCount / GetTotalMotionFrames(i); //40 flame frames out of 100 = 40% are flame (0.4)
                    float ThisFramesOfAverage = PercentOfAverageFramesPerMotion * AverageFramesInMotions[i]; //40% average frames per motion (10) = 4 average flame frames per motion
                    float InverseOfAverageThisFrame = ThisFramesOfAverage; //the opposite number of the average frames: 1 / 4 = 0.25
                    float ChanceAddForThisMotion = (ThisSubFrameCount != 0f) ? InverseOfAverageThisFrame : 0f; //if 0 frames do nothing

                    Chances[j] += ChanceAddForThisMotion;
                    if (ThisSubFrameCount != 0f)
                        Debug.Log("I: " + (CurrentLearn)i + "  J: " + (CurrentLearn)j +
                        "  ThisSubFrameCount: " + ThisSubFrameCount +
                        "  TotalFramesMotionParent: " + GetTotalMotionFrames(i) +
                        "  MyWeightOfTotal: " + (PercentOfAverageFramesPerMotion * 100f) + "%" +
                        "  AverageFramesPerMotion: " + AverageFramesInMotions[i] +
                        "  ThisFramesOfAverage: " + ThisFramesOfAverage +
                        "  ChanceAddForThisMotion: " + ChanceAddForThisMotion
                        //"  Value: " + (EachMotionFrameCount[i].MotionCounts[j] / MovementList[i].Motions.Count)
                        );

                    
                }
            float CurrentTotal = GetTotal(Chances);
            for (int i = 0; i < Chances.Count; ++i)
            {

                float PercentOfWhole = Chances[i] / CurrentTotal;
                //Debug.Log("i: " + i + "  PercentOfWhole: " + PercentOfWhole);
                //Chances[i] = 1f / Chances[i];
                Chances[i] = 1f / PercentOfWhole;

                //Chances[i] = 1f / Chances[i];
            }

            //Debug.Log("Total: " + Total);
            //for (int i = 0; i < Chances.Count; ++i)
            //Debug.Log("i: " + i + "  Weight2: " + Chances[i]);
        }
        /*
        float TotalCurrent = 0;
        for (int i = 0; i < Chances.Count; ++i)
        {
            TotalCurrent += Chances[i];
            Debug.Log("Chances: " + TotalCurrent); 
        }
        */


        float Total = GetTotal(Chances);
        if (Multiplier)
        {
            float Multiplier = 100f / Total;
            for (int i = 0; i < MovementList.Count; ++i)
                Chances[i] = Multiplier * Chances[i];
        }
        return Chances;
        int GetTotalMotionFrames(int Index)
        {
            int Total = 0;
            for (int j = 0; j < EachMotionFrameCount.Count; ++j)
                Total += EachMotionFrameCount[Index].MotionCounts[j];
            //Debug.Log(Total);
            return Total;
        }
        float GetTotal(List<float> Chances)
        {
            float Total = 0;
            for (int i = 0; i < Chances.Count; ++i)
                Total += Chances[i];
            return Total;
        }
    }
    public List<SingleInfo> InterpolatePositions(Vector3 from, Vector3 to)
    {
        List<SingleInfo> LerpList = new List<SingleInfo>();
        float EachChange = 1f / (TotalInterprolateFrames + 1f);
        float Current = EachChange;
        for (int i = 0; i < TotalInterprolateFrames; i++)
        {
            LerpList.Add(new SingleInfo(Vector3.Lerp(from, to, Current), Vector3.zero, Vector3.zero, Vector3.zero));
            Current += EachChange;
        }
        return LerpList;
    }
    void Start()
    {
        StartCoroutine(ManageLists(1 / 60));
        EachMotionFrameCount = GetEachMotionFrameCount();

        PickChances = (AutoPickChance) ? GetPickChances() : OverridePickChances;
        for (int i = 0; i < MovementList.Count; ++i)
            TotalPicks.Add(0);
        if (ShouldDebug)
            Debug.Log("IdealCapacity: " + 2 * MatrixManager.instance.Height * MatrixManager.instance.Width);

        for (int i = 0; i < MovementList.Count; ++i)
            for (int j = 0; j < MovementList[i].Motions.Count; ++j)
                MovementList[i].Motions[j].PlayCount = 0;

        UnitySharpNEAT.ExperimentIO.DeleteAllSaveFiles(NeatSupervisor().Experiment);
        NeatSupervisor().StartEvolution();
    }
    public void GetRandomMotionAndEvent()
    {
        //Debug.Log("rand");
        CurrentMotion = NextMotion;
        CurrentSet = NextSet;
        GetRandomMotion(out NextMotion, out NextSet);
        TotalPicks[CurrentMotion] += 1;
        int MaxFrom = MovementList[CurrentMotion].Motions[CurrentSet].Infos.Count - 1;
        Vector3 From = MovementList[CurrentMotion].Motions[CurrentSet].Infos[MaxFrom].HandPos;
        Vector3 To = MovementList[NextMotion].Motions[NextSet].Infos[0].HandPos;
        interpolateFrames = InterpolatePositions(From, To);
        if (OnNewMotion != null)
            OnNewMotion(CurrentMotion, CurrentSet, interpolateFrames);

        void GetRandomMotion(out int Motion, out int Set)
        {
            Motion = GetMotion();
            //Debug.Log("Motion: " + Motion);
            Set = Random.Range(0, MovementList[Motion].Motions.Count);

            int GetMotion()
            {
                if (WeightedMotionPick)
                {
                    float Max = 0;
                    for (int i = 0; i < PickChances.Count; i++)
                        Max += PickChances[i];
                    float RandomPick = Random.Range(0, Max);
                    float CorrectMax = 0;
                    for (int i = 0; i < PickChances.Count; i++)
                    {
                        CorrectMax += PickChances[i];
                        if (RandomPick < CorrectMax)
                        {
                            if (ShouldDebug)
                                Debug.Log("RandomPick: " + RandomPick + "  Motion: " + (CurrentLearn)i);
                            return i;
                        }
                    }
                    Debug.LogError("no pick at  Max: " + Max + "  RandomPick: " + RandomPick);
                    return 5;
                }
                else
                {
                    if (!DisallowMotions)
                        return Random.Range(0, MovementList.Count);

                    while (true)
                    {
                        int CurrentGuess = Random.Range(0, MovementList.Count);
                        if (UnallowedMotions[CurrentGuess] == true)
                            return CurrentGuess;
                    }


                }
            }
        }
    }
    IEnumerator IntervalMotion()
    {
        while (true)
        {
            yield return new WaitForSeconds(SecondsPerAiAction);
            //
            if (OnIntervalReached != null)
                OnIntervalReached();

            //MatrixManager.instance.ResetMatrix();
            //GetRandomMotionAndEvent();
        }
    }
    public bool ShouldPunish(int Streak) { return Streak >= MaxStreakPunish && ShouldPunishStreakGuess == true; }
    private void Update()
    {
        int Generation = (int)GetComponent<UnitySharpNEAT.NeatSupervisor>().CurrentGeneration;
        if (Generation != LastGeneration)
        {
            LastGeneration = Generation;
            //MatrixManager.instance.ResetMatrix();
            GetRandomMotionAndEvent();
            AgentsWaiting = 0;

            if (OnNewGen != null)
                OnNewGen();
        }
    }
    public void AgentWaiting()
    {
        AgentsWaiting += 1;

        if (AgentsWaiting == gameObject.GetComponent<UnitySharpNEAT.NeatSupervisor>()._spawnParent.childCount)
        {
            StartCoroutine(AgentCooldown());
        }
    }
    IEnumerator AgentCooldown()
    {
        //DataTracker.instance.LogGuess(CurrentMotion, CurrentSet);
        //FinishedAndWaiting = true;
        yield return new WaitForEndOfFrame();

        AgentsWaiting = 0;
        MovementList[CurrentMotion].Motions[CurrentSet].PlayCount += 1;


        //if (OnNewGen != null)
        //OnNewGen();
        //feed frame
        GetRandomMotionAndEvent();

        //OnNewMotion(CurrentMotion, CurrentSet);
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
    public SingleInfo PastFrame(EditSide side, int FramesAgo)
    {
        List<SingleInfo> SideList = (side == EditSide.right) ? RightInfo : LeftInfo;
        return SideList[SideList.Count - FramesAgo];
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
