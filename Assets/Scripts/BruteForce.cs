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

    //[FoldoutGroup("BruteForce")] public int CheckPerFrame;
    
    [FoldoutGroup("BruteForce")] public AllChanges AllChange;
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


    //[FoldoutGroup("Check"), ShowIf("ShouldDebug")] public List<long> CorrectOnTrue, CorrectOnFalse, InCorrectOnTrue, InCorrectOnFalse;
    [FoldoutGroup("Check")] public long Input;
    [FoldoutGroup("Check")] public List<long> Output = new List<long>();
    [FoldoutGroup("Check")] public List<long> MiddleStepCounts = new List<long>();
    [FoldoutGroup("Check")] public long ReInput;
    //, FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Low, OptimizeFor = OptimizeFor.FastCompilation
    [BurstCompile(CompileSynchronously = true)]
    private struct SingleBruteForce : IJobParallelFor
    {
        private struct SingleInfo
        {
            public float Max, Min;
            public int CurrentStep, MiddleSteps;
            public float GetCurrentValue() { return Mathf.Lerp(Min, Max, ((float)CurrentStep) / ((float)MiddleSteps + 1f)); }
            public SingleInfo(float Max, float Min, int CurrentStep, int MiddleSteps) { this.Max = Max; this.Min = Min; this.CurrentStep = CurrentStep; this.MiddleSteps = MiddleSteps; }
        }

        [DeallocateOnJobCompletion, Unity.Collections.ReadOnly] public NativeArray<float> NativeSingles;
        [DeallocateOnJobCompletion, Unity.Collections.ReadOnly] public NativeArray<long> WeightedMiddleSteps;

        [NativeDisableParallelForRestriction]public NativeArray<float> AllValues;

        public long StartAt;
        public int TestValue1;
        public int TestValue2;

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
    [FoldoutGroup("BruteForce"), Button(ButtonSizes.Small)]
    public void StartBruteForceRun()
    {
        float StartTime = Time.realtimeSinceStartup;
        BruteForceSettings = new MotionRestriction(RestrictionManager.instance.RestrictionSettings.MotionRestrictions[(int)motionGet - 1]); //restrictions
        FrameInfo = GetRestrictionsForMotions(motionGet, BruteForceSettings); //correct

        int Runs = Mathf.FloorToInt(MaxFrames / MaxGroup);
        int Remainder = (int)(MaxFrames - ((long)Runs * (long)MaxGroup));

        float RealHighest = 0;
        long RealIndex = 0;
        //ClearValues();
        for (int i = 0; i < Runs + 1; i++) //runner 
        {
            int RunCount = i != Runs ? MaxGroup : Remainder;
            long StartAt = i * MaxGroup;
            //Debug.Log("i: " + i + "  RunCount: " + RunCount + "  StartAt: " + StartAt);
            
            RunBruteForce(RunCount, StartAt, out float Highest, out int Index);
            //Debug.Log("RealHighest: " + RealHighest + "  TryHighest: " + Highest);
            if (Highest > RealHighest)
            {
                RealHighest = Highest;
                RealIndex = Index + StartAt;
            }
        }
        
        List<long> FinalStats = GetOutputList(RealIndex, GetMiddleStats(AllChange.GetSingles()));
        Values = new List<long>(FinalStats);
        

        List<AllChanges.SingleChange> Changes = AllChange.GetSingles();

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
        Debug.Log("BestIndex: " + RealIndex + "  BestValue: " + RealHighest);
        Debug.Log("Frames: " + MaxFrames + " in: " + (Time.realtimeSinceStartup - StartTime).ToString("F3") + " Seconds");

        void RunBruteForce(int RunCount, long StartAt, out float Highest, out int Index) //executor
        {
            NativeArray<bool> StatesStat = new NativeArray<bool>(FrameInfo.Count, Allocator.TempJob);
            NativeArray<float> FlatRawStat = new NativeArray<float>(FrameInfo.Count * FrameInfo[0].OutputRestrictions.Count, Allocator.TempJob);
            for (int i = 0; i < FrameInfo.Count; i++)
            {
                StatesStat[i] = FrameInfo[i].AtMotionState;
                for (int j = 0; j < FrameInfo[0].OutputRestrictions.Count; j++)
                    FlatRawStat[(i * FrameInfo[0].OutputRestrictions.Count) + j] = FrameInfo[i].OutputRestrictions[j];
            }

            List<AllChanges.SingleChange> Singles = AllChange.GetSingles();
            NativeArray<float> AllChangeStatsInput = new NativeArray<float>(Singles.Count * 3, Allocator.TempJob);//all change sttats
            for (int i = 0; i < Singles.Count; i++)
            {
                AllChangeStatsInput[(i * 3)] = Singles[i].Max;
                AllChangeStatsInput[(i * 3) + 1] = Singles[i].Min;
                AllChangeStatsInput[(i * 3) + 2] = Singles[i].GetTotalSteps();
            }

            List<long> MiddleStepCounts = GetMiddleStats(AllChange.GetSingles());
            NativeArray<long> MiddleValueList = new NativeArray<long>(MiddleStepCounts.Count, Allocator.TempJob);
            for (int i = 0; i < MiddleStepCounts.Count; i++)
                MiddleValueList[i] = MiddleStepCounts[i];

            SingleBruteForce BruteForceRun = new SingleBruteForce
            {
                NativeSingles = AllChangeStatsInput,
                States = StatesStat,
                StartAt = StartAt,
                FlatRawValues = FlatRawStat,
                AllValues = new NativeArray<float>(RunCount, Allocator.TempJob),
                WeightedMiddleSteps = MiddleValueList,
            };

            JobHandle jobHandle = BruteForceRun.Schedule(RunCount, BatchSize);
            jobHandle.Complete();
            //Debug.Log("Average: " + (Value / CorrectOnTrue.Count));
            GetHighest(BruteForceRun.AllValues, out Highest, out Index);

            BruteForceRun.AllValues.Dispose();
        }
        void GetHighest(NativeArray<float> Values, out float Highest, out int Index)
        {
            Highest = 0;
            Index = 0;
            for (int i = 0; i < Values.Length; i++)
            {
                if (Values[i] > Highest)
                {
                    Highest = Values[i];
                    Index = i;
                }
            }
        }
    }










    private void Update()
    {
        MiddleStepCounts = GetMiddleStats(AllChange.GetSingles());
        Output = GetOutputList(Input, MiddleStepCounts);
        ReInput = GetIndexFromList(MiddleStepCounts, Output);
    }
    public int TotalFramesToCheck()
    {
        List<AllChanges.SingleChange> SinglesList = AllChange.GetSingles();
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
        List<int> ToCheck = new List<int>() { 0, (int)FrameDataMotion };
        for (int i = 0; i < ToCheck.Count; i++)//motions
            for (int j = 0; j < LearnManager.instance.MovementList[ToCheck[i]].Motions.Count; j++)//set
                for (int k = PastFrameLookup; k < LearnManager.instance.MovementList[ToCheck[i]].Motions[j].Infos.Count; k++)//frame
                {
                    List<float> OutputRestrictions = new List<float>();
                    for (int l = 0; l < RestrictionsMotion.Restrictions.Count; l++)
                        OutputRestrictions.Add(RestrictionManager.RestrictionDictionary[RestrictionsMotion.Restrictions[l].restriction].Invoke(RestrictionsMotion.Restrictions[l], LearnManager.instance.MovementList[ToCheck[i]].GetRestrictionInfoAtIndex(j, k - PastFrameLookup), LearnManager.instance.MovementList[ToCheck[i]].GetRestrictionInfoAtIndex(j, k)));
                    ReturnValue.Add(new SingleFrameRestrictionInfo(OutputRestrictions, LearnManager.instance.MovementList[ToCheck[i]].Motions[j].AtFrameState(k)));
                }
        return ReturnValue;
    }
}
[System.Serializable]
public struct AllChanges
{
    public SingleChange ParentWeightThreshold;
    public List<OneRestrictionChange> Restrictions;
    public AllChanges(AllChanges NewInfo)
    {
        ParentWeightThreshold = NewInfo.ParentWeightThreshold;
        List<OneRestrictionChange> GetInfo = new List<OneRestrictionChange>();
        for (int i = 0; i < NewInfo.Restrictions.Count; i++)
        {
            GetInfo.Add(NewInfo.Restrictions[i]);
        }
        Restrictions = GetInfo;
    }
    public int CurrentDone()
    {
        List<SingleChange> SinglesList = GetSingles();
        int CurrentMultiplier = SinglesList[0].MiddleSteps + 1;
        int CurrentCount = SinglesList[0].CurrentStep;
        for (int i = 1; i < SinglesList.Count; i++)
        {
            //Debug.Log("Count: " + CurrentCount + "  CurrentMultiplier: " + CurrentMultiplier);
            CurrentCount += SinglesList[i].CurrentStep * CurrentMultiplier;
            CurrentMultiplier = CurrentMultiplier * (SinglesList[i].MiddleSteps + 1);
        }
        return CurrentCount;
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
    public bool AllHaveDone()
    {
        List<SingleChange> SinglesList = GetSingles();
        for (int i = 0; i < SinglesList.Count; i++)
            if (SinglesList[i].GetTotalSteps() != SinglesList[i].CurrentStep)
                return false;
        return true;
    }
    public void NextStep()
    {
        List<SingleChange> Singles = GetSingles();
        for (int i = 0; i < Singles.Count; i++)
        {
            Singles[i].NextStep(out bool HitMax);
            if (!HitMax)
                return;
        }
        //reset
        for (int i = 0; i < Singles.Count; i++)
            Singles[i].ResetStep();
        //OnStop();
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
        //public float GetCurrentValueAt(int ) { return Mathf.Lerp(Min, Max, ((float)CurrentStep) / (MiddleSteps + 1f)); }
        public void SetCurrentValue(int NewValue) { CurrentStep = NewValue; }
        public int GetTotalSteps() { return Max == Min ? 1 : MiddleSteps + 2; }
        public float GetCurrentValue() { return Mathf.Lerp(Min, Max, ((float)CurrentStep) / (MiddleSteps + 1f)); }
        public float GetCurrentValueAt(int NewCurrentStep) { return Mathf.Lerp(Min, Max, ((float)NewCurrentStep) / (MiddleSteps + 1f)); }
        public void NextStep(out bool Max)
        {
            CurrentStep += 1;
            Max = CurrentStep == MiddleSteps + 1;
            if (Max)
                ResetStep();
        }
        public void ResetStep() { CurrentStep = 0; }
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


