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
    [Sirenix.OdinInspector.ReadOnly, FoldoutGroup("BruteForce")] public MotionRestrictionStruct BestBruteForceInfo;
    [Sirenix.OdinInspector.ReadOnly, FoldoutGroup("BruteForce")] public MotionRestriction BruteForceSettings;
    [Sirenix.OdinInspector.ReadOnly, FoldoutGroup("BruteForce")] public List<SingleFrameRestrictionInfo> FrameInfo;

    [FoldoutGroup("BruteForce")] public bool UseFrameCount;
    [FoldoutGroup("BruteForce"), ShowIf("UseFrameCount")] public int MaxFrames;

    [BurstCompile(OptimizeFor = OptimizeFor.FastCompilation)]
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
                if (UseFrameCount && CountNumber > MaxFrames)
                    WaitingForFinish = false;
                bool CalledOnce = false;
                
                while (HasOverLap(ConvertedSingles) == true || CalledOnce == false)
                {
                    CountNumber += 1;
                    CalledOnce = true;
                    for (int i = 0; i < ConvertedSingles.Length; i++)
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

                }
                if (!WaitingForFinish)
                    break;
                //OnStop();
                bool HasOverLap(NativeArray<SingleInfo> Singles)
                {
                    ///use linq for check ehre if any in return true
                    for (int i = 0; i < (Singles.Length + 1) / 3; i++)
                        if (Singles[(i * 3) + 1].GetCurrentValue() <= Singles[(i * 3) + 2].GetCurrentValue())
                            return true;
                    return false;
                }
                //check values
                int Correct = 0;
                int InCorrect = 0;
                for (int i = 0; i < FlatRawValues.Length / ExpandValue; i++) // all raw value inputs
                {
                    float TotalWeightValue = 0f;
                    float TotalWeight = 0f;
                    for (int j = 0; j < ExpandValue; j++)// all restrictions
                    {
                        float Weight = ConvertedSingles[(j * 3) + 3].GetCurrentValue();
                        TotalWeightValue += GetValue(ConvertedSingles[(j * 3) + 1].GetCurrentValue(), ConvertedSingles[(j * 3) + 2].GetCurrentValue(), FlatRawValues[(i * ExpandValue) + j]) * Weight;
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
            float GetValue(float Max, float Min, float Input) { return Input < Max && Input > Min ? 1 : 0; }
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
            AllChangeStatsInput[(i * 3) + 2] = Singles[i].MiddleSteps;
        }

        NativeArray<float> OutputVals = new NativeArray<float>(AllChange.GetEncodedInfo().Count + 1, Allocator.TempJob);

        float StartTime = Time.realtimeSinceStartup;
        int Value = 0;
        AllBruteForce BruteForceRun = new AllBruteForce
        {
            OutputValues = OutputVals,
            NativeSingles = AllChangeStatsInput,
            ExpandValue = FrameInfo[0].OutputRestrictions.Count,
            States = StatesStat,
            FlatRawValues = FlatRawStat,
            CountNumber = Value,
            MaxFrames = MaxFrames,
            UseFrameCount = UseFrameCount,
            //AllChangeStats = new AllChanges(AllChange),
        };
        JobHandle jobHandle = BruteForceRun.Schedule();
        //BruteForceRun.Run();
        jobHandle.Complete();
        List<float> EncodedValues = new List<float>();
        for (int i = 0; i < BruteForceRun.OutputValues.Length; i++)
        {
            Debug.Log("value: " + BruteForceRun.OutputValues[i]);
            EncodedValues.Add(BruteForceRun.OutputValues[i]);
        }
        BestBruteForceInfo = new MotionRestrictionStruct(EncodedValues);
        BruteForceRun.OutputValues.Dispose();
        
        
        Debug.Log("Frames: " + BruteForceRun.CountNumber + " in: " + (Time.realtimeSinceStartup - StartTime).ToString("F3") + " Seconds");
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
    public bool HasOverLap()
    {
        for (int i = 0; i < Restrictions.Count; i++)
            if (Restrictions[i].Max.GetCurrentValue() <= Restrictions[i].Min.GetCurrentValue())
                return true;
        return false;
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
            if (SinglesList[i].MiddleSteps + 2 != SinglesList[i].CurrentStep)
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


