using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;
public class PickManager : SerializedMonoBehaviour
{
    public static PickManager instance;
    private void Awake() { instance = this; }
    [FoldoutGroup("FrameData"), ReadOnly] public List<MotionFrames> EachMotionFrameCount;
    [FoldoutGroup("FrameData"), ReadOnly] public List<int> TotalFrameCount;
    [FoldoutGroup("FrameData"), ReadOnly] public List<float> PickChances;
    [FoldoutGroup("FrameData"), ReadOnly] public List<float> AdjustedPickChances;
    [FoldoutGroup("FrameData"), ReadOnly] public List<int> TotalPicks;
    [FoldoutGroup("FrameData"), ReadOnly] public List<float> AverageFramesInMotions;

    public bool ShouldDebug;

    [Range(1f,5f)]public int PastFrameReduce;
    public List<float> GetOutOf100(List<float> Input)
    {
        List<float> Adjusted = new List<float>();
        float Total = 0f;
        for (int i = 0; i < Input.Count; ++i)
            Total += Input[i];
        float Multiplier = 100f / Total;
        for (int i = 0; i < LearnManager.instance.MovementList.Count; ++i)
            Adjusted.Add(Multiplier * Input[i]);
        return Adjusted;
    }
    public void UpdateAdjustedPickChances()
    {
        int TotalPickCount = 0;
        
        int Lowest = 100;
        List<int> AdjustedTotalPickCounts = new List<int>();
        for (int i = 0; i < DataTracker.instance.SpellCalls.Count; ++i)
            if (DataTracker.instance.SpellCalls[i] < Lowest)
                Lowest = DataTracker.instance.SpellCalls[i]; 
        
        for (int i = 0; i < PickChances.Count; ++i)
            TotalPickCount += DataTracker.instance.SpellCalls[i] - Lowest + PastFrameReduce;
        //Debug.Log(TotalPickCount);

        for (int i = 0; i < PickChances.Count; ++i)
        {
            float SpacePercent = (DataTracker.instance.SpellCalls[i] - Lowest + PastFrameReduce) / (float)TotalPickCount;
            float Inverse = 1f / SpacePercent;
            //Debug.Log("SpacePercent: " + SpacePercent + "  Inverse: " + Inverse);
            AdjustedPickChances[i] = Inverse * PickChances[i];
            //AdjustedPickChances[i] = Inverse;
        }
        AdjustedPickChances = GetOutOf100(AdjustedPickChances);
    }
    // Start is called before the first frame update
    void Start()
    {
        EachMotionFrameCount = GetEachMotionFrameCount();

        for (int i = 0; i < LearnManager.instance.MovementList.Count; ++i)
            TotalPicks.Add(1);

        PickChances = (LearnManager.instance.AutoPickChance) ? GetPickChances() : LearnManager.instance.OverridePickChances;
        AdjustedPickChances = new List<float>(PickChances);
        
        if (ShouldDebug)
            Debug.Log("IdealCapacity: " + 2 * MatrixManager.instance.Height * MatrixManager.instance.Width);

        //UpdateAdjustedPickChances();
    }

    public List<MotionFrames> GetEachMotionFrameCount()
    {
        List<MotionFrames> FrameCountList = new List<MotionFrames>();
        for (int i = 0; i < LearnManager.instance.MovementList.Count; ++i)
            FrameCountList.Add(new MotionFrames(LearnManager.instance.MovementList.Count));
        for (int i = 0; i < LearnManager.instance.MovementList.Count; ++i)
        {
            for (int j = 0; j < LearnManager.instance.MovementList[i].Motions.Count; ++j)
            {
                for (int k = 0; k < LearnManager.instance.MovementList[i].Motions[j].Infos.Count; ++k)
                {
                    int ListAdd = (LearnManager.instance.AtFrameStateAlwaysTrue) ? i : (LearnManager.instance.MovementList[i].Motions[j].AtFrameState(k) ? i : 0);
                    FrameCountList[i].MotionCounts[ListAdd] += 1;
                }
            }
        }
        return FrameCountList;
    }
    public List<float> GetPickChances()
    {
        List<float> Chances = new List<float>();
        AverageFramesInMotions = new List<float>();
        //AtFrameStateAlwaysTrue
        float CombinedAverageFrames = 0;
        float TotalFramesInAllMotions = 0;
        //TotalFrameCount = GetTotalFrames();
        for (int i = 0; i < EachMotionFrameCount.Count; ++i)
        {
            Chances.Add(0f);
            TotalFramesInAllMotions += GetTotalMotionFrames(i);

            TotalFrameCount.Add(GetTotalMotionFrames(i));

            float AverageFramesInMotion = (float)GetTotalMotionFrames(i) / (float)LearnManager.instance.MovementList[i].Motions.Count;
            //Debug.Log(AverageFramesInMotion);
            AverageFramesInMotions.Add(AverageFramesInMotion);
            CombinedAverageFrames += AverageFramesInMotion;
        }


        if (LearnManager.instance.AtFrameStateAlwaysTrue)
        {
            for (int i = 0; i < EachMotionFrameCount.Count; ++i)
            {
                //Chances[i] = 1f/ (GetTotalMotionFrames(i) / TotalFramesInMotion);
                //Debug.Log(GetTotalMotionFrames(i));
                float FramesPerMotionAverage = GetTotalMotionFrames(i) / TotalFramesInAllMotions;
                float Inverse = 1f / AverageFramesInMotions[i];
                float Value = Inverse * LearnManager.instance.MovementList.Count;
                Chances[i] = (Value);
            }


            /*
            for (int i = 0; i < EachMotionFrameCount.Count; ++i)
            {
                //Chances[i] = 1f/ (GetTotalMotionFrames(i) / TotalFramesInMotion);
                float FramesPerMotionAverage = GetTotalMotionFrames(i) / MovementList[i].Motions.Count;
                float Inverse = 1f / FramesPerMotionAverage;
                float Value = Inverse;
                Chances[i] = (Value);
            }
            */
        }
        else
        {
            for (int i = 0; i < EachMotionFrameCount.Count; ++i)
                for (int j = 0; j < EachMotionFrameCount[i].MotionCounts.Count; ++j)
                {
                    //AverageFramesInMotions
                    
                    float ThisSubFrameCount = EachMotionFrameCount[i].MotionCounts[j]; // current frames in parent     //10 sets
                    float MyWeightOfTotal = ThisSubFrameCount / (float)GetTotalMotionFrames(i); //40 flame frames out of 100 = 40% are flame (0.4)


                    float MyCutOfAverageFrames = (MyWeightOfTotal) * AverageFramesInMotions[i];
                    float InverseOfAverageThisFrame = MyCutOfAverageFrames; //the opposite number of the average frames: 1 / 4 = 0.25
                    float ChanceAddForThisMotion = (ThisSubFrameCount != 0f) ? InverseOfAverageThisFrame : 0f; //if 0 frames do nothing

                    //float ThisFramesOfAverage = PercentOfAverageFramesPerMotion * AverageFramesInMotions[i]; //40% average frames per motion (10) = 4 average flame frames per motion
                    //float InverseOfAverageThisFrame = ThisFramesOfAverage; //the opposite number of the average frames: 1 / 4 = 0.25
                    
                    
                    Chances[j] += ChanceAddForThisMotion; //total average
                    if (ThisSubFrameCount != 0f && ShouldDebug)
                        Debug.Log("I: " + (CurrentLearn)i + "  J: " + (CurrentLearn)j +
                        "  ThisSubFrameCount: " + ThisSubFrameCount +
                        "  TotalFramesMotionParent: " + GetTotalMotionFrames(i) +
                        "  MyWeightOfTotal: " + (MyWeightOfTotal * 100f) + "%" +
                        "  AverageFramesPerMotion: " + AverageFramesInMotions[i] +
                        //"  ThisFramesOfAverage: " + ThisFramesOfAverage +
                        "  ChanceAddForThisMotion: " + ChanceAddForThisMotion
                        //"  Value: " + (EachMotionFrameCount[i].MotionCounts[j] / MovementList[i].Motions.Count)
                        );


                }
            float CurrentTotal = GetTotal(Chances);
            for (int i = 0; i < Chances.Count; ++i)
            {

                float PercentOfWhole = Chances[i] / CurrentTotal;
               // Debug.Log("i: " + i + "  PercentOfWhole: " + PercentOfWhole);
                //Chances[i] = 1f / Chances[i];
                Chances[i] = 1f / PercentOfWhole;

                //Chances[i] = 1f / Chances[i];
            }

            //Debug.Log("Total: " + Total);
            //for (int i = 0; i < Chances.Count; ++i)
            //Debug.Log("i: " + i + "  Weight2: " + Chances[i]);
        }
        /*
        float TotalCurrent = 0;
        for (int i = 0; i < Chances.Count; ++i)
        {
            TotalCurrent += Chances[i];
            Debug.Log("Chances: " + TotalCurrent); 
        }
        */

        if (LearnManager.instance.Multiplier)
            Chances = GetOutOf100(Chances);
        return Chances;
        int GetTotalMotionFrames(int Index)
        {
            int Total = 0;
            for (int j = 0; j < EachMotionFrameCount.Count; ++j)
                Total += EachMotionFrameCount[Index].MotionCounts[j];
            //Debug.Log(Total);
            return Total;
        }
        float GetTotal(List<float> Chances)
        {
            float Total = 0;
            for (int i = 0; i < Chances.Count; ++i)
                Total += Chances[i];
            return Total;
        }
    }
}
[System.Serializable]
public class MotionFrames
{
    public List<int> MotionCounts = new List<int>();
    public MotionFrames(int Count)
    {
        for (int i = 0; i < Count; ++i)
            MotionCounts.Add(0);
    }
}