using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
public class DataTracker : SerializedMonoBehaviour
{
    public static DataTracker instance;
    private void Awake() { instance = this; }
    [Header("RightVSWrong")]
    public Vector2 Current;
    public Vector2 CurrentTotal;
    public Vector2 AbsoluteTotal;
    public List<Vector2> Past;
    public List<AIStat> Stats;

    public List<float> PastFitness;

    [Header("Stats")]
    public float RefreshInterval = 2f;
    public bool DebugOnRefresh;
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
            CurrentTotal = Vector2.zero;
    }
    public void LogGuess(int Motion, int Set)
    {
        //List<CurrentLearn> Guesses = LearnManager.instance.gameObject.GetComponent<UnitySharpNEAT.NeatSupervisor>()._spawnParent.GetChild(0).GetComponent<UnitySharpNEAT.LearningAgent>().Guesses;
        //List<CurrentLearn> Truths = LearnManager.instance.GetAllMotions(Motion, Set);
        UnitySharpNEAT.LearningAgent agent = LearnManager.instance.gameObject.GetComponent<UnitySharpNEAT.NeatSupervisor>()._spawnParent.GetChild(0).GetComponent<UnitySharpNEAT.LearningAgent>();
        CurrentLearn Guess = agent.CurrentGuess;

        CurrentLearn Truth = LearnManager.instance.MovementList[Motion].Motions[Set].AtFrameState(agent.Frame) ? (CurrentLearn)Motion: CurrentLearn.Nothing;
        int PlayCount = LearnManager.instance.MovementList[Motion].Motions[Set].PlayCount;
        
        Current = (Guess == Truth) ? new Vector2(Current.x + 1, Current.y) : new Vector2(Current.x, Current.y + 1);
        CurrentTotal = (Guess == Truth) ? new Vector2(CurrentTotal.x + 1, CurrentTotal.y) : new Vector2(CurrentTotal.x, CurrentTotal.y + 1);
        AbsoluteTotal = (Guess == Truth) ? new Vector2(AbsoluteTotal.x + 1, AbsoluteTotal.y) : new Vector2(AbsoluteTotal.x, AbsoluteTotal.y + 1);
        //stat.
        Stats.Add(new AIStat(Motion, Set, PlayCount, Guess, Truth));
    }


    
    void Start()
    {
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
    //public List<CurrentLearn> Guesses, Truths;
    public int MotionPlayNum;
    public CurrentLearn Guess, Truth;
    public AIStat(int MotionStat, int SetStat, int MotionPlayNumStat, CurrentLearn GuessStat, CurrentLearn TruthStat)
    {
        //Guesses = new List<CurrentLearn>(GuessesStat);
        //Truths = new List<CurrentLearn>(TruthStat);
        Motion = MotionStat;
        Set = SetStat;
        MotionPlayNum = MotionPlayNumStat;
        Guess = GuessStat;
        Truth = TruthStat;
    }
}


