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
    public bool IsGoodMotion;
    public SingleFrameRestrictionInfo(List<float> OutputRestrictionsStat)
    {
        OutputRestrictions = OutputRestrictionsStat;
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



    [FoldoutGroup("RestrictionInfo"), Button(ButtonSizes.Small)] public void RecalculateRestrictionInfo() { ThisSetInfo = GetRestrictionsForSingleMotion(motionGet, Set, GetAdjustedRestrictionValue); }
    [FoldoutGroup("RestrictionInfo"), Button(ButtonSizes.Small)] public void ExportToExcel() { SpreadSheet.instance.PrintRestrictionStats(motionGet, ThisSetInfo); }

    ///expanded removes arrow
    [FoldoutGroup("RestrictionInfo"), ReadOnly, ListDrawerSettings(HideRemoveButton = false)]
    public List<SingleFrameRestrictionInfo> ThisSetInfo;




    
    
    
    public List<SingleFrameRestrictionInfo> GetRestrictionsForSingleMotion(CurrentLearn motion, int Set, bool GetAdjustedRestrictionValue)
    {
        List<SingleFrameRestrictionInfo> ReturnValue = new List<SingleFrameRestrictionInfo>();

        RestrictionManager RM = RestrictionManager.instance;
        List<SingleRestriction> Restrictions = RM.RestrictionSettings.MotionRestrictions[(int)motion + 1].Restrictions;
        for (int i = PastFrameLookup; i < LearnManager.instance.MovementList[(int)motion].Motions[Set].Infos.Count; i++)
        {
            RestrictionSystem.SingleInfo frame1 = LearnManager.instance.MovementList[(int)motion].GetRestrictionInfoAtIndex(Set, i - PastFrameLookup);
            RestrictionSystem.SingleInfo frame2 = LearnManager.instance.MovementList[(int)motion].GetRestrictionInfoAtIndex(Set, i);

            List<float> OutputRestrictions = new List<float>();

            for (int k = 0; k < Restrictions.Count; k++)
            {
                MotionTest RestrictionType = RestrictionManager.RestrictionDictionary[Restrictions[k].restriction];
                //OutputRestrictions.Add(Restrictions[k].GetValue(RawRestrictionValue));
                float RawValue = RestrictionType.Invoke(Restrictions[k], frame1, frame2);
                float Value = GetAdjustedRestrictionValue ? Restrictions[k].GetValue(RawValue) : RawValue;
                OutputRestrictions.Add(Value);
            }
            ReturnValue.Add(new SingleFrameRestrictionInfo(OutputRestrictions));
        }
        return ReturnValue;
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
    // Update is called once per frame
    void Update()
    {
        
    }
}
