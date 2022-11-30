using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class DataTracker : MonoBehaviour
{

    [Header("RightVSWrong")]
    public Vector2 Current;
    public Vector2 Total;

    //[Header("RightVSWrong")]


    
    //public LastFailureStats FailStats;
    //public 

    public delegate void Guess(bool Correct);
    public static event Guess OnGuess;
    public static void CallGuess(bool Guess) { OnGuess(Guess); }


    [Header("Stats")]
    public float RefreshInterval = 2f;
    public bool DebugOnRefresh;
    void Start()
    {
        OnGuess += AddToGuesses;
        StartCoroutine(RefreshWait());
    }
    public void AddToGuesses(bool Guess)
    {
        Current = (Guess) ? new Vector2(Current.x + 1, Current.y) : new Vector2(Current.x, Current.y + 1);
        Total = (Guess) ? new Vector2(Total.x + 1, Total.y) : new Vector2(Total.x, Total.y + 1);
    }
    IEnumerator RefreshWait()
    {
        while (true)
        {
            if (DebugOnRefresh)
                Debug.Log("Right: %" + (Current.x / (Current.x + Current.y) ) * 100);
            Current = Vector2.zero;
            yield return new WaitForSeconds(RefreshInterval);
        }
    }
    /*
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


