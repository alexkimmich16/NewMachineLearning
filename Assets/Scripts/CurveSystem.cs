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
using Unity.Collections.LowLevel.Unsafe;
public class CurveSystem : SerializedMonoBehaviour
{
    public static CurveSystem instance;
    private void Awake() { instance = this; }
    [FoldoutGroup("Output"), Sirenix.OdinInspector.ReadOnly] public int CurrentIteration;
    [FoldoutGroup("Output"), ListDrawerSettings(ShowIndexLabels = true)] private List<SingleFrameRestrictionValues> FrameInfo;
    [FoldoutGroup("Output"), ListDrawerSettings(ShowIndexLabels = true)] private List<EachCurveInfo> AllFrameInfo;
    [FoldoutGroup("Output")] public List<AnimationCurve> RealCurves;
    //[FoldoutGroup("Output")] public NativeCurveHolder CurveHolder;

    [FoldoutGroup("Input")] public int NumberPerMotion = 15;
    [FoldoutGroup("Input"), Range(0, 1)] private float CurveConfidence;
    [FoldoutGroup("Input")] public float AlphaLearnRate;
    [FoldoutGroup("Input")] private int Resolution = 200;
    [FoldoutGroup("Input"), Range(0, 1)] public float StartingRange;
    [FoldoutGroup("Input")] public int EachSequence;
    [FoldoutGroup("Input")] public int CallCount;
    [FoldoutGroup("Input")] public bool UseDistance;

    public struct EachCurveInfo
    {
        public List<List<SingleFrameRestrictionValues>> Values;
        public EachCurveInfo(List<List<SingleFrameRestrictionValues>> Values) { this.Values = Values; }
    }

    [FoldoutGroup("Curve"), Button(ButtonSizes.Small)]
    public void ClearCurves() { RealCurves.Clear(); }

    //[FoldoutGroup("Curve"), Button(ButtonSizes.Small)]
    //public void RunCurve() { NextCurveState(); }
    [FoldoutGroup("Curve"), Button(ButtonSizes.Small)]
    public void GradientCurve() { GradientChangeCurve(); }

    [FoldoutGroup("Debug"), Sirenix.OdinInspector.ReadOnly] public int CostFunction;
    [FoldoutGroup("Debug")] public List<float> Origional;
    [FoldoutGroup("Debug")] public List<float> Adjusted;
    [FoldoutGroup("Debug")] public List<int4> AllValues;
    [FoldoutGroup("Debug")] public List<float4> Ratios;
    [FoldoutGroup("Debug")] public List<List<float>> NewValues;
    [FoldoutGroup("Debug")] public List<float4> OutputStats;


    [FoldoutGroup("References")] public BruteForce BF;
    [FoldoutGroup("References")] public RestrictionManager RM;

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


    public void ResetDebug()
    {
        Origional = new List<float>();
        Adjusted = new List<float>();
        AllValues = new List<int4>();
        Ratios = new List<float4>();
    }
    public void GradientChangeCurve()
    {
        TryInitializeFrameData();
        TryInitializeCurves();
        for (int c = 0; c < CallCount; c++)
        {
            for (int s = 0; s < EachSequence; s++)
            {
                List<List<float>> ToChange = new List<List<float>>();
                for (int i = 0; i < RealCurves.Count; i++)
                {
                    ToChange.Add(new List<float>());
                    for (int j = 0; j < NumberPerMotion; j++)
                    {
                        FrameStat CheckStats = new FrameStat(RealCurves, AllFrameInfo[i].Values[j]);
                        float CorrectAmount = CheckStats.CorrectPercent();
                        float IncorrectAdjust = CheckStats.IncorrectHighAdjust();
                        if(c + s + i == 0)
                        {
                            Debug.Log("CorrectAmount: " + CorrectAmount + "  IncorrectAdjust: " + IncorrectAdjust + "  InTrue: " + CheckStats.Val.x + "  InFalse: " + CheckStats.Val.z);
                        }
                        float NewValue = RealCurves[i].keys[j].value + AlphaLearnRate * (1f / CheckStats.Total());
                        ToChange[i].Add(NewValue);

                        //float y_predicted = theta0 + theta1 * x;

                        //float J = (1 / (2 * m)) * sum((y_predicted - y_actual) ^ 2)
                    }


                }
                NewValues = ToChange;

                for (int i = 0; i < ToChange.Count; i++)
                {
                    Keyframe[] ks = new Keyframe[ToChange[i].Count];
                    for (int j = 0; j < ToChange[i].Count; j++)
                    {
                        ks[j] = new Keyframe(RealCurves[i].keys[j].time, ToChange[i][j]);
                        //RealCurves[i].keys[j] = new Keyframe(RealCurves[i].keys[j].time, ToChange[i][j]);
                    }
                    RealCurves[i] = new AnimationCurve(ks);
                }
            }
            OutputStats.Add(new FrameStat(RealCurves, FrameInfo).Val);
            //Debug.Log("Iteration: " + CurrentIteration + "  Correct: " + new FrameStat(RealCurves, FrameInfo).CorrectPercent());
            CurrentIteration += 1;
        }
    }
    

    public struct FrameStat
    {
        public List<AnimationCurve> Curves;
        public float4 Val;
        public List<float> Averages;
        public FrameStat(List<AnimationCurve> Curves, List<SingleFrameRestrictionValues> MotionValues)
        {
            this.Curves = Curves;

            Val = int4.zero;
            List<float2> Totals = new List<float2>(); //weight, 
            for (int i = 0; i < Curves.Count; i++)
                Totals.Add(float2.zero);

            for (int i = 0; i < MotionValues.Count; i++)
            {
                float TotalCheckValue = 0f;
                for (int j = 0; j < Curves.Count; j++)
                {
                    TotalCheckValue += Curves[j].Evaluate(MotionValues[i].OutputRestrictions[j]);
                    Totals[j] = new float2();
                }
                    
                bool MotionState = MotionValues[i].AtMotionState;
                bool Correct = (TotalCheckValue > 1) == MotionState;


                ///potential for incorrect difference
                Val = new float4(
                        Val.w + ((Correct && MotionState) ? 1 : 0),
                        Val.x + ((!Correct && MotionState) ? 1 : 0),
                        Val.y + ((Correct && !MotionState) ? 1 : 0),
                        Val.z + ((!Correct && !MotionState) ? 1 : 0));
            }

            Averages = new List<float>();
            for (int i = 0; i < Curves.Count; i++)
                Averages.Add(0);

            //float ValueAverage = Total / MotionValues.Count;
        }
        public float CorrectPercent() { return (Val.w + Val.y) / Total(); }
        public float IncorrectHighAdjust() { return (Val.x - Val.z) / (Val.x + Val.z); }


        //x(ontrue) = 8
        //z(onfalse) = 4
        //positive

        //x(ontrue) = 4
        //z(onfalse) = 8
        //negitive

        //negitive if z > x
        public float Total() { return Val.w + Val.x + Val.y + Val.z; }

    }


    #region Initialize
    [FoldoutGroup("Curve"), Button(ButtonSizes.Small)]
    public void TryInitializeFrameData()
    {
        TryInitializeFrameInfo();
        TryInitializeAllFrameInfo();

        void TryInitializeAllFrameInfo()
        {
            if (AllFrameInfo != null)
                if (AllFrameInfo.Count != 0)
                    return;

            AllFrameInfo = new List<EachCurveInfo>();
            List<float2> Ranges = BF.GetRangeOfMinMaxValues(FrameInfo);
            for (int i = 0; i < FrameInfo[0].OutputRestrictions.Count; i++)
            {
                List<List<SingleFrameRestrictionValues>> Infos = new List<List<SingleFrameRestrictionValues>>();
                for (int j = 0; j < NumberPerMotion; j++) // REFERS TO ALL SPACES
                {
                    List<SingleFrameRestrictionValues> OneSectionInfos = new List<SingleFrameRestrictionValues>();

                    int2 BoarderIndexs = new int2(j == 0 ? 0 : j - 1, j == (NumberPerMotion - 1) ? j : j + 1);
                    float2 MinMax = new float2(GetFrameRange(Ranges[i], BoarderIndexs.x), GetFrameRange(Ranges[i], BoarderIndexs.y));

                    for (int k = 0; k < FrameInfo.Count; k++)//check all frames
                        if (FrameInfo[k].OutputRestrictions[i] > MinMax.x && FrameInfo[k].OutputRestrictions[i] < MinMax.y)
                            OneSectionInfos.Add(FrameInfo[k]);

                    Infos.Add(OneSectionInfos);
                }
                AllFrameInfo.Add(new EachCurveInfo(Infos));
            }
        }
        void TryInitializeFrameInfo()
        {
            if (FrameInfo != null)
                if (FrameInfo.Count != 0)
                    return;
            FrameInfo = BF.GetRestrictionsForMotions(BF.motionGet, RM.RestrictionSettings.MotionRestrictions[(int)BF.motionGet - 1]);
        }
    }
    public void TryInitializeCurves()
    {
        if (RealCurves != null)
            if (RealCurves.Count != 0)
                return;

        List<float2> Ranges = BF.GetRangeOfMinMaxValues(FrameInfo);
        for (int i = 0; i < FrameInfo[0].OutputRestrictions.Count; i++)
        {
            RealCurves.Add(new AnimationCurve());
            for (int j = 0; j < NumberPerMotion; j++)
            {
                RealCurves[i].AddKey(GetFrameRange(Ranges[i], j), StartingRange);
            }
        }
    }
    #endregion
    #region Old
    public struct NativeCurveHolder
    {
        public int preWrapMode;
        public int postWrapMode;
        [NativeDisableContainerSafetyRestriction] public float[][] OrigionalCurves;
        [NativeDisableContainerSafetyRestriction] public float[][] AdjustedCurves;

        public NativeCurveHolder(int preWrapMode, int postWrapMode, float[][] OrigionalValues, float[][] AdjustedValues)
        {
            this.preWrapMode = preWrapMode;
            this.postWrapMode = postWrapMode;
            this.OrigionalCurves = OrigionalValues;
            this.AdjustedCurves = AdjustedValues;
        }
        public float Evaluate(float[] Values, float t)
        {
            var count = Values.Length;

            if (count == 1)
                return Values[0];

            if (t < 0f)
            {
                switch (preWrapMode)
                {
                    default:
                        return Values[0];
                    case 2:
                        t = 1f - (Mathf.Abs(t) % 1f);
                        break;
                    case 4:
                        t = pingpong(t, 1f);
                        break;
                }
            }
            else if (t > 1f)
            {
                switch (postWrapMode)
                {
                    default:
                        return Values[count - 1];
                    case 2:
                        t %= 1f;
                        break;
                    case 4:
                        t = pingpong(t, 1f);
                        break;
                }
            }

            var it = t * (count - 1);

            var lower = (int)it;
            var upper = lower + 1;
            if (upper >= count)
                upper = count - 1;

            return Mathf.Lerp(Values[lower], Values[upper], it - lower);
        }

        private float repeat(float t, float length)
        {
            return Mathf.Clamp(t - Mathf.Floor(t / length) * length, 0, length);
        }

        private float pingpong(float t, float length)
        {
            t = repeat(t, length * 2f);
            return length - Mathf.Abs(t - length);
        }
    }
    [BurstCompile(CompileSynchronously = true)]
    private struct LearnAnimationCurve : IJobParallelFor
    {
        [NativeDisableParallelForRestriction] public NativeArray<int4> AllValues;

        [Unity.Collections.ReadOnly] public int2 WrapModes;

        [Unity.Collections.ReadOnly] public NativeArray<bool> States;
        [Unity.Collections.ReadOnly] public NativeArray<float> FlatRawValues;

        [Unity.Collections.ReadOnly] public NativeArray<float> OrigionalCurveValues;
        [Unity.Collections.ReadOnly] public NativeArray<float> AdjustedCurvesValues;

        [DeallocateOnJobCompletion, Unity.Collections.ReadOnly] public NativeArray<int> WeightedMiddleSteps;

        public int RunsPerCurve;
        public int Resolution;


        public void Execute(int Index)
        {
            NativeArray<int> CurrentValues = new NativeArray<int>(WeightedMiddleSteps.Length, Allocator.Temp);
            int Section = Mathf.FloorToInt(Index / RunsPerCurve);

            int4 Guesses = int4.zero; // w = correct on true, x=  incorrect on true, y = correct on false, z =incorrect on false
            for (int i = 0; i < FlatRawValues.Length / (OrigionalCurveValues.Length / Resolution); i++) // all motions
            {
                float TotalWeightValue = 0f;
                for (int j = 0; j < OrigionalCurveValues.Length / Resolution; j++)
                {
                    bool UseOrigional = Section != i;

                    NativeArray<float> SingleCurve = new NativeArray<float>(Resolution, Allocator.Temp);
                    for (int k = 0; k < Resolution; k++)
                        SingleCurve[k] = UseOrigional ? OrigionalCurveValues[Section * Resolution + k] : AdjustedCurvesValues[Index * Resolution + k];


                    TotalWeightValue += Evaluate(SingleCurve, FlatRawValues[(i * (OrigionalCurveValues.Length / Resolution)) + j], WrapModes);
                }
                bool Correct = (TotalWeightValue >= 1) == States[i];
                Guesses = new int4(
                    Guesses.w + ((Correct && States[i]) ? 1 : 0),
                    Guesses.x + ((!Correct && States[i]) ? 1 : 0),
                    Guesses.y + ((Correct && !States[i]) ? 1 : 0),
                    Guesses.z + ((!Correct && !States[i]) ? 1 : 0));
            }
            //Debug.Log("False: " + FalseTrue.x + " True: " + FalseTrue.y);

            AllValues[Index] = Guesses;
            CurrentValues.Dispose();

            float Evaluate(NativeArray<float> CurvesList, float t, int2 Wraps)
            {
                var count = CurvesList.Length;

                if (count == 1)
                    return CurvesList[0];

                if (t < 0f)
                {
                    switch (Wraps.x)
                    {
                        default:
                            return CurvesList[0];
                        case 2:
                            t = 1f - (Mathf.Abs(t) % 1f);
                            break;
                        case 4:
                            t = pingpong(t, 1f);
                            break;
                    }
                }
                else if (t > 1f)
                {
                    switch (Wraps.y)
                    {
                        default:
                            return CurvesList[count - 1];
                        case 2:
                            t %= 1f;
                            break;
                        case 4:
                            t = pingpong(t, 1f);
                            break;
                    }
                }

                var it = t * (count - 1);

                var lower = (int)it;
                var upper = lower + 1;
                if (upper >= count)
                    upper = count - 1;

                return Mathf.Lerp(CurvesList[lower], CurvesList[upper], it - lower);

                float repeat(float t, float length)
                {
                    return Mathf.Clamp(t - Mathf.Floor(t / length) * length, 0, length);
                }

                float pingpong(float t, float length)
                {
                    t = repeat(t, length * 2f);
                    return length - Mathf.Abs(t - length);
                }
            }
        }
    }
    public float GetFrameRange(float2 Range, int Index) { return Mathf.Lerp(Range.x, Range.y, Index / (float)(NumberPerMotion - 1)); }
    public void NextCurveState()
    {
        for (int s = 0; s < EachSequence; s++)
        {
            if (FrameInfo.Count == 0)
                FrameInfo = BruteForce.instance.GetRestrictionsForMotions(BruteForce.instance.motionGet, RestrictionManager.instance.RestrictionSettings.MotionRestrictions[(int)BruteForce.instance.motionGet - 1]);

            TryInitializeCurves();

            int EachCurveCount = (int)Mathf.Pow(2, NumberPerMotion);
            List<int> EachMiddle = new List<int>();
            for (int i = 0; i < NumberPerMotion; i++)
                EachMiddle.Add(1);
            for (int i = 0; i < NumberPerMotion; i++)
                for (int j = 0; j < i; j++)
                    EachMiddle[j] = EachMiddle[j] * 2;

            Origional = new List<float>();
            NativeArray<float> OrigionalCurves = new NativeArray<float>(RealCurves.Count * Resolution, Allocator.TempJob);
            for (int i = 0; i < RealCurves.Count; i++)
                for (int j = 0; j < Resolution; j++)
                {
                    Origional.Add(RealCurves[i].Evaluate((float)j / (float)Resolution));
                    OrigionalCurves[(i * Resolution) + j] = RealCurves[i].Evaluate((float)j / (float)Resolution);
                }

            Adjusted = new List<float>();
            NativeArray<float> AdjustedCurves = new NativeArray<float>(RealCurves.Count * EachCurveCount * Resolution, Allocator.TempJob);
            for (int i = 0; i < RealCurves.Count; i++)
                for (int j = 0; j < EachCurveCount; j++)
                    for (int k = 0; k < Resolution; k++)
                    {
                        AdjustedCurves[(j * EachCurveCount) + (EachCurveCount * i) + k] = AnimationCurveByIndex(j, i).Evaluate((float)k / (float)Resolution);
                        Adjusted.Add(AnimationCurveByIndex(j, i).Evaluate((float)k / (float)Resolution));
                    }


            LearnAnimationCurve CurveRun = new LearnAnimationCurve
            {
                OrigionalCurveValues = OrigionalCurves,
                AdjustedCurvesValues = AdjustedCurves,
                RunsPerCurve = EachCurveCount,
                AllValues = new NativeArray<int4>(EachCurveCount * RealCurves.Count, Allocator.TempJob),
                States = BruteForce.instance.GetStatesStat(),
                FlatRawValues = BruteForce.instance.GetFlatRawStat(),
                WeightedMiddleSteps = new NativeArray<int>(EachMiddle.ToArray(), Allocator.TempJob),
                Resolution = Resolution,
                WrapModes = new int2((int)RealCurves[0].preWrapMode, (int)RealCurves[0].postWrapMode),
            };

            JobHandle jobHandle = CurveRun.Schedule(EachCurveCount * RealCurves.Count, 1);
            jobHandle.Complete();

            List<int> HighestIndex = new List<int>();
            ResetDebug();
            for (int i = 0; i < RealCurves.Count; i++)
            {

                float MaxValue = 0f;
                int IndexFound = 0;
                for (int j = 0; j < EachCurveCount; j++)
                {
                    //AllCurves.Add(AnimationCurveByIndex(j, i));
                    int4 Val = CurveRun.AllValues[(i * EachCurveCount) + j];
                    AllValues.Add(Val);

                    float Total = Val.w + Val.x + Val.y + Val.z;

                    float Value = (Val.w + Val.y) / Total;



                    float TrueRatio = (Val.w + Val.x) / Total;
                    float FalseRatio = (Val.y + Val.z) / Total;

                    float CorrectRatio = (Val.w + Val.y) / Total;
                    float IncorrectRatio = (Val.x + Val.z) / Total;
                    Ratios.Add(new float4(TrueRatio, FalseRatio, CorrectRatio, IncorrectRatio));
                    //Debug.Log("Value: " + Value);
                    if (Value > MaxValue)
                    {
                        MaxValue = Value;
                        IndexFound = j;
                    }

                }
                Debug.Log("Curve: " + i + "  MaxValue: " + MaxValue + "IndexFound: " + IndexFound);
                HighestIndex.Add(IndexFound);
            }
            for (int i = 0; i < HighestIndex.Count; i++)
            {
                RealCurves[i] = AnimationCurveByIndex(HighestIndex[i], i);
            }


            CurveRun.AllValues.Dispose();
            CurrentIteration += 1;

            //CheckCurveStrength

            AnimationCurve AnimationCurveByIndex(int Index, int curveNum)
            {
                //if(curveNum == 0 && Index == )
                List<long> Outputs = BruteForce.instance.GetOutputList(Index, EachMiddle.Select(i => (long)i).ToList());
                List<AnimationCurve> NewCurves = new List<AnimationCurve>(RealCurves);

                for (int i = 0; i < Outputs.Count; i++)
                {
                    float RangeMultiplier = (Mathf.Pow(CurveConfidence, CurrentIteration)) / 2f;
                    float Range = RangeMultiplier;
                    AnimationCurve curve = RealCurves[curveNum];
                    Keyframe frame = curve.keys[i];
                    float value = frame.value;
                    float NewMax = RealCurves[curveNum].keys[i].value + Range;
                    float NewMin = RealCurves[curveNum].keys[i].value - Range;

                    float ActiveValue = new AllChanges.SingleChange(NewMin, NewMax, 2, (int)Outputs[i]).GetCurrentValue();
                    NewCurves[curveNum].keys[i] = new Keyframe(NewCurves[curveNum].keys[i].time, ActiveValue);
                }
                return NewCurves[curveNum];
            }
        }









    }

    
    
    
    #endregion

}
