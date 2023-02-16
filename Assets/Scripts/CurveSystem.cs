using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using RestrictionSystem;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Collections;
public class CurveSystem : SerializedMonoBehaviour
{
    public static CurveSystem instance;
    private void Awake() { instance = this; }
    [FoldoutGroup("Curve"), ListDrawerSettings(ShowIndexLabels = true)] public List<SingleFrameRestrictionValues> FrameInfo;
    [FoldoutGroup("Curve")] public List<AnimationCurve> RealCurves;
    [FoldoutGroup("Curve"), Range(0, 1)] public float Alpha = 0.05f;
    [FoldoutGroup("Curve"), Sirenix.OdinInspector.ReadOnly] public int CurrentIteration;
    [FoldoutGroup("Curve")] public int NumberPerMotion = 15;
    [FoldoutGroup("Curve"), Range(0, 1)] public float CurveConfidence;

    [FoldoutGroup("Curve"), Button(ButtonSizes.Small)]
    public void ClearCurves() { RealCurves.Clear(); }

    private void Start()
    {
        RealCurves = new List<AnimationCurve>();
    }

    [BurstCompile(CompileSynchronously = true)]
    private struct LearnAnimationCurve : IJobParallelFor
    {
        public NativeArray<NativeCurve> Curves;
        public void Execute(int Index)
        {

        }
    }
    [FoldoutGroup("Curve"), Button(ButtonSizes.Small)]
    public void NextCurveState()
    {
        FrameInfo = BruteForce.instance.GetRestrictionsForMotions(BruteForce.instance.motionGet, RestrictionManager.instance.RestrictionSettings.MotionRestrictions[(int)BruteForce.instance.motionGet - 1]);
        List<float2> Ranges = BruteForce.instance.GetRangeOfMinMaxValues(FrameInfo);
        if (RealCurves.Count == 0) //initialize
        {
            for (int i = 0; i < FrameInfo[0].OutputRestrictions.Count; i++)
            {
                RealCurves.Add(new AnimationCurve());
                for (int j = 0; j < NumberPerMotion; j++)
                {
                    RealCurves[i].AddKey(Mathf.Lerp(Ranges[i].x, Ranges[i].y, j / (float)NumberPerMotion), 0);
                }
            }
            //return;
        }

        int TotalChecksRequired = (int)Mathf.Pow(2, NumberPerMotion);

        List<long> EachMiddle = new List<long>();
        for (int i = 0; i < NumberPerMotion; i++)
            EachMiddle.Add(1);
        for (int i = 0; i < NumberPerMotion; i++)
            for (int j = 0; j < i; j++)
                EachMiddle[j] = (long)(EachMiddle[j] * 2);


        NativeArray<NativeCurve> NativeCurves = new NativeArray<NativeCurve>();
        for (int i = 0; i < NumberPerMotion; i++)
            NativeCurves[i].Update(, 2);
        LearnAnimationCurve BruteForceRun = new LearnAnimationCurve
        {
            Curves = new NativeArray<NativeCurve>()
        };

        JobHandle jobHandle = BruteForceRun.Schedule(2000, 1);
        jobHandle.Complete();

        for (int i = 0; i < RealCurves.Count; i++)
        {
            float MaxValue = 0f;
            int IndexFound = 0;
            for (int j = 0; j < TotalChecksRequired; j++)
            {
                float Value = GetPercentageByIndex(j, i);
                if (Value > MaxValue)
                {
                    MaxValue = Value;
                    IndexFound = j;
                }
            }
            Debug.Log("MaxValue: " + MaxValue);
            RealCurves[i] = AnimationCurveByIndex(IndexFound, i);
            ///set 
        }

        AnimationCurve AnimationCurveByIndex(int Index, int curveNum)
        {
            List<long> Outputs = BruteForce.instance.GetOutputList(Index, EachMiddle);
            List<AnimationCurve> NewCurves = new List<AnimationCurve>(RealCurves);

            for (int i = 0; i < NumberPerMotion; i++)
            {
                float Range = (1f * Mathf.Pow(CurveConfidence, CurrentIteration)) / 2f;
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

}
