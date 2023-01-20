using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;
using UnityEditor;
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
    private void Awake()
    {
        instance = this;
        NeatSupervisor()._networkInputCount = MatrixManager.instance.Width * MatrixManager.instance.Height * InfoCountInMatrixSingle();
        NeatSupervisor()._networkOutputCount = UseAgentSingleOutput ? 1 : 4;
    }
    //public CurrentLearn LearnType;
    [HideInInspector] public learningState state;
    public bool LearnOnStart;

    public void Save()
    {
        //AssetDatabase
    }


    [FoldoutGroup("References")] public List<AllMotions> MovementList;

    [FoldoutGroup("References")] public HandActions Left;
    [FoldoutGroup("References")] public HandActions Right;
    [FoldoutGroup("References")] public Transform Cam;
    [FoldoutGroup("References")] public List<Material> FalseTrue;

    
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
    public int AICount() { return NeatSupervisor().Experiment.DefaultPopulationSize; }

    public int InfoCountInMatrixSingle() { return 5; }
    #endregion
    [FoldoutGroup("NeatDisplay"), ReadOnly] public int AgentsWaiting;
    [FoldoutGroup("NeatDisplay"), ReadOnly] public List<float> WeightedRewardMultiplier;
    

    [FoldoutGroup("NEAT Settings")] public bool ConvertToBytes;
    [FoldoutGroup("NEAT Settings")] public bool UseWeightedRewardMultiplier;
    [FoldoutGroup("NEAT Settings")] public bool MultiplyByHighestGuess;
    [FoldoutGroup("NEAT Settings")] public bool RewardOnInterpolation = false;
    ///multiply by certainty value
    [FoldoutGroup("NEAT Settings")] public bool ShouldPunishStreakGuess;
    [FoldoutGroup("NEAT Settings"), ShowIf("ShouldPunishStreakGuess")] public int MaxStreakPunish;
    [FoldoutGroup("NEAT Settings"), ShowIf("ShouldPunishStreakGuess")] public int StreakPunishAmount;

    [FoldoutGroup("NEAT Settings")] public bool RewardNothingGuess;
    [FoldoutGroup("NEAT Settings")] public bool AtFrameStateAlwaysTrue;

    [FoldoutGroup("NEAT Settings")] public bool InputAllGrid;

    [FoldoutGroup("NEAT Settings")] public bool WeightedMotionPick;
    [FoldoutGroup("NEAT Settings"), ShowIf("WeightedMotionPick")] public bool AutoPickChance;
    [FoldoutGroup("NEAT Settings"), HideIf("AutoPickChance")] public List<float> OverridePickChances;

    [FoldoutGroup("NEAT Settings")] public bool DisallowMotions;
    [FoldoutGroup("NEAT Settings"), ShowIf("DisallowMotions")] public List<bool> UnallowedMotions;

    [FoldoutGroup("NEAT Settings")] public bool UseAgentSingleOutput;

    private int LastGeneration;
    public float SpawnGap = 0.38f;
    public bool ShouldDebug;
    public float SecondsPerAiAction;

    [FoldoutGroup("Interpolation")] public int TotalInterprolateFrames;
    [FoldoutGroup("Interpolation"), ReadOnly] public List<SingleInfo> interpolateFrames;

    //public List<int> TotalPicks;
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

    public float InputWaitTime;

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
        

        for (int i = 0; i < MovementList.Count; ++i)
            for (int j = 0; j < MovementList[i].Motions.Count; ++j)
                MovementList[i].Motions[j].PlayCount = 0;


        if (!LearnOnStart)
            return; 
        UnitySharpNEAT.ExperimentIO.DeleteAllSaveFiles(NeatSupervisor().Experiment);
        NeatSupervisor().StartEvolution();
    }
    public void GetRandomMotionAndEvent()
    {
        //Debug.Log("rand");
        CurrentMotion = NextMotion;
        CurrentSet = NextSet;
        GetRandomMotion(out NextMotion, out NextSet);
        PickManager.instance.TotalPicks[CurrentMotion] += 1;
        PickManager.instance.UpdateAdjustedPickChances();
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
                    for (int i = 0; i < PickManager.instance.AdjustedPickChances.Count; i++)
                        Max += PickManager.instance.AdjustedPickChances[i];
                    float RandomPick = Random.Range(0, Max);
                    float CorrectMax = 0;
                    for (int i = 0; i < PickManager.instance.AdjustedPickChances.Count; i++)
                    {
                        CorrectMax += PickManager.instance.AdjustedPickChances[i];
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
        }
    }
    public bool ShouldPunishStreak(int Streak) { return Streak >= MaxStreakPunish && ShouldPunishStreakGuess == true; }
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

    

    
    
    
    
}

/*
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
    */
