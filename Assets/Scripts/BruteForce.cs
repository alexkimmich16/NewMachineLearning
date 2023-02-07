using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using RestrictionSystem;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Collections;
using System.Linq;
public class BruteForce : SerializedMonoBehaviour
{
    public static BruteForce instance;
    private void Awake() { instance = this; }

    public CurrentLearn motionGet;
    public int PastFrameLookup;

    [ListDrawerSettings(ShowIndexLabels = true, ListElementLabelName = "Motion"), FoldoutGroup("BruteForce")] public List<AllChanges> AllChangesList;
    [FoldoutGroup("BruteForce")] public MotionRestriction BruteForceSettings;
    [FoldoutGroup("BruteForce")] public List<SingleFrameRestrictionValues> FrameInfo;

    [FoldoutGroup("BruteForce")] public long MaxFrames;
    [FoldoutGroup("BruteForce")] private int BatchSize = 1;
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
            long LeftCount = Index + StartAt;
            int RestrictionCount = FlatRawValues.Length / States.Length;//4
            int SinglesPerRestriction = (NativeSingles.Length / 3) / RestrictionCount;//5
            //Debug.Log("SinglesPerRestriction: " + SinglesPerRestriction);
            NativeArray<SingleInfo> ConvertedSingles = new NativeArray<SingleInfo>(NativeSingles.Length / 3, Allocator.Temp);
            for (int i = 0; i < WeightedMiddleSteps.Length; i++)
            {
                ConvertedSingles[i] = new SingleInfo(NativeSingles[(i * 3)], NativeSingles[(i * 3) + 1], Mathf.FloorToInt(LeftCount / WeightedMiddleSteps[i]), (int)NativeSingles[(i * 3) + 2]);     
                LeftCount -= (Mathf.FloorToInt(LeftCount / WeightedMiddleSteps[i]) * WeightedMiddleSteps[i]);
            }

            NativeArray<int2> Checks = new NativeArray<int2>(3, Allocator.Temp) { [0] = new int2(3,1), [1] = new int2(1, 0), [2] = new int2(0, 2) }; 
            for (int i = 0; i < RestrictionCount; i++)//4 checks all restrictions
                for (int j = 0; j < Checks.Length; j++)//3
                    if (ConvertedSingles[(i * SinglesPerRestriction) + Checks[j].x].GetCurrentValue() > ConvertedSingles[(i * SinglesPerRestriction) + Checks[j].y].GetCurrentValue())
                        return;// check if max is smaller than min to save processing power

            for (int i = 0; i < ConvertedSingles.Length; i++) //stop repeats for already found variables
                if (ConvertedSingles[i].Max == ConvertedSingles[i].Min && ConvertedSingles[i].CurrentStep != ConvertedSingles[i].MiddleSteps - 1)//if already found and not top
                    return;
            
            Vector2 Corrects = Vector2.zero;
            for (int i = 0; i < FlatRawValues.Length / RestrictionCount; i++) // all raw value input sets
            {
                float TotalWeightValue = 0f;
                for (int j = 0; j < RestrictionCount; j++)// all restrictions
                {
                    float MaxSafe = ConvertedSingles[(j * SinglesPerRestriction) + 0].GetCurrentValue();
                    float MinSafe = ConvertedSingles[(j * SinglesPerRestriction) + 1].GetCurrentValue();
                    float MaxFalloff = ConvertedSingles[(j * SinglesPerRestriction) + 2].GetCurrentValue();
                    float MinFalloff = ConvertedSingles[(j * SinglesPerRestriction) + 3].GetCurrentValue();
                    float Weight = ConvertedSingles[(j * SinglesPerRestriction) + 4].GetCurrentValue();

                    //weight later after
                    //
                    float Value = FlatRawValues[(i * RestrictionCount) + j];
                    TotalWeightValue += GetOutput(Value) * (Weight > 0 ? Weight : 0);
                    //Debug.Log("Value: " + Values[i].Values[j] + "  Max: " + Max + "  Min: " + Min + "  j: " + j);

                    float GetOutput(float Input)
                    {
                        if (Input < MaxSafe && Input > MinSafe)
                            return 1f;
                        else if (Input < MinFalloff || Input > MaxFalloff)
                            return 0f;
                        else
                        {
                            bool IsLowSide = Input > MinFalloff && Input < MinSafe;
                            float DistanceValue = IsLowSide ? 1f - Remap(Input, new Vector2(MinFalloff, MinSafe)) : Remap(Input, new Vector2(MaxSafe, MaxFalloff));
                            return DistanceValue;
                        }
                        float Remap(float Input, Vector2 MaxMin) { return (Input - MaxMin.x) / (MaxMin.y - MaxMin.x); }
                    }

                }
                bool Guess = TotalWeightValue >= 1;
                bool IsCorrect = Guess == States[i];
                Corrects = new Vector2(Corrects.x + (IsCorrect ? 1f : 0f), Corrects.y + (!IsCorrect ? 1f : 0f));
                //if(Index < 10)
                    //Debug.Log("TotalWeightValue: " + TotalWeightValue);
            }
            
            AllValues[Index] = (Corrects.x / (Corrects.x + Corrects.y)) * 100f;
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
            //Debug.Log("GetStatesStat: " + StatesStat.Length);
            return StatesStat;
        }
        NativeArray<float> GetFlatRawStat()
        {
            NativeArray<float> FlatRawStat = new NativeArray<float>(FrameInfo.Count * FrameInfo[0].OutputRestrictions.Count, Allocator.TempJob);
            for (int i = 0; i < FrameInfo.Count; i++)
                for (int j = 0; j < FrameInfo[0].OutputRestrictions.Count; j++)
                    FlatRawStat[(i * FrameInfo[0].OutputRestrictions.Count) + j] = FrameInfo[i].OutputRestrictions[j];
            //Debug.Log("GetFlatRawStat: " + FlatRawStat.Length);
            return FlatRawStat;
        }
        NativeArray<float> GetAllChangeStatsInput()
        {
            List<AllChanges.SingleChange> Singles = AllChangesList[(int)motionGet - 1].GetSingles();
            NativeArray<float> AllChangeStatsInput = new NativeArray<float>(Singles.Count * 3, Allocator.TempJob);//all change sttats
            for (int i = 0; i < Singles.Count; i++)
            {
                AllChangeStatsInput[(i * 3)] = Singles[i].GuessingMax;
                AllChangeStatsInput[(i * 3) + 1] = Singles[i].GuessingMin;
                AllChangeStatsInput[(i * 3) + 2] = Singles[i].GetTotalSteps();
            }
            //Debug.Log("AllChangeStatsInput: " + AllChangeStatsInput.Length);
            return AllChangeStatsInput;
        }
        NativeArray<long> GetMiddleValueList()
        {
            List<long> MiddleStepCounts = GetMiddleStats(AllChangesList[(int)motionGet - 1].GetSingles());
            NativeArray<long> MiddleValueList = new NativeArray<long>(MiddleStepCounts.Count, Allocator.TempJob);
            for (int i = 0; i < MiddleStepCounts.Count; i++)
                MiddleValueList[i] = MiddleStepCounts[i];
            //Debug.Log("GetMiddleValueList: " + MiddleValueList.Length);
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
        for (int v = 0; v < Sequences; v++)
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

            List<long> FinalStats = GetOutputList(RealIndex, GetMiddleStats(AllChangesList[(int)motionGet - 1].GetSingles()));
            Values = new List<long>(FinalStats);

            
            List<AllChanges.SingleChange> Changes = AllChangesList[(int)motionGet - 1].GetSingles();

            List<SingleRestriction> NewList = new List<SingleRestriction>();


            int SinglesPerRestriction = Changes.Count / BruteForceSettings.Restrictions.Count;
            for (int i = 0; i < BruteForceSettings.Restrictions.Count; i++)
            {
                SingleRestriction NewRestriction = BruteForceSettings.Restrictions[i];
                NewRestriction.MaxSafe = Changes[(i * SinglesPerRestriction) + 0].GetCurrentValueAt((int)FinalStats[(i * SinglesPerRestriction) + 0]);
                NewRestriction.MinSafe = Changes[(i * SinglesPerRestriction) + 1].GetCurrentValueAt((int)FinalStats[(i * SinglesPerRestriction) + 1]);
                NewRestriction.MaxFalloff = Changes[(i * SinglesPerRestriction) + 2].GetCurrentValueAt((int)FinalStats[(i * SinglesPerRestriction) + 2]);
                NewRestriction.MinFalloff = Changes[(i * SinglesPerRestriction) + 3].GetCurrentValueAt((int)FinalStats[(i * SinglesPerRestriction) + 3]);
                //Debug.Log(Changes[(i * SinglesPerRestriction) + 4].GetCurrentValueAt((int)FinalStats[(i * SinglesPerRestriction) + 4]));
                NewRestriction.Weight = Changes[(i * SinglesPerRestriction) + 4].GetCurrentValueAt((int)FinalStats[(i * SinglesPerRestriction) + 4]);
                NewList.Add(NewRestriction);
            }

            BruteForceSettings.Restrictions = NewList;
            List<AllChanges.SingleChange> NewChanges = new List<AllChanges.SingleChange>();
            for (int i = 0; i < Changes.Count; i++)
            {

                float Range = ((Changes[i].GuessingMax - Changes[i].GuessingMin) / 2) * Confidence;
                float NewMax = Range > StopAdjustingPrecision ? Changes[i].GetCurrentValueAt((int)FinalStats[i]) + Range : Changes[i].GetCurrentValueAt((int)FinalStats[i]);
                float NewMin = Range > StopAdjustingPrecision ? Changes[i].GetCurrentValueAt((int)FinalStats[i]) - Range : Changes[i].GetCurrentValueAt((int)FinalStats[i]);

                //4 = (4 / 5) -> 0 && 4 - 0 * 5 = 4
                int Restriction = Mathf.FloorToInt(i / 5);
                int CountLeft = i - Restriction * 5;

                AllChanges.OneRestrictionChange Rest = AllChangesList[(int)motionGet - 1].Restrictions[Restriction];
                if (CountLeft == 0)
                    NewChanges.Add(new AllChanges.SingleChange(NewMax, NewMin, Rest.Max.MiddleSteps, Rest.Max.CurrentStep));
                if (CountLeft == 1)
                    NewChanges.Add(new AllChanges.SingleChange(NewMax, NewMin, Rest.Min.MiddleSteps, Rest.Min.CurrentStep));
                if (CountLeft == 2)
                    NewChanges.Add(new AllChanges.SingleChange(NewMax, NewMin, Rest.MaxFalloff.MiddleSteps, Rest.MaxFalloff.CurrentStep));
                if (CountLeft == 3)
                    NewChanges.Add(new AllChanges.SingleChange(NewMax, NewMin, Rest.MinFalloff.MiddleSteps, Rest.MinFalloff.CurrentStep));
                if (CountLeft == 4)
                    NewChanges.Add(new AllChanges.SingleChange(NewMax, NewMin, Rest.Weight.MiddleSteps, Rest.Weight.CurrentStep));
            }
            //Debug.Log("NewChanges: " + NewChanges.Count);
            AllChangesList[(int)motionGet - 1] = new AllChanges(NewChanges);
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
        for (int i = 0; i < SinglesList.Count; i++)
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
    public List<SingleFrameRestrictionValues> GetRestrictionsForMotions(CurrentLearn FrameDataMotion, MotionRestriction RestrictionsMotion)
    {
        List<SingleFrameRestrictionValues> ReturnValue = new List<SingleFrameRestrictionValues>();
        List<int> ToCheck = UseAllMotions ? new List<int>() { 0, 1, 2, 3 } : new List<int>(){ 0, (int)FrameDataMotion };
        for (int i = 0; i < ToCheck.Count; i++)//motions
            for (int j = 0; j < LearnManager.instance.MovementList[ToCheck[i]].Motions.Count; j++)//set
                for (int k = PastFrameLookup; k < LearnManager.instance.MovementList[ToCheck[i]].Motions[j].Infos.Count; k++)//frame
                {
                    List<float> OutputRestrictions = new List<float>();
                    for (int l = 0; l < RestrictionsMotion.Restrictions.Count; l++)
                    {
                        OutputRestrictions.Add(RestrictionManager.RestrictionDictionary[RestrictionsMotion.Restrictions[l].restriction].Invoke(RestrictionsMotion.Restrictions[l], LearnManager.instance.MovementList[ToCheck[i]].GetRestrictionInfoAtIndex(j, k - PastFrameLookup), LearnManager.instance.MovementList[ToCheck[i]].GetRestrictionInfoAtIndex(j, k)));
                    }
                        
                    ReturnValue.Add(new SingleFrameRestrictionValues(OutputRestrictions, ToCheck[i] == (int)FrameDataMotion && LearnManager.instance.MovementList[ToCheck[i]].Motions[j].AtFrameState(k)));
                }
        return ReturnValue;
    }
}
[System.Serializable]
public struct AllChanges
{
    public string Motion;
    public List<OneRestrictionChange> Restrictions;
    public AllChanges(List<SingleChange> NewInfo)
    {
        Motion = "";
        int SinglesPerRestriction = 5;
        List<OneRestrictionChange> NewRestrictions = new List<OneRestrictionChange>();
        for (int i = 0; i < (NewInfo.Count / SinglesPerRestriction); i++)
        {
            OneRestrictionChange MainRestriction = new OneRestrictionChange();
            MainRestriction.Max = NewInfo[(i * SinglesPerRestriction) + 0];
            MainRestriction.Min = NewInfo[(i * SinglesPerRestriction) + 1];
            MainRestriction.MaxFalloff = NewInfo[(i * SinglesPerRestriction) + 2];
            MainRestriction.MinFalloff = NewInfo[(i * SinglesPerRestriction) + 3];
            MainRestriction.Weight = NewInfo[(i * SinglesPerRestriction) + 4];
            NewRestrictions.Add(MainRestriction);
        }
        Restrictions = NewRestrictions;
    }
    public List<SingleChange> GetSingles()
    {
        List<SingleChange> SinglesList = new List<SingleChange>();
        for (int i = 0; i < Restrictions.Count; i++)
        {
            SinglesList.Add(Restrictions[i].Max);
            SinglesList.Add(Restrictions[i].Min);
            SinglesList.Add(Restrictions[i].MaxFalloff);
            SinglesList.Add(Restrictions[i].MinFalloff);
            SinglesList.Add(Restrictions[i].Weight);
        }
        return SinglesList;
    }

    [System.Serializable]
    public struct OneRestrictionChange
    {
        public SingleChange Max;
        public SingleChange Min;
        public SingleChange MaxFalloff;
        public SingleChange MinFalloff;
        public SingleChange Weight;
    }
    [System.Serializable]
    public struct SingleChange
    {
        public enum Limit
        {
            Variable = 0,
            Limit = 1,
            Zero = 2,
        }

        public float GuessingMax;
        public float GuessingMin;

        [Range(0,50)]public int MiddleSteps;
        
        [Sirenix.OdinInspector.ReadOnly] public int CurrentStep;

        public SingleChange(float Max, float Min, int MiddleSteps, int CurrentStep)
        {
            this.GuessingMax = Max;
            this.GuessingMin = Min;
            this.MiddleSteps = MiddleSteps;
            this.CurrentStep = CurrentStep;
        }
        //public float GetCurrentValueAt(int ) { return Mathf.Lerp(Min, Max, ((float)CurrentStep) / (MiddleSteps + 1f)); }
        public void SetNewMaxMin(float Max, float Min)
        {
            Debug.Log(Max);
            this.GuessingMax = Max;
            this.GuessingMin = Min;
        }
        public int GetTotalSteps() { return GuessingMax == GuessingMin ? 1 : MiddleSteps; }
        public float GetCurrentValue() { return Mathf.Lerp(GuessingMin, GuessingMax, ((float)CurrentStep + 1f) / (MiddleSteps + 1f)); }
        public float GetCurrentValueAt(int NewCurrentStep) { return Mathf.Lerp(GuessingMin, GuessingMax, ((float)NewCurrentStep + 1f) / (MiddleSteps + 1f)); }
    }

}

[System.Serializable]
public struct SingleFrameRestrictionValues
{
    [ListDrawerSettings(HideRemoveButton = false, Expanded = true)]
    public List<float> OutputRestrictions;
    public bool AtMotionState;
    public SingleFrameRestrictionValues(List<float> OutputRestrictionsStat, bool AtMotionState)
    {
        OutputRestrictions = OutputRestrictionsStat;
        this.AtMotionState = AtMotionState;
    }
}
[System.Serializable]
public struct MotionRestrictionStruct
{
    [Range(0, 100)] public float Highest;
    public List<OneRestrictionInfo> RestrictionInfos;
    /*
    public MotionRestrictionStruct(List<float> EncodedStatsList)
    {
        this.Highest = EncodedStatsList[0];
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
        //EncodedStatsList.RemoveAt(0);
        List<OneRestrictionInfo> GetInfo = new List<OneRestrictionInfo>();
        for (int i = 0; i < output.RestrictionInfos.Count; i++)
        {
            GetInfo.Add(output.RestrictionInfos[i]);
        }
        RestrictionInfos = GetInfo;
    }*/
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


