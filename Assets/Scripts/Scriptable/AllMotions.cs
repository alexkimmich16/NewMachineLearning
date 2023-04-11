using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor;
using RestrictionSystem;
using Sirenix.OdinInspector;
[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/AllData", order = 1)]
[System.Serializable]
public class AllMotions : ScriptableObject
{
    [ListDrawerSettings(Expanded = false, ShowIndexLabels = true)] public List<Motion> Motions;
    //public Vector2 Punishment = new Vector2(-1, -1);
    //public Vector2 Reward = new Vector2(1,1);

    //x is i guessed false
    public SingleInfo GetRestrictionInfoAtIndex(int Motion, int Frame) { return Motions[Motion].Infos[Frame]; }
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
public class Motion
{
    //[HideInInspector]
    public List<SingleInfo> Infos;
    public List<Vector2> TrueRanges;
    [HideInInspector] public int TrueIndex;
    [HideInInspector] public int PlayCount;
    public static List<Vector2> ConvertToRange(List<bool> Values)
    {
        List<Vector2> ranges = new List<Vector2>();
        bool Last = false;
        int Start = 0;
        for (int i = 0; i < Values.Count; i++)
        {
            if (Values[i] != Last)//onchange
            {
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
            else if (Last == true && i == Values.Count - 1)
            {
                ranges.Add(new Vector2(Start, i - 1));
            }
        }
        return ranges;
    }
    public void SetRanges(List<bool> Values)
    {
        TrueRanges = ConvertToRange(Values);
    }
    public void SetRanges(List<Vector2> Ranges)
    {
        TrueRanges = Ranges;
    }
    public bool AtFrameState(int Frame)
    {
        for (int i = 0; i < TrueRanges.Count; i++)
            if (Frame >= TrueRanges[i].x && Frame <= TrueRanges[i].y)
                return true;
        return false;
    }
}
