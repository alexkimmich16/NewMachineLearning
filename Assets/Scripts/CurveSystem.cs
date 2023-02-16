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
    //[FoldoutGroup("Output"), ListDrawerSettings(ShowIndexLabels = true)] public List<SingleFrameRestrictionValues> FrameInfo;
    [FoldoutGroup("Output")] public List<AnimationCurve> RealCurves;
    //[FoldoutGroup("Output")] public NativeCurveHolder CurveHolder;

    [FoldoutGroup("Input")] public int NumberPerMotion = 15;
    [FoldoutGroup("Input"), Range(0, 1)] public float CurveConfidence;
    [FoldoutGroup("Input")] public int Resolution = 200;
    

    [FoldoutGroup("Curve"), Button(ButtonSizes.Small)]
    public void ClearCurves() { RealCurves.Clear(); }

    [FoldoutGroup("Curve"), Button(ButtonSizes.Small)]
    public void RunCurve() { NextCurveState(); }

    [FoldoutGroup("Debug")] public List<float> Origional;
    [FoldoutGroup("Debug")] public List<float> Adjusted;

    private void Start()
    {
        RealCurves = new List<AnimationCurve>();
    }
    
    //[BurstCompile(CompileSynchronously = true)]
    private struct LearnAnimationCurve : IJobParallelFor
    {
        [NativeDisableParallelForRestriction] public NativeArray<float> AllValues;

        [Unity.Collections.ReadOnly] public int2 WrapModes;

        [Unity.Collections.ReadOnly] public NativeArray<bool> States;
        [Unity.Collections.ReadOnly] public NativeArray<float> FlatRawValues;

        [Unity.Collections.ReadOnly] public NativeArray<float> OrigionalCurves;
        [Unity.Collections.ReadOnly] public NativeArray<float> AdjustedCurves;

        [DeallocateOnJobCompletion, Unity.Collections.ReadOnly] public NativeArray<int> WeightedMiddleSteps;

        public int RunsPerCurve;
        public int Resolution;
        public void Execute(int Index)
        {
            NativeArray<int> CurrentValues = new NativeArray<int>(WeightedMiddleSteps.Length, Allocator.Temp);
            int Section = Mathf.FloorToInt(Index / RunsPerCurve);

            //int ThisCurveIndex = Index - (RunsPerCurve * Section);
            //int LeftCount = ThisCurveIndex;

            //Holder.OrigionalCurves[Section] = Holder.AdjustedCurves[Index];
            float2 FalseTrue = float2.zero; // x = false
            for (int i = 0; i < FlatRawValues.Length / OrigionalCurves.Length; i++) // all motions
            {
                float TotalWeightValue = 0f;
                for (int j = 0; j < OrigionalCurves.Length; j++)
                {
                    bool UseOrigional = Section != i;

                    NativeArray<float> SingleCurve = new NativeArray<float>(Resolution, Allocator.Temp);
                    for (int k = 0; k < Resolution; k++)
                        SingleCurve[k] = UseOrigional ? OrigionalCurves[Section * Resolution + k] : AdjustedCurves[Index * Resolution + k];

                    
                    TotalWeightValue += Evaluate(SingleCurve, FlatRawValues[(i * OrigionalCurves.Length) + j], WrapModes);
                }
                bool Correct = (TotalWeightValue >= 1) == States[i];
                FalseTrue = new float2(FalseTrue.x + (!Correct ? 1f : 0f), FalseTrue.y + (Correct ? 1f : 0f));
            }

            AllValues[Index] = (FalseTrue.y / (FalseTrue.x + FalseTrue.y)) * 100f;
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
    
    public void NextCurveState()
    {
        if (BruteForce.instance.FrameInfo.Count == 0)
            BruteForce.instance.FrameInfo = BruteForce.instance.GetRestrictionsForMotions(BruteForce.instance.motionGet, RestrictionManager.instance.RestrictionSettings.MotionRestrictions[(int)BruteForce.instance.motionGet - 1]);


        List<float2> Ranges = BruteForce.instance.GetRangeOfMinMaxValues(BruteForce.instance.FrameInfo);
        if (RealCurves.Count == 0) //initialize
        {
            for (int i = 0; i < BruteForce.instance.FrameInfo[0].OutputRestrictions.Count; i++)
            {
                RealCurves.Add(new AnimationCurve());
                for (int j = 0; j < NumberPerMotion; j++)
                {
                    RealCurves[i].AddKey(Mathf.Lerp(Ranges[i].x, Ranges[i].y, j / (float)NumberPerMotion), 0);
                }
            }
        }

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
            OrigionalCurves = OrigionalCurves,
            AdjustedCurves = AdjustedCurves,
            RunsPerCurve = EachCurveCount,
            AllValues = new NativeArray<float>(EachCurveCount * RealCurves.Count, Allocator.TempJob),
            States = BruteForce.instance.GetStatesStat(),
            FlatRawValues = BruteForce.instance.GetFlatRawStat(),
            WeightedMiddleSteps = new NativeArray<int>(EachMiddle.ToArray(), Allocator.TempJob),
            Resolution = Resolution,
            WrapModes = new int2((int)RealCurves[0].preWrapMode, (int)RealCurves[0].postWrapMode),
        };

        JobHandle jobHandle = CurveRun.Schedule(EachCurveCount * RealCurves.Count, 1);
        jobHandle.Complete();

        List<int> HighestIndex = new List<int>();
        for (int i = 0; i < RealCurves.Count; i++)
        {
            float MaxValue = 0f;
            int IndexFound = 0;
            for (int j = 0; j < EachCurveCount; j++)
            {
                //float Value = GetPercentageByIndex(k, k);
                float Value = CurveRun.AllValues[(i * EachCurveCount) + j];
                if (Value > MaxValue)
                {
                    MaxValue = Value;
                    IndexFound = j;
                }
            }
            Debug.Log("Curve: " + i + "  MaxValue: " + MaxValue);
            HighestIndex.Add(IndexFound);
        }
        for (int i = 0; i < HighestIndex.Count; i++)
        {
            RealCurves[i] = AnimationCurveByIndex(HighestIndex[i], i);
        }


        CurveRun.AllValues.Dispose();
        CurrentIteration += 1;
        
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

        float GetPercentageByIndex(int index, int curve)
        {
            List<AnimationCurve> NewCurves = new List<AnimationCurve>(RealCurves);
            NewCurves[curve] = AnimationCurveByIndex(index, curve);
            return CheckCurveStrength(NewCurves);
        }
        
    }

    public float CheckCurveStrength(List<AnimationCurve> ToCheck)
    {
        float2 WrongRight = new float2(0f, 0f);
        List<SingleFrameRestrictionValues> MotionValues = BruteForce.instance.GetRestrictionsForMotions(BruteForce.instance.motionGet, RestrictionManager.instance.RestrictionSettings.MotionRestrictions[(int)BruteForce.instance.motionGet - 1]);
        for (int i = 0; i < MotionValues.Count; i++)
        {
            float TotalCheckValue = 0f;
            for (int j = 0; j < ToCheck.Count; j++)
                TotalCheckValue += ToCheck[j].Evaluate(MotionValues[i].OutputRestrictions[j]);
            bool Correct = (TotalCheckValue > 0) == MotionValues[i].AtMotionState;
            WrongRight = new float2(WrongRight.x + (!Correct ? 1f : 0f), WrongRight.y + (Correct ? 1f : 0f));
        }
        return (WrongRight.y / (WrongRight.x + WrongRight.y)) * 100f;
    }
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
    
}
