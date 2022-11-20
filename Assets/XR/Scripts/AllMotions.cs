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
