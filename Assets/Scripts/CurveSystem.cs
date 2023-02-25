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
using System;
using System.Threading;
using Unity.Collections.LowLevel.Unsafe;
public class CurveSystem : SerializedMonoBehaviour
{
    public static CurveSystem instance;
    private void Awake() { instance = this; }
    [FoldoutGroup("Output"), Sirenix.OdinInspector.ReadOnly] public int CurrentIteration;
    public bool ShowInfos;


    [FoldoutGroup("Output"), ListDrawerSettings(ShowIndexLabels = true), ShowIf("ShowInfos")] private List<SingleFrameRestrictionValues> FrameInfo;
    [FoldoutGroup("Output"), ListDrawerSettings(ShowIndexLabels = true), ShowIf("ShowInfos")] private List<EachCurveInfo> AllFrameInfo;
    [FoldoutGroup("Output")] public List<AnimationCurve> RealCurves;
    [FoldoutGroup("Output")] private List<float2> Ranges;
    //[FoldoutGroup("Output")] public NativeCurveHolder CurveHolder;

    [FoldoutGroup("Input")] public int Keyframes = 15;
    [FoldoutGroup("Input"), Range(0, 1)] public float CurveConfidence;
    [FoldoutGroup("Input")] public float AlphaLearnRate;
    [FoldoutGroup("Input"), Range(0, 1)] public float StartingRange;
    [FoldoutGroup("Input")] public int EachSequence;
    [FoldoutGroup("Input")] public int PossibleValuePicks = 10;

    [FoldoutGroup("ActiveMultipliers")] public bool Incorrect;
    [FoldoutGroup("ActiveMultipliers")] public bool Height;
    [FoldoutGroup("ActiveMultipliers")] public bool Amount;
    [FoldoutGroup("ActiveMultipliers")] public bool SideWeight;

    public struct EachCurveInfo
    {
        public List<List<SingleFrameRestrictionValues>> Values;
        public EachCurveInfo(List<List<SingleFrameRestrictionValues>> Values) { this.Values = Values; }
    }

    [FoldoutGroup("Curve"), Button(ButtonSizes.Small)]
    public void ClearCurves() { Start(); }

    [FoldoutGroup("Curve"), Button(ButtonSizes.Small)]
    public void RunCurve() { BruteForceCurveState(); }
    [FoldoutGroup("Curve"), Button(ButtonSizes.Small)]
    public void GradientCurve() { GradientChangeCurve(); }

    [FoldoutGroup("Debug")] public List<int4> AllValues;
    [FoldoutGroup("Debug")] public List<float4> Ratios;
    [FoldoutGroup("Debug"), ListDrawerSettings(ShowIndexLabels = true, ShowItemCount = true, ShowPaging = true)] public List<List<float>> NewValues;
    [FoldoutGroup("Debug")] public List<float4> OutputStats;
    [FoldoutGroup("Debug")] public float LastPercentCorrect;
    [FoldoutGroup("Debug")] public float MotionStateBalencePercent;

    [FoldoutGroup("References")] public BruteForce BF;
    [FoldoutGroup("References")] public RestrictionManager RM;

    private void Start()
    {
        OutputStats = new List<float4>();
        CurrentIteration = 0;
        ///TryInitializeFrameInfo
        FrameInfo = BF.GetRestrictionsForMotions(BF.motionGet, RM.RestrictionSettings.MotionRestrictions[(int)BF.motionGet - 1]);
        Ranges = BF.GetRangeOfMinMaxValues(FrameInfo); //to be removed eventually
        for (int i = 0; i < FrameInfo.Count; i++)
            for (int j = 0; j < FrameInfo[0].OutputRestrictions.Count; j++)
                FrameInfo[i].OutputRestrictions[j] = 0f + (FrameInfo[i].OutputRestrictions[j] - Ranges[j].x) * (1f - 0f) / (Ranges[j].y - Ranges[j].x);

        ///TryInitializeAllFrameInfo
        AllFrameInfo = new List<EachCurveInfo>();
        for (int i = 0; i < FrameInfo[0].OutputRestrictions.Count; i++)
        {
            List<List<SingleFrameRestrictionValues>> Infos = new List<List<SingleFrameRestrictionValues>>();
            for (int j = 0; j < Keyframes; j++) // REFERS TO ALL SPACES
            {
                List<SingleFrameRestrictionValues> OneSectionInfos = new List<SingleFrameRestrictionValues>();
                float2 RemappedValue = new float2((j == 0f ? 0f : j - 1f), j == (Keyframes - 1f) ? j : j + 1f) / (float)(Keyframes - 1f);

                for (int k = 0; k < FrameInfo.Count; k++)//check all frames
                    if (FrameInfo[k].OutputRestrictions[i] > RemappedValue.x && FrameInfo[k].OutputRestrictions[i] < RemappedValue.y)
                        OneSectionInfos.Add(FrameInfo[k]);

                Infos.Add(OneSectionInfos);
            }
            AllFrameInfo.Add(new EachCurveInfo(Infos));
        }

        ///TryInitializeCurves
        RealCurves = new List<AnimationCurve>();
        for (int i = 0; i < FrameInfo[0].OutputRestrictions.Count; i++)
        {
            RealCurves.Add(new AnimationCurve());
            for (int j = 0; j < Keyframes; j++)
            {
                Keyframe key = new Keyframe(j / (float)(Keyframes - 1), StartingRange);
                key.inWeight = 0f;
                key.outWeight = 0f;
                key.inTangent = 0f;
                key.outTangent = 0f;
                RealCurves[i].AddKey(key);
            }


        }
        SpreadSheet.instance.PrintMotionStats(FrameInfo);
    }
    



    #region BruteForce
    [BurstCompile(CompileSynchronously = true)]
    private struct LearnAnimationCurve : IJobParallelFor
    {
        [NativeDisableParallelForRestriction] public NativeReference<float> MaxValue;
        [NativeDisableParallelForRestriction] public NativeReference<int> MaxIndex;
        //[NativeDisableParallelForRestriction] public NativeArray<int4> AllValues;

        [Unity.Collections.ReadOnly, DeallocateOnJobCompletion] public NativeArray<bool> States;
        [Unity.Collections.ReadOnly, DeallocateOnJobCompletion] public NativeArray<float> FlatRawValues;

        [Unity.Collections.ReadOnly, DeallocateOnJobCompletion] public NativeArray<float> AllCurveValues;

        public int PossibleValuePicks;
        public float Confidence;


        public void Execute(int Index)
        {
            float LerpValue(int PossibleValuePicks, int CurrentStep)
            {

                if (PossibleValuePicks % 2f == 0)
                {
                    int EachSideTotal = (PossibleValuePicks / 2);
                    float Spacing = 0.5f / (float)(EachSideTotal + 1);
                    bool UpperSide = CurrentStep > EachSideTotal - 1;
                    return (UpperSide ? (float)(CurrentStep + 2) : (float)(CurrentStep + 1)) * Spacing;

                }
                else
                {
                    int EachSideTotal = (PossibleValuePicks - 1) / 2;
                    float Spacing = 0.5f / (float)(EachSideTotal + 1);
                    return (float)(CurrentStep + 1) * Spacing;
                }
            }

            int CurveCount = FlatRawValues.Length / States.Length;
            int EachKeyframes = AllCurveValues.Length / CurveCount;
            NativeArray<float> RealCurveValues = new NativeArray<float>(AllCurveValues.Length, Allocator.Temp);
            int LeftCount = Index;
            //get each value
            for (int i = 0; i < RealCurveValues.Length; i++)
            {
                int Reduce = (int)Mathf.Pow(PossibleValuePicks, (RealCurveValues.Length - i) - 1);
                int ReturnedIndex = Mathf.FloorToInt(LeftCount / Reduce);
                LeftCount -= ReturnedIndex * Reduce;
                RealCurveValues[i] = Mathf.Lerp(AllCurveValues[i] - Confidence, AllCurveValues[i] + Confidence, LerpValue(PossibleValuePicks, ReturnedIndex));
            }

            //Set Highest deny values
            float PreviousBest = MaxValue.Value;
            int TotalGuesses = States.Length;
            int MaxWrongGuessesPrevious = (TotalGuesses - Mathf.CeilToInt(TotalGuesses * PreviousBest)); //10

            //
            int4 Guesses = int4.zero;
            for (int i = 0; i < States.Length; i++) // all infos
            {
                float TotalWeightValue = 0f;
                for (int j = 0; j < CurveCount; j++)//all values in each info
                {
                    float it = FlatRawValues[(i * CurveCount) + j] * (EachKeyframes - 1);
                    int lower = (int)it;
                    int upper = lower + 1;
                    if (upper >= EachKeyframes)
                        upper = EachKeyframes - 1;

                    TotalWeightValue += Mathf.Lerp(RealCurveValues[lower], RealCurveValues[upper], it - lower);
                }
                bool Correct = (TotalWeightValue >= 1) == States[i];
                Guesses = new int4(
                    Guesses.w + ((Correct && States[i]) ? 1 : 0),
                    Guesses.x + ((!Correct && States[i]) ? 1 : 0),
                    Guesses.y + ((Correct && !States[i]) ? 1 : 0),
                    Guesses.z + ((!Correct && !States[i]) ? 1 : 0));

                //CHECK HERE
                if (Guesses.x + Guesses.z > MaxWrongGuessesPrevious)
                    return;
            }
            //UPDATE HERE
            float NewValue = (Guesses.w + Guesses.y) / (Guesses.x + Guesses.z);
            float currentHighest = MaxValue.Value;

            while (NewValue > currentHighest)
            {
                float newHighest = NewValue;
                float previousValue = currentHighest;
                currentHighest = MaxValue.Value;
                if (previousValue == currentHighest)
                {
                    // successfully updated the value
                    MaxValue.Value = newHighest;
                    MaxIndex.Value = Index;
                    break;
                }
            }

            //Debug.Log("False: " + FalseTrue.x + " True: " + FalseTrue.y);
            RealCurveValues.Dispose();
            //AllValues[Index] = Guesses;
        }
    }
    
    public void BruteForceCurveState()
    {
        float StartTime = Time.realtimeSinceStartup;
        for (int s = 0; s < EachSequence; s++)
        {
            float Confidence = Mathf.Pow(CurveConfidence, CurrentIteration) / 2f;
            int EachCurveCount = (int)Mathf.Pow(PossibleValuePicks, Keyframes);
            int AllRuns = (int)Mathf.Pow(PossibleValuePicks, Keyframes * RealCurves.Count);

            NativeArray<float> KeyframePoints = new NativeArray<float>(RealCurves.Count * Keyframes, Allocator.TempJob);
            for (int i = 0; i < RealCurves.Count; i++)
                for (int j = 0; j < Keyframes; j++)
                    KeyframePoints[(i * Keyframes) + j] = RealCurves[i].keys[j].value;


            LearnAnimationCurve CurveRun = new LearnAnimationCurve
            {
                AllCurveValues = KeyframePoints,
                MaxValue = new NativeReference<float>(0, Allocator.TempJob),
                MaxIndex = new NativeReference<int>(0, Allocator.TempJob),
                PossibleValuePicks = PossibleValuePicks,
                //AllValues = new NativeArray<int4>(AllRuns, Allocator.TempJob),
                States = BruteForce.instance.GetStatesStat(FrameInfo),
                FlatRawValues = BruteForce.instance.GetFlatRawStat(FrameInfo),
                Confidence = Confidence,
            };

            JobHandle jobHandle = CurveRun.Schedule(AllRuns, 1);
            jobHandle.Complete();

            ResetDebug();

            float MaxValue = CurveRun.MaxValue.Value;
            int IndexValue = CurveRun.MaxIndex.Value;

            //CurveRun.AllValues.Dispose();
            CurveRun.MaxIndex.Dispose();
            CurveRun.MaxValue.Dispose();

            /*
            for (int i = 0; i < CurveRun.AllValues.Length; i++)
            {
                //AllCurves.Add(AnimationCurveByIndex(j, i));
                int4 Val = CurveRun.AllValues[i];
                //AllValues.Add(Val);

                float Total = Val.w + Val.x + Val.y + Val.z;

                float Value = (Val.w + Val.y) / Total;


                //float TrueRatio = (Val.w + Val.x) / Total;
                //float FalseRatio = (Val.y + Val.z) / Total;

                //float CorrectRatio = (Val.w + Val.y) / Total;
                //float IncorrectRatio = (Val.x + Val.z) / Total;
                //Ratios.Add(new float4(TrueRatio, FalseRatio, CorrectRatio, IncorrectRatio));
                //Debug.Log("Value: " + Value);
                if (Value > MaxValue)
                {
                    Debug.Log("Curve: " + i + "  MaxValue: " + MaxValue + "IndexFound: " + IndexFound);
                    MaxValue = Value;
                    IndexFound = i;
                }

            }
            */
            List<AnimationCurve> NewCurves = new List<AnimationCurve>();
            for (int i = 0; i < RealCurves.Count; i++)
                NewCurves.Add(new AnimationCurve());

            int LeftCount = IndexValue;
            for (int i = 0; i < RealCurves.Count; i++)
            {
                for (int j = 0; j < Keyframes; j++)
                {
                    int Reduce = (int)Mathf.Pow(PossibleValuePicks, (Keyframes - j) - 1);
                    int ReturnedIndex = Mathf.FloorToInt(LeftCount / Reduce);
                    LeftCount -= ReturnedIndex * Reduce;
                    NewCurves[i].AddKey(new Keyframe(RealCurves[i].keys[j].time, Mathf.Lerp(RealCurves[i].keys[j].value - Confidence, RealCurves[i].keys[j].value + Confidence, LerpValue(ReturnedIndex))));
                }

            }

            

            CurrentIteration += 1;
            Debug.Log("Frames: " + AllRuns + " in: " + (Time.realtimeSinceStartup - StartTime).ToString("F5") + " Seconds");
            Debug.Log("Accuracy: " + (MaxValue * 100).ToString("F5"));
        }

        float LerpValue(int CurrentStep)
        {

            if (PossibleValuePicks % 2f == 0)
            {
                int EachSideTotal = (PossibleValuePicks / 2);
                float Spacing = 0.5f / (float)(EachSideTotal + 1);
                bool UpperSide = CurrentStep > EachSideTotal - 1;
                return (UpperSide ? (float)(CurrentStep + 2) : (float)(CurrentStep + 1)) * Spacing;

            }
            else
            {
                int EachSideTotal = (PossibleValuePicks - 1) / 2;
                float Spacing = 0.5f / (float)(EachSideTotal + 1);
                return (float)(CurrentStep + 1) * Spacing;
            }
        }
    }
    #endregion

    #region OLD
    public void ResetDebug()
    {

        AllValues = new List<int4>();
        Ratios = new List<float4>();
    }


    public struct FrameStat
    {
        public List<AnimationCurve> Curves;
        public float4 Val;
        public List<float> AverageDifferences;
        public float2 MotionStateTrueFalse;
        public FrameStat(List<AnimationCurve> Curves, List<SingleFrameRestrictionValues> MotionValues)
        {
            this.Curves = Curves;
            Val = int4.zero;
            MotionStateTrueFalse = float2.zero;

            AverageDifferences = new List<float>();
            for (int i = 0; i < Curves.Count; i++)
                AverageDifferences.Add(0f);

            for (int i = 0; i < MotionValues.Count; i++)
            {
                MotionStateTrueFalse = new float2(Val.x + (!MotionValues[i].AtMotionState ? 1f : 0f), Val.y + (MotionValues[i].AtMotionState ? 1f : 0f));
                float TotalCheckValue = 0f;
                for (int j = 0; j < Curves.Count; j++)
                {
                    var count = Curves[j].keys.Length;
                    var it = MotionValues[i].OutputRestrictions[j] * (count - 1);

                    int lower = (int)it;
                    int upper = lower + 1;
                    if (upper >= count)
                        upper = count - 1;


                    TotalCheckValue += Mathf.Lerp(Curves[j].keys[lower].value, Curves[j].keys[upper].value, it - lower);
                    //TotalCheckValue += Curves[j].Evaluate(MotionValues[i].OutputRestrictions[j]);
                    AverageDifferences[j] += MotionValues[i].OutputRestrictions[j];
                }

                bool IsHigh = TotalCheckValue > 1f;
                bool Correct = MotionValues[i].AtMotionState == IsHigh;

                Val = new float4(
                        Val.w + ((Correct && IsHigh) ? 1f : 0f),
                        Val.x + ((!Correct && IsHigh) ? 1f : 0f),
                        Val.y + ((Correct && !IsHigh) ? 1f : 0f),
                        Val.z + ((!Correct && !IsHigh) ? 1f : 0f));
            }
            for (int i = 0; i < AverageDifferences.Count; i++)
                AverageDifferences[i] = AverageDifferences[i] / (float)MotionValues.Count;
            //float ValueAverage = Total / MotionValues.Count;
        }
        public float AverageMotionWeight(int Curve, int Index) { return 1f - Mathf.Abs(AverageDifferences[Curve] - ((float)Index / (float)(CurveSystem.instance.Keyframes - 1f))); }
        public float Total() { return Val.w + Val.x + Val.y + Val.z; }

        public float CorrectHigh() { return Val.w; }
        public float IncorrectHigh() { return Val.x; }
        public float CorrectLow() { return Val.y; }
        public float IncorrectLow() { return Val.z; }

        public float CorrectPercent() { return (Val.w + Val.y) / Total(); }
        public float InCorrectPercent() { return (Val.x + Val.z) / Total(); }
        public float HighPercent() { return (Val.w + Val.x) / Total(); }
        public float LowPercent() { return (Val.y + Val.z) / Total(); }

        public float CorrectCount() { return (int)(Val.w + Val.y); }
        public float InCorrectCount() { return (int)(Val.x + Val.z); }
        public float HighCount() { return (Val.w + Val.x); }
        public float LowCount() { return (Val.y + Val.z); }
    }
    public void GradientChangeCurve()
    {
        for (int s = 0; s < EachSequence; s++)
        {

            float Range = Mathf.Pow(CurveConfidence, CurrentIteration) / 2f;
            List<List<float>> ToChange = new List<List<float>>();
            for (int i = 0; i < RealCurves.Count; i++)
            {
                ToChange.Add(new List<float>());
                for (int j = 0; j < Keyframes; j++)
                {
                    if (AllFrameInfo[i].Values[j].Count == 0)
                        continue;
                    FrameStat Stat = new FrameStat(RealCurves, AllFrameInfo[i].Values[j]);
                    if (Stat.InCorrectCount() == 0)
                        continue;

                    float Max = RealCurves[i].keys[j].value + Range;
                    float Min = RealCurves[i].keys[j].value - Range;

                    float HighestPercent = 0f;
                    float Outputvalue = 0.5f;
                    for (int k = 0; k < PossibleValuePicks; k++)
                    {
                        float Percent = TestIf(i, j, Mathf.Lerp(Min, Max, k / ((float)PossibleValuePicks - 1)));
                        if (Percent > HighestPercent)
                        {
                            Outputvalue = k / ((float)PossibleValuePicks - 1);
                            HighestPercent = Percent;
                        }

                    }
                    Debug.Log("IJ: " + i + " " + j + "  HighestPercent: " + HighestPercent + "  Outputvalue: " + Outputvalue);

                    /*
                    float IncorrectMultiplier = Stat.InCorrectCount() / Stat.Total();
                    float HeightMultiplier = (Stat.IncorrectHigh() - Stat.IncorrectLow()) / Stat.InCorrectCount();
                    float AmountMultiplier = 1f / Stat.Total();
                    float SideWeightMultiplier = Stat.AverageMotionWeight(i, j);
                    float ChangeAmount = AlphaLearnRate;
                    if (Incorrect)
                        ChangeAmount = ChangeAmount * IncorrectMultiplier;
                    if (Height)
                        ChangeAmount = ChangeAmount * HeightMultiplier;
                    if (Amount)
                        ChangeAmount = ChangeAmount * AmountMultiplier;
                    if (SideWeight)
                        ChangeAmount = ChangeAmount * SideWeightMultiplier;
                    
                    if ((s + i == 0 && j == 0) || true)
                    {
                        Debug.Log("IJ: " + i + " " + j + "  ChangeAmount: " + ChangeAmount + "  Incorrect%: " + IncorrectMultiplier + "  Height%: " + HeightMultiplier + "  IncorrectHigh: " + Stat.IncorrectHigh() + "  IncorrectLow: " + Stat.IncorrectLow() + "  SideWeightMultiplier: " + SideWeightMultiplier);
                        //Debug.Log("Height: " + HeightMultiplier + "  IncorrectHigh: " + Stat.IncorrectHigh() + "  IncorrectLow: " + Stat.IncorrectLow() + "  InCorrectCount: " + Stat.InCorrectCount());
                        //Debug.Log("Cruve: " + i + "  KeyFrame: " + j + "  SideWeightMultiplier: " + SideWeightMultiplier + "  AverageDifferences: " + Stat.AverageDifferences[i]);
                    }
                    */


                    float NewValue = Outputvalue;
                    ToChange[i].Add(NewValue);
                    //Debug.Log("ChangeAmount: " + ChangeAmount);
                    Keyframe key = new Keyframe(RealCurves[i].keys[j].time, NewValue);
                    key.inWeight = 0f;
                    key.outWeight = 0f;
                    key.inTangent = 0f;
                    key.outTangent = 0f;
                    Keyframe[] NewKeyframes = RealCurves[i].keys;
                    NewKeyframes[j] = key;
                    RealCurves[i] = new AnimationCurve(NewKeyframes);

                    for (int p = 0; p < RealCurves.Count; p++)
                        for (int l = 0; l < RealCurves.Count; l++)
                        {

                        }
                    //RealCurves[p].keys[l].weightedMode = WeightedMode.None;

                    //Debug.Log("");
                    //return;
                }


            }
            NewValues = ToChange;
            CurrentIteration += 1;
        }

        FrameStat FinalInfo = new FrameStat(RealCurves, FrameInfo);
        LastPercentCorrect = FinalInfo.CorrectPercent();
        MotionStateBalencePercent = FinalInfo.MotionStateTrueFalse.y / (FinalInfo.MotionStateTrueFalse.x + FinalInfo.MotionStateTrueFalse.y);
        OutputStats.Add(new FrameStat(RealCurves, FrameInfo).Val);

        
        //Debug.Log("Iteration: " + CurrentIteration + "  Correct: " + new FrameStat(RealCurves, FrameInfo).CorrectPercent());
    }
    public float TestIf(int Curve, int Keyframe, float Value)
    {
        //Set Curve
        Keyframe[] NewKeyframes = RealCurves[Curve].keys;
        NewKeyframes[Keyframe] = new Keyframe(RealCurves[Curve].keys[Keyframe].time, Value);
        List<AnimationCurve> Curves = new List<AnimationCurve>(RealCurves);
        Curves[Curve] = new AnimationCurve(NewKeyframes);

        //Test
        FrameStat Stat = new FrameStat(Curves, AllFrameInfo[Curve].Values[Keyframe]);
        return Stat.CorrectPercent();
    }
    #endregion
}

/*
[FoldoutGroup("Debug")] public int TestInput;
[FoldoutGroup("Debug"), Sirenix.OdinInspector.ReadOnly] public float Output;
[FoldoutGroup("Debug"), Sirenix.OdinInspector.ReadOnly] public int2 BoarderIndexs;
[FoldoutGroup("Debug"), Sirenix.OdinInspector.ReadOnly] public float2 Range;

[FoldoutGroup("Curve"), Button(ButtonSizes.Small)]
public void TestRange()
{
    FrameInfo = BF.GetRestrictionsForMotions(BF.motionGet, RM.RestrictionSettings.MotionRestrictions[(int)BF.motionGet - 1]);
    float2 TrueRange = BF.GetRangeOfMinMaxValues(FrameInfo)[0];
    BoarderIndexs = new int2(TestInput == 0 ? 0 : TestInput - 1, TestInput == (NumberPerMotion - 1) ? TestInput : TestInput + 1);
    Range = new float2(GetFrameRange(TrueRange, BoarderIndexs.x), GetFrameRange(TrueRange, BoarderIndexs.y));
}
*/