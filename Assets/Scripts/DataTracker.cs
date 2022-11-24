using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class DataTracker : MonoBehaviour
{

    [Header("RightVSWrong")]
    public RightWrongStats RightStats;
    private Vector2 Current;

    public InfoType type;
    public int CapMaxInterval = 40;
    public int EndTotal;


    [Header("LastFailures")]
    public LastFailureStats FailStats;
    public int FramesBefore;
    public int Finished;
    public int TotalAI;
    private bool Concluded = false;
    //public 
    /*
    [Header("Other")]
    private LearningAgent LA;

    

    public bool ReachedEndTotal()
    {
        return (RightStats.RightWrong.Count * CapMaxInterval) + Current.x + Current.y > EndTotal;
    }
    void Start()
    {
        //LA = LearningAgent.instance;
        
        LearningAgent[] components = GameObject.FindObjectsOfType<LearningAgent>();
        TotalAI = components.Length;
        for (int i = 0; i < components.Length; i++)
        {
            components[i].MoveToNextEvent += OnNextFrame;
            components[i].FinalFrame += FinalFrame;
        }  
        
    }
    public void FinishTest()
    {
        Concluded = true;
        SpreadSheet.instance.PrintRightWrong(RightStats);
        SpreadSheet.instance.PrintFalseFrames(FailStats);
    }
    public void FinalFrame()
    {
        Finished += 1;
        if (Finished == TotalAI)
            FinishTest();
        Debug.Log("finished");
    }
    public void OnNextFrame(bool State, int Cycle, int Set)
    {
        int AsInt = System.Convert.ToInt32(State);
        if(State == true)
        {
            Current.x += 1;
        }
        else if (State == false)
        {
            Current.y += 1;
            if (Cycle > LA.DesiredCycles - FramesBefore)
                FailStats.UpdateNum(Set);
        }

        if (Concluded == false && ReachedEndTotal())
            FinishTest();

        //graph stats
        if (type == InfoType.RightWrongBool)
        {
            //if (LA.CurrentFrame().Works != LA.LastState())
                //ResetAndLog();
        }
        else if (type == InfoType.CapRatio)
        {
            int BothTotal = (int)Current.x + (int)Current.y;
            
            if (BothTotal > CapMaxInterval)
                ResetAndLog();
        }
        void ResetAndLog()
        {
            if (Current != Vector2.zero)
                RightStats.RightWrong.Add(new Vector3(Current.x, Current.y, AsInt));
            Current = Vector2.zero;
        }
    }
    */
}
[System.Serializable]
public class RightWrongStats
{
    public List<Vector3> RightWrong;
}

[System.Serializable]
public class LastFailureStats
{
    //index, amount
    public List<Vector3> LastSetFailures;
    public void UpdateNum(int index)
    {
        for (int i = 0; i < LastSetFailures.Count; i++)
        {
            if (LastSetFailures[i].x == index)
            {
                LastSetFailures[i] = new Vector3(LastSetFailures[i].x, LastSetFailures[i].y + 1, 0);
                return;
            }
        }
        LastSetFailures.Add(new Vector3(index, 1,0));
    }
}


