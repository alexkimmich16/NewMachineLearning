using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/AllData", order = 1)]
public class AllMotions : ScriptableObject
{
    public List<Motion> Motions;
    public Vector2 Punishment = new Vector2(-1, -1);
    public Vector2 Reward = new Vector2(1,1);

    //x is i guessed false
    public float GetReward(bool GotRight, bool Correct)
    {
        Vector2 Consequence;
        if (GotRight == true)
            Consequence = Reward;
        else
            Consequence = Punishment;

        if (Correct == false)
            return Consequence.x;
        else
            return Consequence.y;
    }

    public List<Motion> Random()
    {
        List<Motion> NewMotions = new List<Motion>(Motions);
        for (int i = 0; i < NewMotions.Count; i++)
            Motions[i].TrueIndex = i;
        Debug.Log("try");
        Shuffle.ShuffleSet(NewMotions);
        return NewMotions;
        //return null;
    }
}
[System.Serializable]
public class SingleInfo
{
    public Vector3 HeadPos, HeadRot, HandPos, HandRot, HandVel, AdjustedHandPos;
    //public bool Works;
}
[System.Serializable]
public class Motion
{
    //[HideInInspector]
    public List<SingleInfo> Infos;
    public List<Vector2> TrueRanges;
    public int TrueIndex;

    public void IntoRange(List<bool> Values)
    {
        List<Vector2> ranges = new List<Vector2>();
        bool Last = false;
        int Start = 0;
        for (int i = 0; i < Values.Count; i++)
        {
            if (Values[i] != Last)
            {
                //onchange
                if (Last == false)
                {
                    Start = i;
                }
                else if (Last == true)
                {
                    //range is start to i
                    ranges.Add(new Vector2(Start, i - 1));
                }
                Last = Values[i];
            }
        }
        TrueRanges = ranges;
    }
    public bool AtFrameState(int Frame)
    {
        for (int i = 0; i < TrueRanges.Count; i++)
            if (Frame >= TrueRanges[i].x && Frame <= TrueRanges[i].y)
                return true;
        return false;
    }
}