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
    public CurrentLearn motionGet;
    public int PastFrameLookup;

    //[FoldoutGroup("BruteForce")] public int CheckPerFrame;
    
    [FoldoutGroup("BruteForce")] public AllChanges AllChange;
    [Sirenix.OdinInspector.ReadOnly, FoldoutGroup("BruteForce")] public MotionRestriction BruteForceSettings;
    [Sirenix.OdinInspector.ReadOnly, FoldoutGroup("BruteForce")] public List<SingleFrameRestrictionInfo> FrameInfo;

    [FoldoutGroup("BruteForce")] public bool UseFrameCount;
    [FoldoutGroup("BruteForce"), ShowIf("UseFrameCount")] public int MaxFrames;
    [FoldoutGroup("BruteForce")] public int BatchSize;
    [FoldoutGroup("BruteForce")] public int MaxGroup;
    [FoldoutGroup("BruteForce"), Sirenix.OdinInspector.ReadOnly] public int TotalFrameCount;
    [FoldoutGroup("BruteForce"), Button(ButtonSizes.Small)]
    public void SetHighestNum() { TotalFrameCount = TotalFramesToCheck(); }

    [FoldoutGroup("BruteForce")] public List<float> Values;

    //[FoldoutGroup("BruteForce") hide] public List<float> Test1;
    //[FoldoutGroup("BruteForce")] public List<float> Test2;
    //[FoldoutGroup("BruteForce")] public List<float> Test3;
    


    [FoldoutGroup("Check")] public long Input;
    [FoldoutGroup("Check")] public List<long> Output = new List<long>();
    [FoldoutGroup("Check")] public List<long> MiddleStepCounts = new List<long>();
    [FoldoutGroup("Check")] public long ReInput;
    //
    [BurstCompile(CompileSynchronously = true)]
    private struct SingleBruteForce : IJobParallelFor
    {
        private struct SingleInfo
        {
            public float Max;
            public float Min;
            public int CurrentStep;
            public int MiddleSteps;
            public float GetCurrentValue() { return Mathf.Lerp(Min, Max, ((float)CurrentStep) / (MiddleSteps + 1f)); }

            public SingleInfo(float Max, float Min, int CurrentStep, int MiddleSteps) { this.Max = Max; this.Min = Min; this.CurrentStep = CurrentStep; this.MiddleSteps = MiddleSteps; }
        }

        [DeallocateOnJobCompletion, Unity.Collections.ReadOnly] public NativeArray<float> NativeSingles;
        [DeallocateOnJobCompletion, Unity.Collections.ReadOnly] public NativeArray<long> WeightedMiddleSteps;
        //[Unity.Collections.ReadOnly]

        [NativeDisableParallelForRestriction]public NativeArray<float> AllValues;

        //[NativeDisableParallelForRestriction] public NativeArray<long> Test1In, Test2In, Test3In;

        [Unity.Collections.ReadOnly] public NativeArray<bool> States;
        [Unity.Collections.ReadOnly] public NativeArray<float> FlatRawValues;

        public int CurrentGroup;
        public int MaxGroup;


        public void Execute(int Index)
        {
            long RealIndex = CurrentGroup


            NativeArray<SingleInfo> ConvertedSingles = new NativeArray<SingleInfo>(NativeSingles.Length / 3, Allocator.Temp);  

            int TotalRestrictions = FlatRawValues.Length / States.Length;
            long LeftCount = Index;

            for (int i = 0; i < WeightedMiddleSteps.Length; i++)
            {
                ConvertedSingles[i] = new SingleInfo(NativeSingles[(i * 3)], NativeSingles[(i * 3) + 1], Mathf.FloorToInt(LeftCount / WeightedMiddleSteps[i]), (int)NativeSingles[(i * 3) + 2]);
                LeftCount -= (Mathf.FloorToInt(LeftCount / WeightedMiddleSteps[i]) * WeightedMiddleSteps[i]);
            }

            for (int i = 0; i < TotalRestrictions; i++)
                if (ConvertedSingles[(i * 3) + 1].GetCurrentValue() < ConvertedSingles[(i * 3) + 2].GetCurrentValue()) // check if max is smaller than min to save processing power
                    return;

            int Correct = 0;
            int InCorrect = 0;
            for (int i = 0; i < FlatRawValues.Length / TotalRestrictions; i++) // all raw value input sets
            {
                float TotalWeightValue = 0f;
                float TotalWeight = 0f;
                for (int j = 0; j < TotalRestrictions; j++)// all restrictions
                {
                    float Weight = ConvertedSingles[(j * 3) + 3].GetCurrentValue();

                    TotalWeightValue += FlatRawValues[(i * TotalRestrictions) + j] < ConvertedSingles[(j * 3) + 1].GetCurrentValue() && FlatRawValues[(i * TotalRestrictions) + j] > ConvertedSingles[(j * 3) + 2].GetCurrentValue() ? Weight : 0;
                    TotalWeight += Weight;
                }
                //max = 1, 4, 7,
                //min = 2, 5, 8
                //weight = 3, 6, 9
                

                float MinWeightThreshold = ConvertedSingles[0].GetCurrentValue() * TotalWeight;
                bool IsCorrect = (TotalWeightValue >= MinWeightThreshold) == States[i];

                //int Testing = 500;

               // if (i == Testing)
                    //Test1In[Index] = Mathf.FloorToInt(ConvertedSingles[0].Max * 100f);
                //if (i == Testing)
                    //Test2In[Index] = Mathf.FloorToInt(ConvertedSingles[0].MiddleSteps * 100f);
                //if (i == Testing)
                    //Test3In[Index] = Mathf.FloorToInt(ConvertedSingles[0].CurrentStep * 100f);
                //System.Convert.ToInt64(States[i]);
                Correct += IsCorrect ? 1 : 0;
                InCorrect += IsCorrect ? 0 : 1;
            }
            float PercentGuess = ((float)Correct / ((float)Correct + (float)InCorrect)) * 100f;

            AllValues[Index] = PercentGuess;

            ConvertedSingles.Dispose();
            //MiddleStepCounts.Dispose();
        }
    }

    [FoldoutGroup("BruteForce"), Button(ButtonSizes.Small)]
    public void StartBruteForceRun()
    {
        BruteForceSettings = new MotionRestriction(RestrictionManager.instance.RestrictionSettings.MotionRestrictions[(int)motionGet - 1]); //restrictions
        FrameInfo = GetRestrictionsForMotions(motionGet, BruteForceSettings);

        NativeArray<bool> StatesStat = new NativeArray<bool>(FrameInfo.Count, Allocator.TempJob);
        NativeArray<float> FlatRawStat = new NativeArray<float>(FrameInfo.Count * FrameInfo[0].OutputRestrictions.Count, Allocator.TempJob);
        for (int i = 0; i < FrameInfo.Count; i++)
        {
            StatesStat[i] = FrameInfo[i].AtMotionState;
            for (int j = 0; j < FrameInfo[0].OutputRestrictions.Count; j++)
            {
                int Index = i + (j * FrameInfo[0].OutputRestrictions.Count);
                FlatRawStat[Index] = FrameInfo[i].OutputRestrictions[j];
            }
        }
        List<AllChanges.SingleChange> Singles = AllChange.GetSingles();
        
        NativeArray<float> AllChangeStatsInput = new NativeArray<float>(Singles.Count * 3, Allocator.TempJob);//all change sttats

        for (int i = 0; i < Singles.Count; i++)
        {
            AllChangeStatsInput[(i * 3)] = Singles[i].Max;
            AllChangeStatsInput[(i * 3) + 1] = Singles[i].Min;
            AllChangeStatsInput[(i * 3) + 2] = Singles[i].GetTotalSteps();
        }
        float StartTime = Time.realtimeSinceStartup;

        List<long> MiddleStepCounts = GetMiddleStats(AllChange.GetSingles());
        NativeArray<long> MiddleValueList = new NativeArray<long>(MiddleStepCounts.Count, Allocator.TempJob);
        for (int i = 0; i < MiddleStepCounts.Count; i++)
            MiddleValueList[i] = MiddleStepCounts[i];

        int RunObjective = UseFrameCount ? MaxFrames : TotalFramesToCheck();
        int Runs = Mathf.FloorToInt(RunObjective / MaxGroup);
        int Remainder = RunObjective - (Runs * MaxGroup);

        List<float> AllScores = new List<float>();
        for (int i = 0; i < Runs; i++) //runner 
        {
            RunBruteForce(Runs, MaxGroup, i, out List<float> values);
            AllScores.AddRange(values);
            //RunBruteForce
        }
        ///add remainder

        void RunBruteForce(int RunCount, int MaxGroup, int CurrentGroup, out List<float> Values) //executor
        {
            ///only feed relevant info
            SingleBruteForce BruteForceRun = new SingleBruteForce
            {
                NativeSingles = AllChangeStatsInput,
                States = StatesStat,
                FlatRawValues = FlatRawStat,
                AllValues = new NativeArray<float>(MaxFrames + 1, Allocator.TempJob),
                WeightedMiddleSteps = MiddleValueList,
            };

            JobHandle jobHandle = BruteForceRun.Schedule(RunCount, BatchSize);
            jobHandle.Complete();

            Values = new List<float>();
            for (int i = 0; i < BruteForceRun.AllValues.Length; i++)
            {
                Values.Add(BruteForceRun.AllValues[i]);
            }

            BruteForceRun.AllValues.Dispose();
        }

        int BestIndex = 0;
        float BestValue = 0f;
        for (int i = 0; i < AllScores.Count; i++)
        {
            if (AllScores[i] > BestValue)
            {
                BestIndex = i;
                BestValue = AllScores[i];
            }
        }
        Debug.Log("BestIndex: " + BestIndex + "  BestValue: " + BestValue);
        Debug.Log("Frames: " + (UseFrameCount ? MaxFrames : TotalFramesToCheck()).ToString() + " in: " + (Time.realtimeSinceStartup - StartTime).ToString("F3") + " Seconds");



        Values.Clear();

        List<long> FinalStats = GetOutputList(BestIndex, GetMiddleStats(AllChange.GetSingles()));
        BruteForceSettings.WeightedValueThreshold = FinalStats[0];
        for (int i = 0; i < BruteForceSettings.Restrictions.Count; i++)
        {
            BruteForceSettings.Restrictions[i].MaxSafe = FinalStats[(i * 3) + 1];
            BruteForceSettings.Restrictions[i].MinSafe = FinalStats[(i * 3) + 2];
            BruteForceSettings.Restrictions[i].Weight = FinalStats[(i * 3) + 3];
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
            for (int j = 0; j < LearnManager.instance.MovementList[i].Motions.Count; j++)//set
                for (int k = PastFrameLookup; k < LearnManager.instance.MovementList[i].Motions[j].Infos.Count; k++)//frame
                {
                    List<float> OutputRestrictions = new List<float>();
                    for (int l = 0; l < RestrictionsMotion.Restrictions.Count; l++)
                        OutputRestrictions.Add(RestrictionManager.RestrictionDictionary[RestrictionsMotion.Restrictions[l].restriction].Invoke(RestrictionsMotion.Restrictions[l], LearnManager.instance.MovementList[i].GetRestrictionInfoAtIndex(j, k - PastFrameLookup), LearnManager.instance.MovementList[i].GetRestrictionInfoAtIndex(j, k)));
                    ReturnValue.Add(new SingleFrameRestrictionInfo(OutputRestrictions, LearnManager.instance.MovementList[i].Motions[j].AtFrameState(k)));
                }
        return ReturnValue;
    }

    public void OnStopForceCheck()
    {
        Debug.Log("stop");
    }
    private void Start()
    {
       // AllChanges.OnStop += OnStopForceCheck;
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
            public float GetCurrentValue() { return Mathf.Lerp(Min, Max, ((float)CurrentStep) / (MiddleSteps + 1f)); }
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
    //public delegate void StopEvent();
    //public static event StopEvent OnStop;
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
        public int GetTotalSteps() { return Max == Min ? 1 : MiddleSteps + 2; }
        public float GetCurrentValue() { return Mathf.Lerp(Min, Max, ((float)CurrentStep) / (MiddleSteps + 1f)); }
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


