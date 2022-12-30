using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
public class DataTracker : SerializedMonoBehaviour
{
    public static DataTracker instance;
    private void Awake() { instance = this; }
    [FoldoutGroup("CurrentInfo"), ReadOnly] public int TotalLogCount;
    [FoldoutGroup("CurrentInfo"), ReadOnly] public Vector2 Current;
    [FoldoutGroup("CurrentInfo"), ReadOnly] public Vector2 CurrentTotal;
    [FoldoutGroup("CurrentInfo"), ReadOnly] public Vector2 AbsoluteTotal;
    [FoldoutGroup("CurrentInfo"), ReadOnly] public List<Vector2> Past;

    [FoldoutGroup("CurrentInfo"), ReadOnly] public List<AIStat> Stats;
    [FoldoutGroup("CurrentInfo"), ReadOnly] public List<AIStat> PastFrameInfoKeepForTesting;

    [FoldoutGroup("CurrentInfo"), ReadOnly] public List<int> SpellCalls;


    //[FoldoutGroup("Stats")] 
    public float RefreshInterval = 2f;
    //[FoldoutGroup("Stats")] 
    public bool DebugOnRefresh;
    public int PastFrameInfoKeep;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
            CurrentTotal = Vector2.zero;
    }
    public void LogGuess()
    {
        LearnManager LM = LearnManager.instance;
        TotalLogCount += 1;

        int Motion = LM.LoggingAI().MotionIndex;
        int Set = LM.LoggingAI().Set;

        bool AtFrameState = LM.MovementList[Motion].Motions[Set].AtFrameState(LM.LoggingAI().Frame);
        CurrentLearn Truth = (LM.AtFrameStateAlwaysTrue) ? ((CurrentLearn)Motion) : (AtFrameState ? (CurrentLearn)Motion : CurrentLearn.Nothing);
        //Debug.Log("Motion: " + (CurrentLearn)Motion + "  Truth: " + Truth + "");
        //Debug.Log("Same: " + ((CurrentLearn)Motion == Truth));
        SpellCalls[(int)Truth] += 1;

        CurrentLearn Guess = LM.LoggingAI().CurrentGuess;

        int PlayCount = LM.MovementList[Motion].Motions[Set].PlayCount;
        
        Current = (Guess == Truth) ? new Vector2(Current.x + 1, Current.y) : new Vector2(Current.x, Current.y + 1);
        CurrentTotal = (Guess == Truth) ? new Vector2(CurrentTotal.x + 1, CurrentTotal.y) : new Vector2(CurrentTotal.x, CurrentTotal.y + 1);
        AbsoluteTotal = (Guess == Truth) ? new Vector2(AbsoluteTotal.x + 1, AbsoluteTotal.y) : new Vector2(AbsoluteTotal.x, AbsoluteTotal.y + 1);
        //stat.
        Stats.Add(new AIStat(Motion, Set, PlayCount, Guess, Truth));
        PastFrameInfoKeepForTesting.Add(new AIStat(Motion, Set, PlayCount, Guess, Truth));
        if (PastFrameInfoKeepForTesting.Count > PastFrameInfoKeep)
            PastFrameInfoKeepForTesting.RemoveAt(0);
    }
    void Start()
    {
        for (int i = 0; i < LearnManager.instance.MovementList.Count; ++i)
            SpellCalls.Add(1);
        UnitySharpNEAT.LearningAgent.OnLog += LogGuess;

        StartCoroutine(RefreshWait());
    }
    IEnumerator RefreshWait()
    {
        while (true)
        {
            if(Current != Vector2.zero)
            {
                if (DebugOnRefresh)
                    Debug.Log("Right: %" + (Current.x / (Current.x + Current.y)) * 100);
                Past.Add(Current);
                Current = Vector2.zero;
            }
            yield return new WaitForSeconds(RefreshInterval);
        }
    }
}

[System.Serializable]
public class AIStat
{
    public int Motion, Set;
    public int MotionPlayNum;
    public float Timer;
    public CurrentLearn Guess, Truth;
    public AIStat(int MotionStat, int SetStat, int MotionPlayNumStat, CurrentLearn GuessStat, CurrentLearn TruthStat)
    {
        Motion = MotionStat;
        Set = SetStat;
        MotionPlayNum = MotionPlayNumStat;
        Guess = GuessStat;
        Truth = TruthStat;
        Timer = Time.timeSinceLevelLoad;
    }
}


