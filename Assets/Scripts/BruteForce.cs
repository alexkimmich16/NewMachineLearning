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
    [FoldoutGroup("BruteForce")] public int FramesToCaptureDebug;
    [FoldoutGroup("BruteForce")] public int Test1;
    [FoldoutGroup("BruteForce")] public int Test2;
    [FoldoutGroup("BruteForce"), Sirenix.OdinInspector.ReadOnly] public int TotalFrameCount;
    [FoldoutGroup("BruteForce"), Button(ButtonSizes.Small)]
    public void SetHighestNum() { TotalFrameCount = TotalFramesToCheck(); MaxFrames = TotalFramesToCheck(); }

    [FoldoutGroup("BruteForce")] public List<long> Values;


    [FoldoutGroup("Check")] public List<long> CorrectOnTrue, CorrectOnFalse, InCorrectOnTrue, InCorrectOnFalse;
    [FoldoutGroup("Check")] public long Input;
    [FoldoutGroup("Check")] public List<long> Output = new List<long>();
    [FoldoutGroup("Check")] public List<long> MiddleStepCounts = new List<long>();
    [FoldoutGroup("Check")] public long ReInput;
    
    [BurstCompile(CompileSynchronously = true)]
    private struct SingleBruteForce : IJobParallelFor
    {
        private struct SingleInfo
        {
            public float Max, Min;
            public int CurrentStep, MiddleSteps;
            public float GetCurrentValue() { return Mathf.Lerp(Min, Max, ((float)CurrentStep) / (MiddleSteps + 1f)); }
            public SingleInfo(float Max, float Min, int CurrentStep, int MiddleSteps) { this.Max = Max; this.Min = Min; this.CurrentStep = CurrentStep; this.MiddleSteps = MiddleSteps; }
        }

        [DeallocateOnJobCompletion, Unity.Collections.ReadOnly] public NativeArray<float> NativeSingles;
        [DeallocateOnJobCompletion, Unity.Collections.ReadOnly] public NativeArray<long> WeightedMiddleSteps;

        [NativeDisableParallelForRestriction]public NativeArray<float> AllValues;

        public long StartAt;
        public int TestValue1;
        public int TestValue2;

        [NativeDisableParallelForRestriction] public NativeArray<long> CorrectOnTrue, CorrectOnFalse, InCorrectOnTrue, InCorrectOnFalse;

        [Unity.Collections.ReadOnly] public NativeArray<bool> States;
        [Unity.Collections.ReadOnly] public NativeArray<float> FlatRawValues;


        public void Execute(int Index)
        {
            NativeArray<SingleInfo> ConvertedSingles = new NativeArray<SingleInfo>(NativeSingles.Length / 3, Allocator.Temp);  
            
            long LeftCount = Index + StartAt;

            int CorrectOnTrue = 0;
            int CorrectOnFalse = 0;
            int InCorrectOnTrue = 0;
            int InCorrectOnFalse = 0;
            //test for 2 and 4
            for (int i = 0; i < WeightedMiddleSteps.Length; i++)
            {
                ConvertedSingles[i] = new SingleInfo(NativeSingles[(i * 3)], NativeSingles[(i * 3) + 1], Mathf.FloorToInt(LeftCount / WeightedMiddleSteps[i]), (int)NativeSingles[(i * 3) + 2]);
                if(i == 4)
                    CorrectOnFalse = Mathf.FloorToInt(Mathf.FloorToInt(LeftCount / WeightedMiddleSteps[4]) * 100); // 5 == 1

                
                    
                    
                    //InCorrectOnFalse = Mathf.FloorToInt(Mathf.FloorToInt(LeftCount / WeightedMiddleSteps[2]) * 100); // 3 == 52
                    
                if(i == 3)
                    InCorrectOnTrue = Mathf.FloorToInt(Mathf.FloorToInt(LeftCount / WeightedMiddleSteps[3]) * 100); //4 == 52
                if (i == 5)
                    CorrectOnTrue = Mathf.FloorToInt(Mathf.FloorToInt(LeftCount / WeightedMiddleSteps[5]) * 100);
                LeftCount -= (Mathf.FloorToInt(LeftCount / WeightedMiddleSteps[i]) * WeightedMiddleSteps[i]);
            }

            ///now check max valuev
            
           


            int TotalRestrictions = FlatRawValues.Length / States.Length;
            for (int i = 0; i < TotalRestrictions; i++)
                if (ConvertedSingles[(i * 3) + 1].GetCurrentValue() < ConvertedSingles[(i * 3) + 2].GetCurrentValue()) // check if max is smaller than min to save processing power
                    return;

            

            for (int i = 0; i < FlatRawValues.Length / TotalRestrictions; i++) // all raw value input sets
            {
                float TotalWeightValue = 0f;
                float TotalWeight = 0f;
                for (int j = 0; j < TotalRestrictions; j++)// all restrictions
                {
                    float Weight = ConvertedSingles[(j * 3) + 3].GetCurrentValue();
                    float Max = ConvertedSingles[(j * 3) + 1].GetCurrentValue();
                    float Min = ConvertedSingles[(j * 3) + 2].GetCurrentValue();
                    float CurrentRawValue = FlatRawValues[(i * TotalRestrictions) + j];
                    TotalWeightValue += CurrentRawValue < Max && CurrentRawValue > Min ? Weight : 0;
                    TotalWeight += Weight;

                    if (i == Index && j == TestValue1 && false)
                    {
                        CorrectOnTrue = Mathf.FloorToInt(Max * 100) ;
                        CorrectOnFalse = Mathf.FloorToInt(ConvertedSingles[(j * 3) + 1].CurrentStep * 100) ;
                        InCorrectOnTrue = Mathf.FloorToInt(ConvertedSingles[(j * 3) + 1].MiddleSteps * 100) ;
                        InCorrectOnFalse = Mathf.FloorToInt(ConvertedSingles[(j * 3) + 1].Max * 100);
                    }

                }
                 
                float MinWeightThreshold = ConvertedSingles[0].GetCurrentValue() * TotalWeight;
                bool Guess = TotalWeightValue >= MinWeightThreshold;
                bool IsCorrect = Guess == States[i];
                
                
                /*
                CorrectOnTrue += IsCorrect && States[i] == true ? 1 : 0;
                CorrectOnFalse += IsCorrect && States[i] == false  ? 1 : 0;
                InCorrectOnTrue += !IsCorrect && States[i] == true ? 1 : 0;
                InCorrectOnFalse += !IsCorrect && States[i] == false ? 1 : 0;
                */
                }


                this.CorrectOnTrue[Index] = CorrectOnTrue;
            this.CorrectOnFalse[Index] = CorrectOnFalse;
            this.InCorrectOnTrue[Index] = InCorrectOnTrue;
            this.InCorrectOnFalse[Index] = InCorrectOnFalse;

            int Correct = CorrectOnTrue + CorrectOnFalse;
            int InCorrect = InCorrectOnTrue + InCorrectOnFalse;
            float PercentGuess = ((float)Correct / ((float)InCorrect + (float)Correct)) * 100f;
            AllValues[Index] = PercentGuess;

            ConvertedSingles.Dispose();
            //MiddleStepCounts.Dispose();
        }
    }
    /// <summary>
    /// question is why does index 0 return best info?
    /// </summary>
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
        ClearValues();
        for (int i = 0; i < Runs + 1; i++) //runner 
        {
            int RunCount = i != Runs ? MaxGroup : Remainder;
            long StartAt = i * MaxGroup;
            //Debug.Log("i: " + i + "  RunCount: " + RunCount + "  StartAt: " + StartAt);
            
            RunBruteForce(RunCount, StartAt, out float Highest, out int Index);
            Debug.Log("RealHighest: " + RealHighest + "  TryHighest: " + Highest);
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
                CorrectOnTrue = new NativeArray<long>(RunCount, Allocator.TempJob),
                CorrectOnFalse = new NativeArray<long>(RunCount, Allocator.TempJob),
                InCorrectOnTrue = new NativeArray<long>(RunCount, Allocator.TempJob),
                InCorrectOnFalse = new NativeArray<long>(RunCount, Allocator.TempJob),
                TestValue1 = Test1,
                TestValue2 = Test2,
            };

            JobHandle jobHandle = BruteForceRun.Schedule(RunCount, BatchSize);
            jobHandle.Complete();

            for (int i = 0; i < FramesToCaptureDebug; i++)
            {
                CorrectOnTrue.Add(BruteForceRun.CorrectOnTrue[i]);
                CorrectOnFalse.Add(BruteForceRun.CorrectOnFalse[i]);
                InCorrectOnTrue.Add(BruteForceRun.InCorrectOnTrue[i]);
                InCorrectOnFalse.Add(BruteForceRun.InCorrectOnFalse[i]);
            }
            float Value = 0;
            for (int i = 0; i < CorrectOnTrue.Count; i++)
            {
                Value += CorrectOnTrue[i];
            }
            Debug.Log("Average: " + (Value / CorrectOnTrue.Count));
            GetHighest(BruteForceRun.AllValues, out Highest, out Index);

            BruteForceRun.AllValues.Dispose();

            BruteForceRun.CorrectOnTrue.Dispose();
            BruteForceRun.CorrectOnFalse.Dispose();
            BruteForceRun.InCorrectOnTrue.Dispose();
            BruteForceRun.InCorrectOnFalse.Dispose();
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
        void ClearValues()
        {
            CorrectOnTrue.Clear();
            CorrectOnFalse.Clear();
            InCorrectOnTrue.Clear();
            InCorrectOnFalse.Clear();
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
            for (int j = 0; j < i - 1; j++)
                Output[j] = (long)(Output[j] * (SinglesList[i].GetTotalSteps()));
        Output.RemoveAt(0);
        Output.Add(1);

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

    [BurstCompile]
    private struct AllBruteForce : IJob
    {
        private struct SingleInfo
        {
            public float Max;
            public float Min;
            public int MiddleSteps;
            public int CurrentStep;
            public float GetCurrentValue() { return Mathf.Lerp(Min, Max, ((float)CurrentStep) / ((float)MiddleSteps + 1f)); }
            public void NextStep(out bool Max)
            {
                CurrentStep += 1;
                Max = CurrentStep == MiddleSteps + 1;
                if (Max)
                    ResetStep();
            }
            public void ResetStep() { CurrentStep = 0; }

            public SingleInfo(float Max, float Min, int MiddleSteps)
            {
                this.Max = Max;
                this.Min = Min;
                this.MiddleSteps = MiddleSteps;
                CurrentStep = 0;
            }
        }

        [DeallocateOnJobCompletion] public NativeArray<float> NativeSingles;

        public NativeArray<float> OutputValues;

        [DeallocateOnJobCompletion] public NativeArray<bool> States;
        [DeallocateOnJobCompletion] public NativeArray<float> FlatRawValues;

        //public AllChanges AllChangeStats;
        public int ExpandValue;
        public int CountNumber;

        public int MaxFrames;
        public bool UseFrameCount;

        public void Execute()
        {
            bool WaitingForFinish = true;
            NativeArray<SingleInfo> ConvertedSingles = new NativeArray<SingleInfo>(NativeSingles.Length / 3, Allocator.Temp);
            for (int i = 0; i < NativeSingles.Length / 3; i++)// ConvertedSingles
            {
                ConvertedSingles[i] = new SingleInfo(NativeSingles[(i * 3)], NativeSingles[(i * 3) + 1], (int)NativeSingles[(i * 3) + 2]);
            }
            while (WaitingForFinish)
            {
                while (true)
                {
                    CountNumber += 1;
                    for (int i = 0; i < ConvertedSingles.Length; i++) //next frame
                    {
                        ConvertedSingles[i].NextStep(out bool HitMax);
                        if (HitMax == false)
                            break;
                        if (i == ConvertedSingles.Length - 1)
                        {
                            WaitingForFinish = false;
                            break;
                        }

                    }
                    bool HasConflict = false;
                    for (int i = 0; i < (ConvertedSingles.Length + 1) / 3; i++)
                        if (ConvertedSingles[(i * 3) + 1].GetCurrentValue() <= ConvertedSingles[(i * 3) + 2].GetCurrentValue())
                            HasConflict = true;

                    if (HasConflict == false)
                        break;
                }

                if (!WaitingForFinish || UseFrameCount && CountNumber > MaxFrames)
                    break;


                int Correct = 0;
                int InCorrect = 0;
                for (int i = 0; i < FlatRawValues.Length / ExpandValue; i++) // all raw value inputs
                {
                    float TotalWeightValue = 0f;
                    float TotalWeight = 0f;
                    for (int j = 0; j < ExpandValue; j++)// all restrictions
                    {
                        float Weight = ConvertedSingles[(j * 3) + 3].GetCurrentValue();
                        TotalWeightValue += Weight * FlatRawValues[(i * ExpandValue) + j] < ConvertedSingles[(j * 3) + 1].GetCurrentValue() && FlatRawValues[(i * ExpandValue) + j] > ConvertedSingles[(j * 3) + 2].GetCurrentValue() ? 1 : 0;
                        TotalWeight += Weight;
                    }
                    float MinWeightThreshold = ConvertedSingles[0].GetCurrentValue() * TotalWeight;
                    bool IsCorrect = (TotalWeightValue >= MinWeightThreshold) == States[i];
                    Correct += IsCorrect ? 1 : 0;
                    InCorrect += IsCorrect ? 0 : 1;
                }
                float PercentGuess = (Correct / (Correct + InCorrect)) * 100f;
                if (PercentGuess > OutputValues[0])
                {
                    OutputValues[0] = PercentGuess;
                    for (int i = 1; i < ConvertedSingles.Length; i++)
                        OutputValues[i] = ConvertedSingles[i].GetCurrentValue();
                }
            }
            ConvertedSingles.Dispose();
        }
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


