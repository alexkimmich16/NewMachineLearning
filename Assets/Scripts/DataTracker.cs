using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class DataTracker : MonoBehaviour
{
    public static DataTracker instance;
    private void Awake() { instance = this; }
    [Header("RightVSWrong")]
    public Vector2 Current;
    public Vector2 Total;
    public List<Vector2> Past;
    public List<AIStat> Stats;

    public List<float> PastFitness;


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
            Total = Vector2.zero;
    }
    public void LogGuess(int Motion, int Set)
    {
        List<CurrentLearn> Guesses = LearnManager.instance.gameObject.GetComponent<UnitySharpNEAT.NeatSupervisor>()._spawnParent.GetChild(0).GetComponent<UnitySharpNEAT.LearningAgent>().Guesses;
        List<CurrentLearn> Truths = LearnManager.instance.GetAllMotions(Motion, Set);
        int PlayCount = LearnManager.instance.MovementList[Motion].Motions[Set].PlayCount;
        
        //Current = (guess == Truth) ? new Vector2(Current.x + 1, Current.y) : new Vector2(Current.x, Current.y + 1);
        //Total = (guess == Truth) ? new Vector2(Total.x + 1, Total.y) : new Vector2(Total.x, Total.y + 1);
        //stat.
        Stats.Add(new AIStat(Motion, Set, Guesses, Truths, PlayCount));
    }


    [Header("Stats")]
    public float RefreshInterval = 2f;
    public bool DebugOnRefresh;
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
    public List<CurrentLearn> Guesses, Truths;
    public int MotionPlayNum;
    public AIStat(int MotionStat, int SetStat, List<CurrentLearn> GuessesStat, List<CurrentLearn> TruthStat, int MotionPlayNumStat)
    {
        Guesses = new List<CurrentLearn>(GuessesStat);
        Truths = new List<CurrentLearn>(TruthStat);
        Motion = MotionStat;
        Set = SetStat;
        MotionPlayNum = MotionPlayNumStat;
    }
}


