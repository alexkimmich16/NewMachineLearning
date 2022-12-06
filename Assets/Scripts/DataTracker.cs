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

    private int NewGenAgents;

    private int AIStatIndex;
    private int AIStatCount;

    public int AIShouldWatch = 5;
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
            Total = Vector2.zero;
    }
    public void AgentNewGenCall()
    {
        NewGenAgents += 1;
        if (NewGenAgents == gameObject.GetComponent<UnitySharpNEAT.NeatSupervisor>()._spawnParent.childCount)
        {
            NewGenAgents = 0;
        }
    }
    public void CallGuess(CurrentLearn guess, CurrentLearn Truth, int Set)
    {
        Current = (guess == Truth) ? new Vector2(Current.x + 1, Current.y) : new Vector2(Current.x, Current.y + 1);
        Total = (guess == Truth) ? new Vector2(Total.x + 1, Total.y) : new Vector2(Total.x, Total.y + 1);
        AIStatCount += 1;
        if (AIStatCount == gameObject.GetComponent<UnitySharpNEAT.NeatSupervisor>()._spawnParent.childCount)
        {
            AIStatCount = 0;
            AIStatIndex += 1;
            return;
        }
            
        if (AIShouldWatch < AIStatCount)
            return;
        
        AIStat stat = new AIStat(guess, Truth, AIStatIndex, Set);
        //stat.
        Stats.Add(stat);
        AIStatIndex += 1;
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
    public CurrentLearn Guess, Truth;
    public bool Correct;
    public int Index, Set;
    public AIStat(CurrentLearn GuessStat, CurrentLearn TruthStat, int ListNum, int SetNum)
    {
        Guess = GuessStat;
        Truth = TruthStat;
        Correct = GuessStat == TruthStat;
        Index = ListNum;
        Set = SetNum;
    }
}


