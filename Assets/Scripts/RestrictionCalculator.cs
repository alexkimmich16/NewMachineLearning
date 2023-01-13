using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using RestrictionSystem;


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

    //[FoldoutGroup("RestrictionInfo"), Button(ButtonSizes.Small)] public void RecalculateRestrictionInfo() { ThisSetInfo = GetRestrictionsForSingleMotion(motionGet, Set, BruteForceSettings); }
    [FoldoutGroup("RestrictionInfo"), Button(ButtonSizes.Small)] public void ExportToExcel() { SpreadSheet.instance.PrintRestrictionStats(motionGet, ThisSetInfo); }

    ///expanded removes arrow
    [FoldoutGroup("RestrictionInfo"), ReadOnly, ListDrawerSettings(HideRemoveButton = false)]
    public List<SingleFrameRestrictionInfo> ThisSetInfo;

    /*
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
    */
   
    
    
    
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
