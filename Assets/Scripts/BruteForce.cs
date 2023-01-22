using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using RestrictionSystem;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Collections;
public class BruteForce : SerializedMonoBehaviour
{
    public static BruteForce instance;
    private void Awake() { instance = this; }

    public CurrentLearn motionGet;
    public int PastFrameLookup;

    [ListDrawerSettings(ShowIndexLabels = true, ListElementLabelName = "Motion"), FoldoutGroup("BruteForce")] public List<AllChanges> AllChangesList;
    [FoldoutGroup("BruteForce")] public MotionRestriction BruteForceSettings;
    [FoldoutGroup("BruteForce")] public List<SingleFrameRestrictionInfo> FrameInfo;

    [FoldoutGroup("BruteForce")] public long MaxFrames;
    [FoldoutGroup("BruteForce")] public int BatchSize;
    [FoldoutGroup("BruteForce")] public int MaxGroup;
    [FoldoutGroup("BruteForce")] public bool ShouldDebug;
    [FoldoutGroup("BruteForce"), ShowIf("ShouldDebug")] public int FramesToCaptureDebug;
    [FoldoutGroup("BruteForce")] public int Test1;
    [FoldoutGroup("BruteForce")] public int Test2;
    [FoldoutGroup("BruteForce"), Sirenix.OdinInspector.ReadOnly] public int TotalFrameCount;
    [FoldoutGroup("BruteForce"), Button(ButtonSizes.Small)]
    public void SetHighestNum() { TotalFrameCount = TotalFramesToCheck(); MaxFrames = TotalFramesToCheck(); }

    [FoldoutGroup("BruteForce")] public List<long> Values;
    [FoldoutGroup("BruteForce")] public List<long> FloatValues;
    [FoldoutGroup("BruteForce")] public bool UseAllMotions;

    [FoldoutGroup("Check")] public long Input;
    [FoldoutGroup("Check")] public List<long> Output = new List<long>();
    [FoldoutGroup("Check")] public List<long> MiddleStepCounts = new List<long>();
    [FoldoutGroup("Check")] public long ReInput;

    [FoldoutGroup("CustomCheck")] public bool LockValues;
    [FoldoutGroup("CustomCheck")] public int Sequences;
    [FoldoutGroup("CustomCheck"), Range(0,1)] public float Confidence;
    [FoldoutGroup("CustomCheck")] public float StopAdjustingPrecision = 0.005f; //range at which stops

    

    //, FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Low, OptimizeFor = OptimizeFor.FastCompilation
    [BurstCompile(CompileSynchronously = true)]
    private struct SingleBruteForce : IJobParallelFor
    {
        private struct SingleInfo
        {
            public float Max, Min;
            public int CurrentStep, MiddleSteps;
            public float GetCurrentValue() { return Mathf.Lerp(Min, Max, ((float)CurrentStep + 1f) / ((float)MiddleSteps + 1f)); }
            public SingleInfo(float Max, float Min, int CurrentStep, int MiddleSteps) { this.Max = Max; this.Min = Min; this.CurrentStep = CurrentStep; this.MiddleSteps = MiddleSteps; }
        }

        [DeallocateOnJobCompletion, Unity.Collections.ReadOnly] public NativeArray<float> NativeSingles;
        [DeallocateOnJobCompletion, Unity.Collections.ReadOnly] public NativeArray<long> WeightedMiddleSteps;

        [NativeDisableParallelForRestriction]public NativeArray<float> AllValues;

        public long StartAt;

        //[NativeDisableParallelForRestriction] public NativeArray<long> CorrectOnTrue, CorrectOnFalse, InCorrectOnTrue, InCorrectOnFalse;

        [Unity.Collections.ReadOnly] public NativeArray<bool> States;
        [Unity.Collections.ReadOnly] public NativeArray<float> FlatRawValues;

        public void Execute(int Index)
        {
            NativeArray<SingleInfo> ConvertedSingles = new NativeArray<SingleInfo>(NativeSingles.Length / 3, Allocator.Temp);  
            
            long LeftCount = Index + StartAt;


            for (int i = 0; i < WeightedMiddleSteps.Length; i++)
            {
                ConvertedSingles[i] = new SingleInfo(NativeSingles[(i * 3)], NativeSingles[(i * 3) + 1], Mathf.FloorToInt(LeftCount / WeightedMiddleSteps[i]), (int)NativeSingles[(i * 3) + 2]);     
                LeftCount -= (Mathf.FloorToInt(LeftCount / WeightedMiddleSteps[i]) * WeightedMiddleSteps[i]);
            }

            for (int i = 0; i < FlatRawValues.Length / States.Length; i++)
                if (ConvertedSingles[(i * 3) + 1].GetCurrentValue() < ConvertedSingles[(i * 3) + 2].GetCurrentValue()) // check if max is smaller than min to save processing power
                    return;

            for (int i = 0; i < ConvertedSingles.Length; i++) //stop repeats for already found variables
                if (ConvertedSingles[i].Max == ConvertedSingles[i].Min && ConvertedSingles[i].CurrentStep != ConvertedSingles[i].MiddleSteps - 1)//if already found and not top
                    return;

            int Correct = 0;
            int InCorrect = 0;
            int TotalRestrictions = FlatRawValues.Length / States.Length;
            for (int i = 0; i < FlatRawValues.Length / TotalRestrictions; i++) // all raw value input sets
            {
                float TotalWeightValue = 0f;
                float TotalWeight = 0f;
                for (int j = 0; j < TotalRestrictions; j++)// all restrictions
                {
                    float Max = ConvertedSingles[(j * 3) + 1].GetCurrentValue(); //1
                    float Min = ConvertedSingles[(j * 3) + 2].GetCurrentValue();
                    float Weight = ConvertedSingles[(j * 3) + 3].GetCurrentValue();
                    

                    TotalWeightValue += FlatRawValues[(i * TotalRestrictions) + j] < Max && FlatRawValues[(i * TotalRestrictions) + j] > Min ? Weight : 0;
                    //Debug.Log("Value: " + Values[i].Values[j] + "  Max: " + Max + "  Min: " + Min + "  j: " + j);
                    TotalWeight += Weight;
                }
                 
                float MinWeightThreshold = ConvertedSingles[0].GetCurrentValue() * TotalWeight;
                bool Guess = TotalWeightValue >= MinWeightThreshold;
                bool IsCorrect = Guess == States[i];

                Correct += IsCorrect ? 1 : 0;
                InCorrect += !IsCorrect ? 1 : 0;
            }
            float PercentGuess = ((float)Correct / ((float)InCorrect + (float)Correct)) * 100f;
            AllValues[Index] = PercentGuess;

            ConvertedSingles.Dispose();
        }
    }


    void RunBruteForce(int RunCount, long StartAt, out float Highest, out int Index) //executor
    {
        NativeArray<bool> GetStatesStat()
        {
            NativeArray<bool> StatesStat = new NativeArray<bool>(FrameInfo.Count, Allocator.TempJob);
            for (int i = 0; i < FrameInfo.Count; i++)
                StatesStat[i] = FrameInfo[i].AtMotionState;
            return StatesStat;
        }
        NativeArray<float> GetFlatRawStat()
        {
            NativeArray<float> FlatRawStat = new NativeArray<float>(FrameInfo.Count * FrameInfo[0].OutputRestrictions.Count, Allocator.TempJob);
            for (int i = 0; i < FrameInfo.Count; i++)
                for (int j = 0; j < FrameInfo[0].OutputRestrictions.Count; j++)
                    FlatRawStat[(i * FrameInfo[0].OutputRestrictions.Count) + j] = FrameInfo[i].OutputRestrictions[j];
            return FlatRawStat;
        }
        NativeArray<float> GetAllChangeStatsInput()
        {
            List<AllChanges.SingleChange> Singles = AllChangesList[(int)motionGet - 1].GetSingles();
            NativeArray<float> AllChangeStatsInput = new NativeArray<float>(Singles.Count * 3, Allocator.TempJob);//all change sttats
            for (int i = 0; i < Singles.Count; i++)
            {
                AllChangeStatsInput[(i * 3)] = Singles[i].Max;
                AllChangeStatsInput[(i * 3) + 1] = Singles[i].Min;
                AllChangeStatsInput[(i * 3) + 2] = Singles[i].GetTotalSteps();

            }
            return AllChangeStatsInput;
        }
        NativeArray<long> GetMiddleValueList()
        {
            List<long> MiddleStepCounts = GetMiddleStats(AllChangesList[(int)motionGet - 1].GetSingles());
            NativeArray<long> MiddleValueList = new NativeArray<long>(MiddleStepCounts.Count, Allocator.TempJob);
            for (int i = 0; i < MiddleStepCounts.Count; i++)
                MiddleValueList[i] = MiddleStepCounts[i];
            return MiddleValueList;
        }

        SingleBruteForce BruteForceRun = new SingleBruteForce
        {
            NativeSingles = GetAllChangeStatsInput(),
            States = GetStatesStat(),
            StartAt = StartAt,
            FlatRawValues = GetFlatRawStat(),
            AllValues = new NativeArray<float>(RunCount, Allocator.TempJob),
            WeightedMiddleSteps = GetMiddleValueList(),
        };

        JobHandle jobHandle = BruteForceRun.Schedule(RunCount, BatchSize);
        jobHandle.Complete();
        //Debug.Log("Average: " + (Value / CorrectOnTrue.Count));

        Highest = 0;
        Index = 0;
        for (int i = 0; i < BruteForceRun.AllValues.Length; i++)
        {
            if (BruteForceRun.AllValues[i] > Highest)
            {
                Highest = BruteForceRun.AllValues[i];
                Index = i;
            }
        }

        BruteForceRun.AllValues.Dispose();
    }
    [FoldoutGroup("BruteForce"), Button(ButtonSizes.Small)]
    public void RunBruteForce()
    {
        int Locks = LockValues ? Sequences : 1;
        for (int v = 0; v < Locks; v++)
        {
            float StartTime = Time.realtimeSinceStartup;
            BruteForceSettings = new MotionRestriction(RestrictionManager.instance.RestrictionSettings.MotionRestrictions[(int)motionGet - 1]); //restrictions
            if(FrameInfo.Count == 0)
                FrameInfo = GetRestrictionsForMotions(motionGet, BruteForceSettings); //correct

            int Runs = Mathf.FloorToInt(MaxFrames / MaxGroup);
            int Remainder = (int)(MaxFrames - ((long)Runs * (long)MaxGroup));

            float RealHighest = 0;
            long RealIndex = 0;
            for (int i = 0; i < Runs + 1; i++) //runner 
            {
                int RunCount = i != Runs ? MaxGroup : Remainder;
                long StartAt = i * MaxGroup;

                RunBruteForce(RunCount, StartAt, out float Highest, out int Index);
                if (Highest > RealHighest)
                {
                    RealHighest = Highest;
                    RealIndex = Index + StartAt;
                }
            }

            //output
            //FloatValues
            List<long> FinalStats = GetOutputList(RealIndex, GetMiddleStats(AllChangesList[(int)motionGet - 1].GetSingles()));
            Values = new List<long>(FinalStats);

            
            List<AllChanges.SingleChange> Changes = AllChangesList[(int)motionGet - 1].GetSingles();
            BruteForceSettings.WeightedValueThreshold = Changes[0].GetCurrentValueAt((int)FinalStats[0]);

            List<SingleRestriction> NewList = new List<SingleRestriction>();
            for (int i = 0; i < BruteForceSettings.Restrictions.Count; i++)
            {
                SingleRestriction NewRestriction = BruteForceSettings.Restrictions[i];
                NewRestriction.MaxSafe = Changes[(i * 3) + 1].GetCurrentValueAt((int)FinalStats[(i * 3) + 1]);
                NewRestriction.MinSafe = Changes[(i * 3) + 2].GetCurrentValueAt((int)FinalStats[(i * 3) + 2]);
                NewRestriction.Weight = Changes[(i * 3) + 3].GetCurrentValueAt((int)FinalStats[(i * 3) + 3]);
                NewList.Add(NewRestriction);
            }


            BruteForceSettings.Restrictions = NewList;
            if (LockValues)
            {
                List<AllChanges.SingleChange> NewChanges = new List<AllChanges.SingleChange>();
                for (int i = 0; i < Changes.Count; i++)
                {
                    
                    float Range = ((Changes[i].Max - Changes[i].Min) / 2) * Confidence;

                    float NewMax = Range > StopAdjustingPrecision ? Changes[i].GetCurrentValueAt((int)FinalStats[i]) + Range : Changes[i].GetCurrentValueAt((int)FinalStats[i]);
                    float NewMin = Range > StopAdjustingPrecision ? Changes[i].GetCurrentValueAt((int)FinalStats[i]) - Range : Changes[i].GetCurrentValueAt((int)FinalStats[i]);


                    int Restriction = Mathf.FloorToInt((i - 1) / 3);
                    int CountLeft = (i - 1) - Restriction * 3;

                    //Debug.Log("Restriction: " + Restriction + "CountLeft: " + CountLeft +  "  i: " + i);

                    if(i == 0)
                    {
                        NewChanges.Add(new AllChanges.SingleChange(NewMax, NewMin, AllChangesList[(int)motionGet - 1].ParentWeightThreshold.MiddleSteps, AllChangesList[(int)motionGet - 1].ParentWeightThreshold.CurrentStep));
                    }
                    else
                    {
                        AllChanges.OneRestrictionChange Rest = AllChangesList[(int)motionGet - 1].Restrictions[Restriction];
                        
                        if (CountLeft == 0)
                            NewChanges.Add(new AllChanges.SingleChange(NewMax, NewMin, Rest.Max.MiddleSteps, Rest.Max.CurrentStep));
                        if (CountLeft == 1)
                            NewChanges.Add(new AllChanges.SingleChange(NewMax, NewMin, Rest.Min.MiddleSteps, Rest.Min.CurrentStep));
                        if (CountLeft == 2)
                            NewChanges.Add(new AllChanges.SingleChange(NewMax, NewMin, Rest.Weight.MiddleSteps, Rest.Weight.CurrentStep));
                    }
                    
                }
                Debug.Log("NewChanges: " + NewChanges.Count);
                AllChangesList[(int)motionGet - 1] = new AllChanges(NewChanges);
            }
            Debug.Log("BestIndex: " + RealIndex + "  BestValue: " + RealHighest);
            Debug.Log("Frames: " + MaxFrames + " in: " + (Time.realtimeSinceStartup - StartTime).ToString("F3") + " Seconds");
        }
    }

    private void Update()
    {
        MiddleStepCounts = GetMiddleStats(AllChangesList[(int)motionGet - 1].GetSingles());
        Output = GetOutputList(Input, MiddleStepCounts);
        ReInput = GetIndexFromList(MiddleStepCounts, Output);
    }
    public int TotalFramesToCheck()
    {
        List<AllChanges.SingleChange> SinglesList = AllChangesList[(int)motionGet - 1].GetSingles();
        int Total = SinglesList[0].GetTotalSteps();
        for (int i = 1; i < SinglesList.Count; i++)
            Total = Total * SinglesList[i].GetTotalSteps();
        return Total;
    }
    private long GetIndexFromList(List<long> MaxStepCounts, List<long> Output)
    {
        int ReInput = 0;
        for (int i = 0; i < MaxStepCounts.Count; i++)
            ReInput += (int)(Output[i] * MaxStepCounts[i]);
        return ReInput;
    }
    private List<long> GetMiddleStats(List<AllChanges.SingleChange> SinglesList)
    {
        List<long> Output = new List<long>();
        for (int i = 0; i < SinglesList.Count; i++)
            Output.Add(1);

        for (int i = 0; i < SinglesList.Count; i++)
            for (int j = 0; j < i; j++)
                Output[j] = (long)(Output[j] * (SinglesList[i].GetTotalSteps()));
        return Output;
    }
    private List<long> GetOutputList(long Total, List<long> MiddleStepCounts)
    {
        List<long> Output = new List<long>();
        long LeftCount = Total;
        for (int i = 0; i < MiddleStepCounts.Count; i++)
        {
            Output.Add((long)Mathf.Floor(LeftCount / MiddleStepCounts[i]));
            LeftCount -= (long)Mathf.Floor(LeftCount / MiddleStepCounts[i]) * MiddleStepCounts[i];
        }
        return Output;
    }
    public List<SingleFrameRestrictionInfo> GetRestrictionsForMotions(CurrentLearn FrameDataMotion, MotionRestriction RestrictionsMotion)
    {
        List<SingleFrameRestrictionInfo> ReturnValue = new List<SingleFrameRestrictionInfo>();
        List<int> ToCheck = UseAllMotions ? new List<int>() { 0, 1, 2, 3 } : new List<int>(){ 0, (int)FrameDataMotion };
        for (int i = 0; i < ToCheck.Count; i++)//motions
            for (int j = 0; j < LearnManager.instance.MovementList[ToCheck[i]].Motions.Count; j++)//set
                for (int k = PastFrameLookup; k < LearnManager.instance.MovementList[ToCheck[i]].Motions[j].Infos.Count; k++)//frame
                {
                    List<float> OutputRestrictions = new List<float>();
                    for (int l = 0; l < RestrictionsMotion.Restrictions.Count; l++)
                        OutputRestrictions.Add(RestrictionManager.RestrictionDictionary[RestrictionsMotion.Restrictions[l].restriction].Invoke(RestrictionsMotion.Restrictions[l], LearnManager.instance.MovementList[ToCheck[i]].GetRestrictionInfoAtIndex(j, k - PastFrameLookup), LearnManager.instance.MovementList[ToCheck[i]].GetRestrictionInfoAtIndex(j, k)));
                    ReturnValue.Add(new SingleFrameRestrictionInfo(OutputRestrictions, ToCheck[i] == (int)FrameDataMotion && LearnManager.instance.MovementList[ToCheck[i]].Motions[j].AtFrameState(k)));
                }
        return ReturnValue;
    }
}
[System.Serializable]
public struct AllChanges
{
    public string Motion;
    public SingleChange ParentWeightThreshold;
    public List<OneRestrictionChange> Restrictions;
    public AllChanges(List<SingleChange> NewInfo)
    {
        Motion = "";
        ParentWeightThreshold = NewInfo[0];
        List<OneRestrictionChange> NewRestrictions = new List<OneRestrictionChange>();
        for (int i = 0; i < (NewInfo.Count / 3); i++)
        {
            Debug.Log("OneCall");
            OneRestrictionChange MainRestriction = new OneRestrictionChange();
            MainRestriction.Max = NewInfo[(i * 3) + 1];
            MainRestriction.Min = NewInfo[(i * 3) + 2];
            MainRestriction.Weight = NewInfo[(i * 3) + 3];
            NewRestrictions.Add(MainRestriction);
        }
        Restrictions = NewRestrictions;
    }
    public List<SingleChange> GetSingles()
    {
        List<SingleChange> SinglesList = new List<SingleChange>();
        SinglesList.Add(ParentWeightThreshold);
        for (int i = 0; i < Restrictions.Count; i++)
        {
            SinglesList.Add(Restrictions[i].Max);
            SinglesList.Add(Restrictions[i].Min);
            SinglesList.Add(Restrictions[i].Weight);
        }
        return SinglesList;
    }
    public List<float> GetEncodedInfo()
    {
        List<float> ReturnInfo = new List<float>();
        ReturnInfo.Add(ParentWeightThreshold.GetCurrentValue());
        for (int i = 0; i < Restrictions.Count; i++)
        {
            ReturnInfo.Add(Restrictions[i].Max.GetCurrentValue());
            ReturnInfo.Add(Restrictions[i].Min.GetCurrentValue());
            ReturnInfo.Add(Restrictions[i].Weight.GetCurrentValue());
        }
        return ReturnInfo;
    }

    [System.Serializable]
    public struct OneRestrictionChange
    {
        public SingleChange Max;
        public SingleChange Min;
        public SingleChange Weight;
    }
    [System.Serializable]
    public struct SingleChange
    {
        public float Max;
        public float Min;
        public int MiddleSteps;
        
        [Sirenix.OdinInspector.ReadOnly] public int CurrentStep;

        public SingleChange(float Max, float Min, int MiddleSteps, int CurrentStep)
        {
            this.Max = Max;
            this.Min = Min;
            this.MiddleSteps = MiddleSteps;
            this.CurrentStep = CurrentStep;
        }
        //public float GetCurrentValueAt(int ) { return Mathf.Lerp(Min, Max, ((float)CurrentStep) / (MiddleSteps + 1f)); }
        public void SetNewMaxMin(float Max, float Min)
        {
            Debug.Log(Max);
            this.Max = Max;
            this.Min = Min;
        }
        public int GetTotalSteps() { return Max == Min ? 1 : MiddleSteps; }
        public float GetCurrentValue() { return Mathf.Lerp(Min, Max, ((float)CurrentStep + 1f) / (MiddleSteps + 1f)); }
        public float GetCurrentValueAt(int NewCurrentStep) { return Mathf.Lerp(Min, Max, ((float)NewCurrentStep + 1f) / (MiddleSteps + 1f)); }
    }

}

[System.Serializable]
public struct SingleFrameRestrictionInfo
{
    [ListDrawerSettings(HideRemoveButton = false, Expanded = true)]
    public List<float> OutputRestrictions;
    public bool AtMotionState;
    public SingleFrameRestrictionInfo(List<float> OutputRestrictionsStat, bool AtMotionState)
    {
        OutputRestrictions = OutputRestrictionsStat;
        this.AtMotionState = AtMotionState;
    }
}
[System.Serializable]
public struct MotionRestrictionStruct
{
    [Range(0, 100)] public float Highest;

    public float ParentWeightThreshold;
    public List<OneRestrictionInfo> RestrictionInfos;
    public MotionRestrictionStruct(List<float> EncodedStatsList)
    {
        this.Highest = EncodedStatsList[0];
        ParentWeightThreshold = EncodedStatsList[1];
        //EncodedStatsList.RemoveAt(0);
        List<OneRestrictionInfo> GetInfo = new List<OneRestrictionInfo>();
        for (int i = 0; i < (EncodedStatsList.Count / 3) - 1; i++)
        {
            GetInfo.Add(new OneRestrictionInfo(EncodedStatsList[(i * 3) + 1], EncodedStatsList[(i * 3) + 2], EncodedStatsList[(i * 3) + 3]));
        }
        RestrictionInfos = GetInfo;
    }
    public MotionRestrictionStruct(MotionRestriction output)
    {
        Highest = 0;
        ParentWeightThreshold = output.WeightedValueThreshold;
        //EncodedStatsList.RemoveAt(0);
        List<OneRestrictionInfo> GetInfo = new List<OneRestrictionInfo>();
        for (int i = 0; i < output.Restrictions.Count; i++)
        {
            GetInfo.Add(new OneRestrictionInfo(output.Restrictions[i].MaxSafe, output.Restrictions[i].MinSafe, output.Restrictions[i].Weight));
        }
        RestrictionInfos = GetInfo;
    }
    public MotionRestrictionStruct(MotionRestrictionStruct output)
    {
        Highest = output.Highest;
        ParentWeightThreshold = output.ParentWeightThreshold;
        //EncodedStatsList.RemoveAt(0);
        List<OneRestrictionInfo> GetInfo = new List<OneRestrictionInfo>();
        for (int i = 0; i < output.RestrictionInfos.Count; i++)
        {
            GetInfo.Add(output.RestrictionInfos[i]);
        }
        RestrictionInfos = GetInfo;
    }

    [System.Serializable]
    public struct OneRestrictionInfo
    {
        public OneRestrictionInfo(float MaxThreshold, float MinThreshold, float Weight)
        {
            this.MaxThreshold = MaxThreshold;
            this.MinThreshold = MinThreshold;
            this.Weight = Weight;
        }

        public float MaxThreshold;
        public float MinThreshold;
        public float Weight;
    }
}


