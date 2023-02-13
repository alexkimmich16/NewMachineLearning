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
    [FoldoutGroup("BruteForce")] public List<SingleFrameRestrictionValues> FrameInfo;

    [FoldoutGroup("BruteForce")] public long MaxFrames;
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
        [DeallocateOnJobCompletion, Unity.Collections.ReadOnly] public NativeArray<float3> Natives; //x, y, z
        [DeallocateOnJobCompletion, Unity.Collections.ReadOnly] public NativeArray<long> WeightedMiddleSteps;

        [NativeDisableParallelForRestriction] public NativeArray<float> AllValues;

        public long StartAt;

        //[NativeDisableParallelForRestriction] public NativeArray<long> CorrectOnTrue, CorrectOnFalse, InCorrectOnTrue, InCorrectOnFalse;
        ///min = x, max = y, middlesteps = z;
        [Unity.Collections.ReadOnly] public NativeArray<bool> States;
        [Unity.Collections.ReadOnly] public NativeArray<float> FlatRawValues;


        public void Execute(int Index)
        {
            
            float GetCurrentValue(float4 Values) { return Mathf.Lerp(Values.w, Values.x, (Values.y + 1f) / (Values.z + 1f)); }
            long LeftCount = Index + StartAt;
            int RestrictionCount = FlatRawValues.Length / States.Length;//4
            int SinglesPerRestriction = Natives.Length / RestrictionCount;//5


            NativeArray<float4> ConvertedSingles = new NativeArray<float4>(Natives.Length, Allocator.Temp); 
            for (int i = 0; i < WeightedMiddleSteps.Length; i++)
            {
                
                ConvertedSingles[i] = new float4(Natives[i].x, Natives[i].y, Mathf.FloorToInt(LeftCount / WeightedMiddleSteps[i]), Natives[i].z);
                //ConvertedSingles[i] = new SingleInfo(Natives[(i * 3)], Natives[(i * 3) + 1], Mathf.FloorToInt(LeftCount / WeightedMiddleSteps[i]), (int)Natives[(i * 3) + 2]);     
                LeftCount -= (Mathf.FloorToInt(LeftCount / WeightedMiddleSteps[i]) * WeightedMiddleSteps[i]);
            }

            NativeArray<int2> Checks = new NativeArray<int2>(3, Allocator.Temp) { [0] = new int2(3,1), [1] = new int2(1, 0), [2] = new int2(0, 2) }; 
            for (int i = 0; i < RestrictionCount; i++)//4 checks all restrictions
                for (int j = 0; j < Checks.Length; j++)//3
                    if (GetCurrentValue(ConvertedSingles[(i * SinglesPerRestriction) + Checks[j].x]) > GetCurrentValue(ConvertedSingles[(i * SinglesPerRestriction) + Checks[j].y]))
                    {
                        Conclude();
                        return;
                    }
            Checks.Dispose();
            for (int i = 0; i < ConvertedSingles.Length; i++) //stop repeats for already found variables
                if (ConvertedSingles[i].w == ConvertedSingles[i].x && ConvertedSingles[i].y != ConvertedSingles[i].z - 1)//if already found and not top
                {
                    Conclude();
                    return;
                }

            int2 Corrects = int2.zero;

            float TotalGuesses = (FlatRawValues.Length / RestrictionCount);
            float LowestPercent = 0.60f;
            int MaxWrongGuesses = Mathf.FloorToInt(Mathf.Log((TotalGuesses / LowestPercent/ (1f - LowestPercent)), 2));
            for (int i = 0; i < FlatRawValues.Length / RestrictionCount; i++) // all raw value input sets
            {
                float TotalWeightValue = 0f;
                for (int j = 0; j < RestrictionCount; j++)// all 3 or 4 restrictions etc velocity
                {
                    float GetValue(int Index) { return GetCurrentValue(ConvertedSingles[(j * SinglesPerRestriction) + Index]); }

                    TotalWeightValue += GetOutput(FlatRawValues[(i * RestrictionCount) + j]) * (GetValue(4) > 0 ? GetValue(4) : 0);
                    if (MaxWrongGuesses < Corrects.y)
                    {
                        Conclude();
                        return;
                    }

                    float GetOutput(float Input) { return Input < GetValue(0) && Input > GetValue(1) ? 1 : Input < GetValue(3) || Input > GetValue(4) ? 0 : Input > GetValue(3) && Input < GetValue(1) ? 1f - Remap(Input, new float2(GetValue(3), GetValue(1))) : Remap(Input, new float2(GetValue(0), GetValue(1))); }
                    float Remap(float Input, float2 MaxMin) { return (Input - MaxMin.x) / (MaxMin.y - MaxMin.x); }
                }
                bool Guess = TotalWeightValue >= 1;
                Corrects = new int2(Corrects.x + (Guess == States[i] ? 1 : 0), Corrects.y + (!Guess == States[i] ? 1 : 0));
            }
            void Conclude()
            {
                ConvertedSingles.Dispose();
            }
            AllValues[Index] = (Corrects.x / (Corrects.x + Corrects.y)) * 100f;
            Conclude();
        }
    }
    #region Stats
    NativeArray<bool> GetStatesStat()//constant
    {
        NativeArray<bool> StatesStat = new NativeArray<bool>(FrameInfo.Count, Allocator.TempJob);
        for (int i = 0; i < FrameInfo.Count; i++)
            StatesStat[i] = FrameInfo[i].AtMotionState;
        //Debug.Log("GetStatesStat: " + StatesStat.Length);
        return StatesStat;
    }
    NativeArray<float> GetFlatRawStat()//constant
    {
        NativeArray<float> FlatRawStat = new NativeArray<float>(FrameInfo.Count * FrameInfo[0].OutputRestrictions.Count, Allocator.TempJob);
        for (int i = 0; i < FrameInfo.Count; i++)
            for (int j = 0; j < FrameInfo[0].OutputRestrictions.Count; j++)
                FlatRawStat[(i * FrameInfo[0].OutputRestrictions.Count) + j] = FrameInfo[i].OutputRestrictions[j];
        //Debug.Log("GetFlatRawStat: " + FlatRawStat.Length);
        return FlatRawStat;
    }
    NativeArray<float3> GetAllChangeStatsInput(AllChanges AllChanges)
    {
        List<AllChanges.SingleChange> Singles = AllChanges.GetSingles();
        NativeArray<float3> AllChangeStatsInput = new NativeArray<float3>(Singles.Count, Allocator.TempJob);//all change sttats
        for (int i = 0; i < Singles.Count; i++)
            AllChangeStatsInput[i] = new float3(Singles[i].GuessingMax, Singles[i].GuessingMin, Singles[i].GetTotalSteps());
        //Debug.Log("AllChangeStatsInput: " + AllChangeStatsInput.Length);
        return AllChangeStatsInput;
    }
    NativeArray<long> GetMiddleValueList(AllChanges AllChanges)
    {
        List<long> MiddleStepCounts = GetMiddleStats(AllChanges.GetSingles());
        NativeArray<long> MiddleValueList = new NativeArray<long>(MiddleStepCounts.Count, Allocator.TempJob);
        for (int i = 0; i < MiddleStepCounts.Count; i++)
            MiddleValueList[i] = MiddleStepCounts[i];
        //Debug.Log("GetMiddleValueList: " + MiddleValueList.Length);
        return MiddleValueList;
    }
    #endregion

    [FoldoutGroup("BruteForce"), Button(ButtonSizes.Small)]
    public void RunBruteForce() { DoBruteForceTest(new MotionRestriction(RestrictionManager.instance.RestrictionSettings.MotionRestrictions[(int)motionGet - 1]), AllChangesList[(int)motionGet - 1], out float Value); }

    public void DoBruteForceTest(MotionRestriction Restriction, AllChanges allChanges, out float Value)
    {
        Value = 0;
        float StartTime = Time.realtimeSinceStartup;
        for (int v = 0; v < Sequences; v++)
        {
            if(FrameInfo.Count == 0)
                FrameInfo = GetRestrictionsForMotions(motionGet, Restriction); //correct

            int Runs = Mathf.FloorToInt(MaxFrames / MaxGroup);
            int Remainder = (int)(MaxFrames - ((long)Runs * (long)MaxGroup));

            float RealHighest = 0;
            long RealIndex = 0;
            for (int i = 0; i < Runs + 1; i++) //runner 
            {
                int RunCount = i != Runs ? MaxGroup : Remainder;
                long StartAt = i * MaxGroup;

                SingleBruteForce BruteForceRun = new SingleBruteForce
                {
                    Natives = GetAllChangeStatsInput(allChanges),
                    States = GetStatesStat(),
                    StartAt = StartAt,
                    FlatRawValues = GetFlatRawStat(),
                    AllValues = new NativeArray<float>(RunCount, Allocator.TempJob),
                    WeightedMiddleSteps = GetMiddleValueList(allChanges),
                };

                JobHandle jobHandle = BruteForceRun.Schedule(RunCount, 1);
                jobHandle.Complete();

                for (int j = 0; j < BruteForceRun.AllValues.Length; j++)
                {
                    if (BruteForceRun.AllValues[j] > RealHighest)
                    {
                        RealHighest = BruteForceRun.AllValues[j];
                        RealIndex = j + StartAt;
                    }
                }
                BruteForceRun.AllValues.Dispose();
            }

            List<long> FinalStats = GetOutputList(RealIndex, GetMiddleStats(allChanges.GetSingles()));

            Values = new List<long>(FinalStats);
            List<AllChanges.SingleChange> Changes = allChanges.GetSingles();

            List<SingleRestriction> NewList = new List<SingleRestriction>();


            int SinglesPerRestriction = Changes.Count / Restriction.Restrictions.Count;
            for (int i = 0; i < Restriction.Restrictions.Count; i++) // get singlerestrictionlist
            {
                SingleRestriction NewRestriction = Restriction.Restrictions[i];
                for (int j = 0; j < 5; j++)
                    NewRestriction.SetOutputValue(j, Changes[(i * SinglesPerRestriction) + j].GetCurrentValueAt((int)FinalStats[(i * SinglesPerRestriction) + j]));
                NewList.Add(NewRestriction);
            }

            Restriction.Restrictions = NewList;
            List<AllChanges.SingleChange> NewChanges = new List<AllChanges.SingleChange>();
            for (int i = 0; i < Changes.Count; i++)
            {

                float Range = ((Changes[i].GuessingMax - Changes[i].GuessingMin) / 2) * Confidence;
                float NewMax = Range > StopAdjustingPrecision ? Changes[i].GetCurrentValueAt((int)FinalStats[i]) + Range : Changes[i].GetCurrentValueAt((int)FinalStats[i]);
                float NewMin = Range > StopAdjustingPrecision ? Changes[i].GetCurrentValueAt((int)FinalStats[i]) - Range : Changes[i].GetCurrentValueAt((int)FinalStats[i]);

                int RestrictionValue = Mathf.FloorToInt(i / 5);
                int CountLeft = i - RestrictionValue * 5;

                AllChanges.OneRestrictionChange Rest = allChanges.Restrictions[RestrictionValue];
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
            allChanges = new AllChanges(NewChanges);
            Value = RealHighest;

            //Debug.Log("BestIndex: " + RealIndex + "  BestValue: " + RealHighest);
            
        }
        Debug.Log("Frames: " + MaxFrames + " in: " + (Time.realtimeSinceStartup - StartTime).ToString("F5") + " Seconds");
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
    [ListDrawerSettings(Expanded = false, ShowIndexLabels = true)]public List<OneRestrictionChange> Restrictions;
    public AllChanges(string Motion, List<OneRestrictionChange> Restrictions)
    {
        this.Motion = Motion;
        this.Restrictions = Restrictions;
    }
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


