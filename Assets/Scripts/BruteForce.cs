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

    [FoldoutGroup("BruteForce"), ListDrawerSettings(ShowIndexLabels = true, ListElementLabelName = "Motion")] public List<AllChanges> AllChangesList;
    [FoldoutGroup("BruteForce")] public MotionRestriction BruteForceSettings;
    [FoldoutGroup("BruteForce"), ListDrawerSettings(ShowIndexLabels = true)] public List<SingleFrameRestrictionValues> FrameInfo;

    [FoldoutGroup("BruteForce")] public long MaxFrames;
    [FoldoutGroup("BruteForce")] public int MaxGroup;
    [FoldoutGroup("BruteForce")] public bool ShouldDebug;
    [FoldoutGroup("BruteForce")] public bool UseMaxMinAsRange;
    [FoldoutGroup("BruteForce"), ShowIf("UseMaxMinAsRange"), Range(0,2)] public float MaxMinOfRangeStatOfRange;
    [FoldoutGroup("BruteForce"), ShowIf("ShouldDebug")] public int FramesToCaptureDebug;
    [FoldoutGroup("BruteForce"), Sirenix.OdinInspector.ReadOnly] public int TotalFrameCount;
    [FoldoutGroup("BruteForce"), Button(ButtonSizes.Small)]
    public void SetHighestNum() { TotalFrameCount = TotalFramesToCheck(AllChangesList[(int)motionGet - 1]); MaxFrames = TotalFramesToCheck(AllChangesList[(int)motionGet - 1]); }

    [FoldoutGroup("BruteForce")] public List<long> Values;
    [FoldoutGroup("BruteForce")] public List<long> FloatValues;
    
    [FoldoutGroup("BruteForce")] public bool UseAllMotions;
    

    [FoldoutGroup("Check")] public long Input;
    [FoldoutGroup("Check")] public List<long> Output = new List<long>();
    [FoldoutGroup("Check")] public List<long> MiddleStepCounts = new List<long>();
    [FoldoutGroup("Check")] public long ReInput;

    [FoldoutGroup("CustomCheck")] public int Sequences;
    [FoldoutGroup("CustomCheck"), Range(0,1)] public float Confidence;
    [FoldoutGroup("CustomCheck"), Range(0,1)] public float MakeCloserToMiddleMulitplier;
    [FoldoutGroup("CustomCheck")] public float StopAdjustingPrecision = 0.005f; //range at which stops

    [FoldoutGroup("Debug"), ListDrawerSettings(ShowIndexLabels = true)] public List<float> Test1;
    [FoldoutGroup("Debug"), ListDrawerSettings(ShowIndexLabels = true)] public List<float4> Test2;
    [FoldoutGroup("Debug"), ListDrawerSettings(ShowIndexLabels = true)] public List<int> Test3;
    [FoldoutGroup("Debug"), ListDrawerSettings(ShowIndexLabels = true)] public List<float4> Test4;

    [FoldoutGroup("Debug")] public List<float> Weights;

    [FoldoutGroup("Curve")] private List<AnimationCurve> RealCurves;
    [FoldoutGroup("Curve"), ListDrawerSettings(ShowIndexLabels = true)] public List<PointHolder> CurrentFrames;
    [FoldoutGroup("Curve"), Range(0,1)] public float Alpha = 0.05f;

    [FoldoutGroup("Curve"), Button(ButtonSizes.Small)]
    public void ClearCurves() { RealCurves.Clear(); }

    private void Start()
    {
        RealCurves = new List<AnimationCurve>();
        CurrentFrames = new List<PointHolder>();
    }

    [System.Serializable]
    public struct PointHolder
    {
        public List<Point> Points;
        public PointHolder(List<Point> Points)
        {
            this.Points = Points;
        }
    }
    [System.Serializable]
    public struct Point
    {
        public float Input;
        public float Output;
        public Point(float Input, float Output)
        {
            this.Input = Input;
            this.Output = Output;
        }
    }
    [FoldoutGroup("Curve"), Button(ButtonSizes.Small)]
    public void NextCurveState()
    {
        FrameInfo = GetRestrictionsForMotions(motionGet, RestrictionManager.instance.RestrictionSettings.MotionRestrictions[(int)motionGet - 1]);
        if(CurrentFrames.Count == 0)
        {
            for (int i = 0; i < FrameInfo[0].OutputRestrictions.Count; i++)
                CurrentFrames.Add(new PointHolder(new List<Point>()));

            for (int i = 0; i < FrameInfo.Count; i++)
                for (int j = 0; j < CurrentFrames.Count; j++)
                    CurrentFrames[j].Points.Add(new Point(FrameInfo[i].OutputRestrictions[j], FrameInfo[i].AtMotionState ? 0f : 2f));
            return;
        }
        ///TEST input aginst ENTIRE dataset!!
        ///input not used
        for (int i = 0; i < CurrentFrames[0].Points.Count; i++)
        {
            //Debug.Log("i: " + i + "  count2: " + MotionValues.Count);
            for (int j = 0; j < CurrentFrames.Count; j++)
            {
                float TotalCheckValue = 0f;
                for (int o = 0; o < FrameInfo.Count; o++)
                {
                    TotalCheckValue += CurrentFrames[o].Points[i].Output;
                    for (int p = 0; p < FrameInfo[0].OutputRestrictions.Count; p++)
                    {

                    }
                    
                }
                    

                float LastCurveValue = CurrentFrames[j].Points[i].Output;
                float ToAdjustAmount = 0f;
                float DistanceTo1 = Mathf.Abs(1f - TotalCheckValue);
                
                if (FrameInfo[i].AtMotionState && TotalCheckValue < 1)  //if 0.8(below) = 1.x
                {
                    ToAdjustAmount = DistanceTo1;
                    //Debug.Log("below");
                }
                else if(!FrameInfo[i].AtMotionState && TotalCheckValue > 1) //if 1.2(above) = -1.x,
                {
                    ToAdjustAmount = -DistanceTo1;
                    //Debug.Log("above");
                }
                //Debug.Log("ToAdjustAmount: " + ToAdjustAmount);
                float NewValue = LastCurveValue + (Alpha * (ToAdjustAmount));
                CurrentFrames[j].Points[i] = new Point(CurrentFrames[j].Points[i].Input, NewValue);
            }
        }
        //RealCurves = Curves;
        Debug.Log("Strength: " + CheckCurveStrength(CurrentFrames));
    }
    public float CheckCurveStrength(List<PointHolder> Info)
    {

        float2 WrongRight = new float2(0f, 0f);
        List<SingleFrameRestrictionValues> MotionValues = GetRestrictionsForMotions(motionGet, RestrictionManager.instance.RestrictionSettings.MotionRestrictions[(int)motionGet - 1]);
        for (int i = 0; i < Info[0].Points.Count; i++)
        {
            float TotalCheckValue = 0f;
            for (int j = 0; j < Info.Count; j++)
                TotalCheckValue += Info[j].Points[i].Output;
            bool Correct = (TotalCheckValue > 1) == MotionValues[i].AtMotionState;
            WrongRight = new float2(WrongRight.x + (!Correct ? 1f : 0f), WrongRight.y + (Correct ? 1f : 0f));
        }
        return (WrongRight.y / (WrongRight.x + WrongRight.y)) * 100f;
    }
    public List<float2> GetRangeOfMinMaxValues(List<SingleFrameRestrictionValues> MotionValues)
    {
        List<float2> MinMax = new List<float2>();
        for (int i = 0; i < MotionValues[0].OutputRestrictions.Count; i++)
            MinMax.Add(new float2 (1000f, 0f));

        Debug.Log("MinMax: " + MinMax.Count + " MotionValues: " + MotionValues.Count + " OutputRestrictions: " + MotionValues[0].OutputRestrictions.Count + " ");
        for (int i = 0; i < MotionValues.Count; i++)
            if (MotionValues[i].AtMotionState)
            {
                for (int j = 0; j < MinMax.Count; j++)
                {
                    //Debug.Log("i: " + i + "  j: " + j);
                    if (MinMax[j].x > MotionValues[i].OutputRestrictions[j])
                        MinMax[j] = new float2(MotionValues[i].OutputRestrictions[j], MinMax[j].y);
                    if (MinMax[j].y < MotionValues[i].OutputRestrictions[j])
                        MinMax[j] = new float2(MinMax[j].x, MotionValues[i].OutputRestrictions[j]);
                }
            }

        for (int j = 0; j < MinMax.Count; j++)
            Debug.Log("J: " + j + "  Max: " + MinMax[j].y + "  Min: " + MinMax[j].x);
        return MinMax;
    }

    //, FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Low, OptimizeFor = OptimizeFor.FastCompilation
    [BurstCompile(CompileSynchronously = true)]
    private struct SingleBruteForce : IJobParallelFor
    {
        private struct SingleInfo
        {
            public float Max, Min;
            public int CurrentStep, MiddleSteps;
            public float Multiplier;
            public float GetCurrentValue()
            {
                float OrigionalLerpValue = LerpValue(); //.25
                float LerpValueToMiddleRange = 0.5f - OrigionalLerpValue;//.25
                float AdjustedLerpValue = OrigionalLerpValue + (Multiplier * LerpValueToMiddleRange);
                return Mathf.Lerp(Min, Max, LerpValue());
            }
            public float LerpValue()
            {
                
                if (MiddleSteps % 2f == 0)
                {
                    int EachSideTotal = (int)(MiddleSteps / 2);
                    float Spacing = 0.5f * 1 / (EachSideTotal + 1);
                    bool UpperSide = CurrentStep > EachSideTotal - 1;
                    return (UpperSide ? (CurrentStep + 2) : (CurrentStep + 1)) * Spacing;

                }
                else
                {
                    int EachSideTotal = (int)(MiddleSteps - 1) / 2;
                    float Spacing = 0.5f * 1 / (EachSideTotal + 1);
                    return (CurrentStep + 1) * Spacing;
                }
            }
            public SingleInfo(float Max, float Min, int CurrentStep, int MiddleSteps, float Multiplier) { this.Max = Max; this.Min = Min; this.CurrentStep = CurrentStep; this.MiddleSteps = MiddleSteps; this.Multiplier = Multiplier; }
        }

        [DeallocateOnJobCompletion, Unity.Collections.ReadOnly] public NativeArray<float> NativeSingles;
        [DeallocateOnJobCompletion, Unity.Collections.ReadOnly] public NativeArray<int> WeightedMiddleSteps;

        [NativeDisableParallelForRestriction] public NativeArray<float> AllValues;
        //[NativeDisableParallelForRestriction] public NativeArray<float4> Test2;
        //[NativeDisableParallelForRestriction] public NativeArray<float4> Test4;
        //[NativeDisableParallelForRestriction] public NativeArray<float> Test1;

        public long StartAt;
        public float PointMultiplier;
        //public float PointMultiplier = 0.5f;

        //[NativeDisableParallelForRestriction] public NativeArray<long> CorrectOnTrue, CorrectOnFalse, InCorrectOnTrue, InCorrectOnFalse;

        [Unity.Collections.ReadOnly] public NativeArray<bool> States;
        [Unity.Collections.ReadOnly] public NativeArray<float> FlatRawValues;

        public float PreviousBest;

        public void Execute(int Index)
        {
            long LeftCount = Index + StartAt;
            int RestrictionCount = FlatRawValues.Length / States.Length;//4
            int SinglesPerRestriction = (NativeSingles.Length / 3) / RestrictionCount;//5
            //Debug.Log("SinglesPerRestriction: " + SinglesPerRestriction);
            NativeArray<SingleInfo> ConvertedSingles = new NativeArray<SingleInfo>(NativeSingles.Length / 3, Allocator.Temp);

            float4 Collect = float4.zero;
            int IndexCheck = 1000;

            //Test2[Index] = new float4(NativeSingles[0], NativeSingles[1], NativeSingles[2], NativeSingles[3]);
            for (int i = 0; i < WeightedMiddleSteps.Length; i++)
            {
                //if(Index == IndexCheck)
                   // Test2[i] = new float4(NativeSingles[(i * 3)], NativeSingles[(i * 3) + 1], (float)Mathf.FloorToInt(LeftCount / WeightedMiddleSteps[i]), (int)NativeSingles[(i * 3) + 2]);
                //if(i == WeightedMiddleSteps.Length - 1)
                //Test2[Index] = new float4(NativeSingles[(i * 3)], NativeSingles[(i * 3) + 1], NativeSingles[(i * 3) + 2], ConvertedSingles[ConvertedSingles.Length - 3].Max);
                ConvertedSingles[i] = new SingleInfo(NativeSingles[(i * 3)], NativeSingles[(i * 3) + 1], Mathf.FloorToInt(LeftCount / WeightedMiddleSteps[i]), (int)NativeSingles[(i * 3) + 2], PointMultiplier);
                LeftCount -= (Mathf.FloorToInt(LeftCount / WeightedMiddleSteps[i]) * WeightedMiddleSteps[i]);
            }
            /*
            if (Index == IndexCheck)
            {
                for (int i = 0; i < WeightedMiddleSteps.Length; i++)
                    Test4[i] = new float4(ConvertedSingles[i].Max, ConvertedSingles[i].Min, ConvertedSingles[i].CurrentStep, ConvertedSingles[i].GetCurrentValue());
            }
            
            Test2[Index] = new float4(NativeSingles[(i * 3)], NativeSingles[(i * 3) + 1], NativeSingles[(i * 3) + 2], ConvertedSingles[ConvertedSingles.Length - 3].Max);
            Test2[Index] = new float4(ConvertedSingles[ConvertedSingles.Length - 1].CurrentStep, ConvertedSingles[ConvertedSingles.Length - 2].CurrentStep, ConvertedSingles[ConvertedSingles.Length - 3].CurrentStep, ConvertedSingles[ConvertedSingles.Length - 4].CurrentStep);
            Test2[Index] = new float4(ConvertedSingles[ConvertedSingles.Length - 1].CurrentStep, ConvertedSingles[ConvertedSingles.Length - 2].CurrentStep, ConvertedSingles[ConvertedSingles.Length - 3].CurrentStep, ConvertedSingles[ConvertedSingles.Length - 4].CurrentStep);
            */

            NativeArray<int2> Checks = new NativeArray<int2>(3, Allocator.Temp) { [0] = new int2(3, 1), [1] = new int2(1, 0), [2] = new int2(0, 2) };
            for (int i = 0; i < RestrictionCount; i++)//4 checks all restrictions
                for (int j = 0; j < Checks.Length; j++)//3
                    if (ConvertedSingles[(i * SinglesPerRestriction) + Checks[j].x].GetCurrentValue() > ConvertedSingles[(i * SinglesPerRestriction) + Checks[j].y].GetCurrentValue())
                        return;// check if max is smaller than min to save processing power

            for (int i = 0; i < ConvertedSingles.Length; i++) //stop repeats for already found variables
                if (ConvertedSingles[i].Max == ConvertedSingles[i].Min && ConvertedSingles[i].CurrentStep != ConvertedSingles[i].MiddleSteps - 1)//if already found and not top
                    return;
            

            float TotalGuesses = (FlatRawValues.Length / RestrictionCount);
            float LowestPercent = 10f;
            float MaxWrongGuessesThreshold = (TotalGuesses - Mathf.Ceil(TotalGuesses * (LowestPercent  / 100f)));

            float PercentBelowLastToStop = 20f;
            float MaxWrongGuessesPrevious = (TotalGuesses - Mathf.Ceil(TotalGuesses * ((PreviousBest - PercentBelowLastToStop) / 100f)));

            


            Vector2 Corrects = Vector2.zero; // x = false
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

                    //if(i == 0 && j == 0)
                    /*
                    if (Index == IndexCheck )
                    {
                        //Test4[i] = new float4(MaxSafe, MinSafe, MaxFalloff, MinFalloff);
                    }
                    if (i == (FlatRawValues.Length / RestrictionCount) - 1 && j == 0)
                    {
                        //Test2[Index] = new float4(ConvertedSingles[(j * SinglesPerRestriction) + (ConvertedSingles.Length - 1)].AdjustedLerpValue(), ConvertedSingles[(j * SinglesPerRestriction) + (ConvertedSingles.Length - 1)].MiddleSteps, ConvertedSingles[(j * SinglesPerRestriction) + (ConvertedSingles.Length - 2)].AdjustedLerpValue(), ConvertedSingles[(j * SinglesPerRestriction) + (ConvertedSingles.Length - 2)].MiddleSteps);
                        
                    }
                    */

                    
                    bool UsePresetMin = false;
                    bool UsePastMin = false;
                    if ((Corrects.x >= MaxWrongGuessesThreshold && UsePresetMin) || (Corrects.x >= MaxWrongGuessesPrevious && UsePastMin))
                    {
                        AllValues[Index] = 3.14f;
                        return;
                    }
                    

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
                Corrects = new Vector2(Corrects.x + (!IsCorrect ? 1f : 0f), Corrects.y + (IsCorrect ? 1f : 0f));
                //if(Index < 10)
                //Debug.Log("TotalWeightValue: " + TotalWeightValue);
            }
            //Test2[Index] = new float4(ConvertedSingles[ConvertedSingles.Length - 3].GetCurrentValue(), ConvertedSingles[ConvertedSingles.Length - 3].CurrentStep, ConvertedSingles[ConvertedSingles.Length - 3].Min, ConvertedSingles[ConvertedSingles.Length - 3].Max);
            AllValues[Index] = (Corrects.y / (Corrects.x + Corrects.y)) * 100f;
            ConvertedSingles.Dispose();
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
            {
                FlatRawStat[(i * FrameInfo[0].OutputRestrictions.Count) + j] = FrameInfo[i].OutputRestrictions[j];
            }
        //Debug.Log("GetFlatRawStat: " + FlatRawStat.Length);
        return FlatRawStat;
    }
    NativeArray<float> GetAllChangeStatsInput(AllChanges AllChanges)
    {
        List<AllChanges.SingleChange> Singles = AllChanges.GetSingles();
        NativeArray<float> AllChangeStatsInput = new NativeArray<float>(Singles.Count * 3, Allocator.TempJob);//all change sttats
        for (int i = 0; i < Singles.Count; i++)
        {
            Weights.Add(Singles[i].GuessingMax);
            Weights.Add(Singles[i].GuessingMin);
            Weights.Add(Singles[i].GetTotalSteps());

            AllChangeStatsInput[(i * 3) + 0] = Singles[i].GuessingMax;
            AllChangeStatsInput[(i * 3) + 1] = Singles[i].GuessingMin;
            AllChangeStatsInput[(i * 3) + 2] = Singles[i].GetTotalSteps();
        }
        //Debug.Log("ALL: " + AllChangeStatsInput.Length);
        //Debug.Log("AllChangeStatsInput: " + AllChangeStatsInput.Length);
        return AllChangeStatsInput;
    }
    NativeArray<int> GetMiddleValueList(AllChanges AllChanges)
    {
        List<long> MiddleStepCountsLong = GetMiddleStats(AllChanges.GetSingles());
        List<int> MiddleStepCounts = new List<int>();
        for (int i = 0; i < MiddleStepCountsLong.Count; i++)
            MiddleStepCounts.Add((int)MiddleStepCountsLong[i]);
        NativeArray<int> MiddleValueList = new NativeArray<int>(MiddleStepCounts.Count, Allocator.TempJob);
        for (int i = 0; i < MiddleStepCounts.Count; i++)
            MiddleValueList[i] = MiddleStepCounts[i];
        Test3 = MiddleStepCounts;
        //Debug.Log("MIDDLE: " + Test3.Count);
        //Debug.Log("GetMiddleValueList: " + MiddleValueList.Length);
        return MiddleValueList;
    }
    #endregion

    [FoldoutGroup("BruteForce"), Button(ButtonSizes.Small)]
    public void RunBruteForce() { DoBruteForceTest(new MotionRestriction(RestrictionManager.instance.RestrictionSettings.MotionRestrictions[(int)motionGet - 1]), AllChangesList[(int)motionGet - 1], out float Value); }

    public void DoBruteForceTest(MotionRestriction Restriction, AllChanges allChanges, out float BestValue)
    {
        if (FrameInfo.Count == 0)
            FrameInfo = GetRestrictionsForMotions(motionGet, Restriction); //correct
        AllChanges CurrentAllChanges = UseMaxMinAsRange ? new AllChanges(GetRangeOfMinMaxValues(FrameInfo)) : allChanges;
        
        BestValue = 0;
        float StartTime = Time.realtimeSinceStartup;

        TotalFrameCount = TotalFramesToCheck(CurrentAllChanges); 
        MaxFrames = TotalFramesToCheck(CurrentAllChanges);

        for (int v = 0; v < Sequences; v++)
        {
            int Runs = Mathf.FloorToInt(MaxFrames / MaxGroup);
            int Remainder = (int)(MaxFrames - ((long)Runs * (long)MaxGroup));
            
            int PieCount = 0;

            long RealIndex = 0;
            float LocalBest = 0f;
            for (int i = 0; i < Runs + 1; i++) //runner 
            {
                int RunCount = i != Runs ? MaxGroup : Remainder;
                long StartAt = i * MaxGroup;

                SingleBruteForce BruteForceRun = new SingleBruteForce
                {
                    NativeSingles = GetAllChangeStatsInput(CurrentAllChanges),
                    States = GetStatesStat(),
                    StartAt = StartAt,
                    FlatRawValues = GetFlatRawStat(),
                    AllValues = new NativeArray<float>(RunCount, Allocator.TempJob),
                    //Test2 = new NativeArray<float4>(RunCount, Allocator.TempJob),
                    //Test4 = new NativeArray<float4>(RunCount, Allocator.TempJob),
                    PointMultiplier = MakeCloserToMiddleMulitplier,
                    //Test1 ,
                    //Test1 = new NativeArray<float>(RunCount, Allocator.TempJob),
                    //Test2 = new NativeArray<int2>(RunCount, Allocator.TempJob),
                    WeightedMiddleSteps = GetMiddleValueList(CurrentAllChanges),
                    PreviousBest = BestValue, 
                };

                JobHandle jobHandle = BruteForceRun.Schedule(RunCount, 1);
                jobHandle.Complete();
                for (int j = 0; j < BruteForceRun.AllValues.Length; j++)
                {
                    //Test1.Add(BruteForceRun.AllValues[j]);
                    //Test2.Add(BruteForceRun.Test2[j]);
                    //Test4.Add(BruteForceRun.Test4[j]);
                    if (BruteForceRun.AllValues[j] == 3.14f)
                        PieCount += 1;

                    if (BruteForceRun.AllValues[j] > LocalBest)
                    {
                        LocalBest = BruteForceRun.AllValues[j];
                        RealIndex = j + StartAt;

                        if(BruteForceRun.AllValues[j] > BestValue)
                        {
                            BestValue = BruteForceRun.AllValues[j];
                        }
                    }
                }

                BruteForceRun.AllValues.Dispose();
                //BruteForceRun.Test1.Dispose();
                //BruteForceRun.Test2.Dispose();
            }
            Debug.Log("PI: " + PieCount); 
            List<long> FinalStats = GetOutputList(RealIndex, GetMiddleStats(CurrentAllChanges.GetSingles()));

            Values = new List<long>(FinalStats);
            List<AllChanges.SingleChange> Changes = CurrentAllChanges.GetSingles();

            //List<SingleRestriction> NewList = new List<SingleRestriction>();

            /*
            int SinglesPerRestriction = Changes.Count / Restriction.Restrictions.Count;
            for (int i = 0; i < Restriction.Restrictions.Count; i++) // get singlerestrictionlist
            {
                SingleRestriction NewRestriction = Restriction.Restrictions[i];
                for (int j = 0; j < 5; j++)
                    NewRestriction.SetOutputValue(j, Changes[(i * SinglesPerRestriction) + j].GetCurrentValueAt((int)FinalStats[(i * SinglesPerRestriction) + j]));
                NewList.Add(NewRestriction);
            }
            
            Restriction.Restrictions = NewList;
            */


            List<AllChanges.SingleChange> NewChanges = new List<AllChanges.SingleChange>();
            for (int i = 0; i < Changes.Count; i++)
            {
                float Range = ((Changes[i].GuessingMax - Changes[i].GuessingMin) / 2) * Confidence;
                float NewMax = Range > StopAdjustingPrecision ? Changes[i].GetCurrentValueAt((int)FinalStats[i]) + Range : Changes[i].GetCurrentValueAt((int)FinalStats[i]);
                float NewMin = Range > StopAdjustingPrecision ? Changes[i].GetCurrentValueAt((int)FinalStats[i]) - Range : Changes[i].GetCurrentValueAt((int)FinalStats[i]);

                int RestrictionValue = Mathf.FloorToInt(i / 5);
                int CountLeft = i - RestrictionValue * 5;
                
                AllChanges.OneRestrictionChange Rest = CurrentAllChanges.Restrictions[RestrictionValue];
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

                if (i == 0 && CountLeft == 0)
                {
                    //Debug.Log("Max, Max: " + NewMax);
                }
            }
            //Debug.Log("NewChanges: " + NewChanges.Count);
            CurrentAllChanges = new AllChanges(NewChanges);
            //Debug.Log(CurrentAllChanges.Motion);
            //AllChangesList[(int)motionGet - 1] = allChanges;
            Debug.Log("BestIndex: " + RealIndex + "  BestValue: " + LocalBest);
            
        }
        AllChangesList[(int)motionGet - 1] = CurrentAllChanges;
        Debug.Log("Frames: " + MaxFrames + " in: " + (Time.realtimeSinceStartup - StartTime).ToString("F5") + " Seconds");
    }
    /*
    public List<SingleFrameRestrictionValues> WorkingValues(List<SingleFrameRestrictionValues> AllValues)
    {
        List<SingleFrameRestrictionValues> WorkingValues = new List<SingleFrameRestrictionValues>();
        for (int i = 0; i < AllValues.Count; i++)
            if (AllValues[i].AtMotionState)
                WorkingValues.Add(AllValues[i]);
        return WorkingValues;
    }
    */
    private void Update()
    {
        MiddleStepCounts = GetMiddleStats(AllChangesList[(int)motionGet - 1].GetSingles());
        Output = GetOutputList(Input, MiddleStepCounts);
        ReInput = GetIndexFromList(MiddleStepCounts, Output);
    }
    public int TotalFramesToCheck(AllChanges changes)
    {
        List<AllChanges.SingleChange> SinglesList = changes.GetSingles();
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
    //105091227
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

    public AllChanges(List<float2> MinMax)
    {
        Motion = "";
        List<OneRestrictionChange> NewRestrictions = new List<OneRestrictionChange>();
        for (int i = 0; i < MinMax.Count; i++)
        {
            float Range = MinMax[i].y - MinMax[i].x;
            //float EachSide = Range / 2f;
            float EachSide = (Range * BruteForce.instance.MaxMinOfRangeStatOfRange) / 2f;
            OneRestrictionChange MainRestriction = new OneRestrictionChange();
            MainRestriction.Max = new SingleChange(MinMax[i].y + EachSide, MinMax[i].y - EachSide, 2, 0);
            MainRestriction.Min = new SingleChange(MinMax[i].x + EachSide, MinMax[i].x - EachSide, 2, 0);
            MainRestriction.MaxFalloff = new SingleChange(MinMax[i].y + EachSide, MinMax[i].y - EachSide, 2, 0);
            MainRestriction.MinFalloff = new SingleChange(MinMax[i].x + EachSide, MinMax[i].x - EachSide, 2, 0);
            MainRestriction.Weight = new SingleChange(0, 1, 2, 0);
            NewRestrictions.Add(MainRestriction);
        }
        this.Restrictions = NewRestrictions;
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
        public int GetTotalSteps() { return GuessingMax == GuessingMin ? 1 : MiddleSteps; }
        public float GetCurrentValue() { return Mathf.Lerp(GuessingMin, GuessingMax, ((float)CurrentStep + 1f) / (MiddleSteps + 1f)); }
        public float GetCurrentValueAt(int NewCurrentStep)
        {
            float LerpValue = 0;
            if (MiddleSteps % 2f == 0)
            {
                int EachSideTotal = (int)(MiddleSteps / 2);
                float Spacing = 0.5f * 1 / (EachSideTotal + 1);
                bool UpperSide = NewCurrentStep > EachSideTotal - 1;
                LerpValue = (UpperSide ? (NewCurrentStep + 2) : (NewCurrentStep + 1)) * Spacing;

            }
            else
            {
                int EachSideTotal = (int)(MiddleSteps - 1) / 2;
                float Spacing = 0.5f * 1 / (EachSideTotal + 1);
                LerpValue = (NewCurrentStep + 1) * Spacing;
            }
            return Mathf.Lerp(GuessingMin, GuessingMax, LerpValue);
        }
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


