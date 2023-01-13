using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using RestrictionSystem;

[System.Serializable]
public class SingleFrameRestrictionInfo
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
public class RestrictionCalculator : SerializedMonoBehaviour
{
    public CurrentLearn motionGet;

    //[FoldoutGroup("Stats"), ReadOnly] public int NextSet;
    [FoldoutGroup("Stats")] public int Set;
    [FoldoutGroup("Stats")] public bool GetAdjustedRestrictionValue;
    [FoldoutGroup("Stats")] public int PastFrameLookup;
    [FoldoutGroup("Stats")] public Dictionary<CurrentLearn, int> RestrictionIndex;

    [FoldoutGroup("MaxMin"), ReadOnly] public List<Vector2> MaxMin;
    [Button(ButtonSizes.Small)]
    [FoldoutGroup("MaxMin")] public void CalculateMaxMin() { MaxMin = GetMaxMinValues(); }



    [FoldoutGroup("RestrictionInfo"), Button(ButtonSizes.Small)] public void RecalculateRestrictionInfo() { ThisSetInfo = GetRestrictionsForSingleMotion(motionGet, Set, BruteForceSettings); }
    [FoldoutGroup("RestrictionInfo"), Button(ButtonSizes.Small)] public void ExportToExcel() { SpreadSheet.instance.PrintRestrictionStats(motionGet, ThisSetInfo); }

    ///expanded removes arrow
    [FoldoutGroup("RestrictionInfo"), ReadOnly, ListDrawerSettings(HideRemoveButton = false)]
    public List<SingleFrameRestrictionInfo> ThisSetInfo;

    [System.Serializable]
    public class AllChanges
    {
        public delegate void StopEvent();
        public event StopEvent OnStop;
        public SingleChange ParentWeightThreshold;
        public List<OneRestrictionChange> Restrictions;
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
                Singles[i].CurrentStep = 0;
            OnStop();
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
        public class OneRestrictionChange
        {
            public SingleChange Max;
            public SingleChange Min;
            public SingleChange Weight;
        }
        [System.Serializable]
        public class SingleChange
        {
            public float Max;
            public float Min;
            public int MiddleSteps;
            [ReadOnly] public int CurrentStep;
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
    [FoldoutGroup("BruteForce")] public int CheckPerFrame;
    [ReadOnly, FoldoutGroup("BruteForce")] public bool RunningBruteForce;
    [FoldoutGroup("BruteForce")] public AllChanges AllChange;
    [ReadOnly, FoldoutGroup("BruteForce")] public MotionRestrictionOutput BestBruteForceInfo;
    [ReadOnly, FoldoutGroup("BruteForce")] public MotionRestriction BruteForceSettings;
    [ReadOnly, FoldoutGroup("BruteForce")] public List<SingleFrameRestrictionInfo> FrameInfo;
    

    public List<SingleFrameRestrictionInfo> GetInputInfo(CurrentLearn motion)
    {
        List<SingleFrameRestrictionInfo> ReturnValue = new List<SingleFrameRestrictionInfo>();

        for (int i = 0; i < LearnManager.instance.MovementList[(int)motion].Motions.Count; i++)
            ReturnValue.AddRange(GetRestrictionsForSingleMotion(motion, i, BruteForceSettings));
        for (int i = 0; i < LearnManager.instance.MovementList[0].Motions.Count; i++)
            ReturnValue.AddRange(GetRestrictionsForSingleMotion(CurrentLearn.Nothing, i, BruteForceSettings));
        ///introduce others as all false
        return ReturnValue;
    }
    [ReadOnly, FoldoutGroup("CombinationStats")] public int PossibleCombinations;
    [ReadOnly, FoldoutGroup("CombinationStats")] public float SecondsRequired;
    [ReadOnly, FoldoutGroup("CombinationStats")] public float MinutesRequired;
    [ReadOnly, FoldoutGroup("CombinationStats")] public int CurrentFramesDone;
    [ReadOnly, FoldoutGroup("CombinationStats"), Range(0,100)] public float PercentDone;
    [FoldoutGroup("CombinationStats"), Button(ButtonSizes.Small)]
    public void GetMaxCombinations()
    {
        List<AllChanges.SingleChange> SinglesList = AllChange.GetSingles();
        int Total = SinglesList[0].MiddleSteps + 2;
        for (int i = 1; i < SinglesList.Count; i++)
            Total = Total * (SinglesList[i].MiddleSteps + 2);
        PossibleCombinations = Total;
        SecondsRequired = PossibleCombinations / (60f * CheckPerFrame);
        MinutesRequired = SecondsRequired / 60f;
        CurrentFramesDone = AllChange.CurrentDone();
        PercentDone = (((float)CurrentFramesDone) / ((float)PossibleCombinations)) * 100f;
    }
    
    [FoldoutGroup("BruteForce"), Button(ButtonSizes.Small)]
    public void StartBruteForceRun()
    {
        BruteForceSettings = new MotionRestriction(RestrictionManager.instance.RestrictionSettings.MotionRestrictions[(int)motionGet - 1]);
        FrameInfo = GetInputInfo(motionGet);
        RunningBruteForce = true;
        StartCoroutine(BruteForceCheck());
    }

    IEnumerator BruteForceCheck()
    {
        while (RunningBruteForce)
        {
            int CurrentChecked = 0;
            while (CurrentChecked < CheckPerFrame)
            {
                AllChange.NextStep();

                int Past = 0;
                while (AllChange.HasOverLap() == true)
                {
                    AllChange.NextStep();
                    Past += 1;
                    Debug.Log("Past: " + Past);
                }

                /// set values
                List<float> EncodedStatsList = AllChange.GetEncodedInfo();
                //Debug.Log("Count: " + EncodedStatsList.Count);
                BruteForceSettings.WeightedValueThreshold = EncodedStatsList[0];
                for (int i = 0; i < ((EncodedStatsList.Count - 1) / 3); i++)
                {
                    BruteForceSettings.Restrictions[i].MaxSafe = EncodedStatsList[(i * 3) + 1];
                    BruteForceSettings.Restrictions[i].MinSafe = EncodedStatsList[(i * 3) + 2];
                    BruteForceSettings.Restrictions[i].Weight = EncodedStatsList[(i * 3) + 3];
                }

                Vector2 Total = Vector2.zero;

                for (int i = 0; i < FrameInfo.Count; i++)
                {
                    bool IsCorrect = Correct(FrameInfo[i]);
                    Total += IsCorrect ? new Vector2(1, 0) : new Vector2(0, 1);
                }
                float PercentGuess = (Total.x / (Total.x + Total.y)) * 100f;
                if (PercentGuess > BestBruteForceInfo.Highest)
                    BestBruteForceInfo = new MotionRestrictionOutput(EncodedStatsList, PercentGuess);

                CurrentChecked += 1;
                bool Correct(SingleFrameRestrictionInfo info)
                {
                    float TotalWeightValue = 0f;
                    float TotalWeight = 0f;
                    for (int i = 0; i < info.OutputRestrictions.Count; i++)
                    {
                        //Debug.Log(i + "  Total: " + info.OutputRestrictions.Count);
                        float RestrictionValue = BruteForceSettings.Restrictions[i].GetValue(info.OutputRestrictions[i]);
                        TotalWeightValue += BruteForceSettings.Restrictions[i].Active ? RestrictionValue * BruteForceSettings.Restrictions[i].Weight : 0;
                        TotalWeight += BruteForceSettings.Restrictions[i].Active ? BruteForceSettings.Restrictions[i].Weight : 0;
                    }
                    float MinWeightThreshold = BruteForceSettings.WeightedValueThreshold * TotalWeight;
                    return (TotalWeightValue >= MinWeightThreshold) == info.AtMotionState;
                }
            }
            yield return new WaitForEndOfFrame();
        }
    }
    public void OnStopForceCheck()
    {
        RunningBruteForce = false;
        Debug.Log("stop");
    }
    public List<SingleFrameRestrictionInfo> GetRestrictionsForSingleMotion(CurrentLearn FrameDataMotion, int Set, MotionRestriction RestrictionsMotion)
    {
        List<SingleFrameRestrictionInfo> ReturnValue = new List<SingleFrameRestrictionInfo>();
        for (int i = PastFrameLookup; i < LearnManager.instance.MovementList[(int)FrameDataMotion].Motions[Set].Infos.Count; i++)
        {
            RestrictionSystem.SingleInfo frame1 = LearnManager.instance.MovementList[(int)FrameDataMotion].GetRestrictionInfoAtIndex(Set, i - PastFrameLookup);
            RestrictionSystem.SingleInfo frame2 = LearnManager.instance.MovementList[(int)FrameDataMotion].GetRestrictionInfoAtIndex(Set, i);

            List<float> OutputRestrictions = new List<float>();

            for (int k = 0; k < RestrictionsMotion.Restrictions.Count; k++)
            {
                MotionTest RestrictionType = RestrictionManager.RestrictionDictionary[RestrictionsMotion.Restrictions[k].restriction];
                float RawValue = RestrictionType.Invoke(RestrictionsMotion.Restrictions[k], frame1, frame2);
                float Value = RawValue;
                OutputRestrictions.Add(Value);
            }
            ReturnValue.Add(new SingleFrameRestrictionInfo(OutputRestrictions, LearnManager.instance.MovementList[(int)FrameDataMotion].Motions[Set].AtFrameState(i)));
        }
        return ReturnValue;
    }
    private void Start()
    {
        AllChange.OnStop += OnStopForceCheck;
    }
    [FoldoutGroup("MaxMin"), Button(ButtonSizes.Small)]
    public void CopyToDebug()
    {
        RestrictionManager RM = RestrictionManager.instance;
        DebugRestrictions.instance.Restrictions.Restrictions = new List<SingleRestriction>(RM.RestrictionSettings.MotionRestrictions[(int)motionGet + 1].Restrictions);
        for (int i = 0; i < DebugRestrictions.instance.Restrictions.Restrictions.Count; i++)
        {
            DebugRestrictions.instance.Restrictions.Restrictions[i].MinSafe = MaxMin[i].x;
            DebugRestrictions.instance.Restrictions.Restrictions[i].MaxSafe = MaxMin[i].y;
        }
    }
    public List<Vector2> GetMaxMinValues()
    {
        RestrictionManager RM = RestrictionManager.instance;
        List<Vector2> Values = new List<Vector2>();
        AllMotions motion = LearnManager.instance.MovementList[(int)motionGet];
        //List<>
        List<SingleRestriction> Restrictions = RM.RestrictionSettings.MotionRestrictions[(int)motionGet + 1].Restrictions;
        for (int i = 0; i < Restrictions.Count; i++)
            Values.Add(new Vector2(1000, 0));
            //for (int i = 0; i < motion.Motions.Count; i++)
            //Values.Add()
        for (int i = 0; i < motion.Motions.Count; i++)//motion
        {
            for (int j = PastFrameLookup; j < motion.Motions[i].Infos.Count; j++)//single info
            {
                for (int k = 0; k < Restrictions.Count; k++)//single restriction
                {
                    if (motion.Motions[i].AtFrameState(j))
                    {
                        RestrictionSystem.SingleInfo frame1 = motion.GetRestrictionInfoAtIndex(i, j - PastFrameLookup);
                        RestrictionSystem.SingleInfo frame2 = motion.GetRestrictionInfoAtIndex(i, j);
                        MotionTest RestrictionType = RestrictionManager.RestrictionDictionary[Restrictions[k].restriction];
                        float RawRestrictionValue = RestrictionType.Invoke(Restrictions[k], frame1, frame2);

                        if (RawRestrictionValue < Values[k].x)
                            Values[k] = new Vector2(RawRestrictionValue, Values[k].y);
                        if (RawRestrictionValue > Values[k].y)
                            Values[k] = new Vector2(Values[k].x, RawRestrictionValue);
                    }
                    //Restrictions
                }
                //Restrictions
            }
        }
        return Values;
    }
    //public void Get
}
[System.Serializable]
public class MotionRestrictionOutput
{
    [Range(0, 100)]public float Highest;

    public float ParentWeightThreshold;
    public List<OneRestrictionInfo> RestrictionInfos;
    public MotionRestrictionOutput(List<float> EncodedStatsList, float Highest)
    {
        this.Highest = Highest;
        ParentWeightThreshold = EncodedStatsList[0];
        //EncodedStatsList.RemoveAt(0);
        List<OneRestrictionInfo> GetInfo = new List<OneRestrictionInfo>();
        for (int i = 0; i < (EncodedStatsList.Count / 3); i++)
        {
            GetInfo.Add(new OneRestrictionInfo(EncodedStatsList[(i * 3) + 1], EncodedStatsList[(i * 3) + 2], EncodedStatsList[(i * 3) + 3]));
        }
        RestrictionInfos = GetInfo;
    }
    [System.Serializable]
    public class OneRestrictionInfo
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