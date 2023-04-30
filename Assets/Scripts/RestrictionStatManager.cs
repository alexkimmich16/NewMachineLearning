using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using RestrictionSystem;
namespace RestrictionSystem
{
    public class RestrictionStatManager : SerializedMonoBehaviour
    {
        public static RestrictionStatManager instance;
        private void Awake() { instance = this; }

        [FoldoutGroup("Info")] public int PastFrameLookup;
        [FoldoutGroup("Info")] public bool UseSpecialMotions;
        [FoldoutGroup("Info")] public bool UseFalseMotions;
        [FoldoutGroup("Info"), ShowIf("UseSpecialMotions")] public bool UseOnlyTrueMotions;

        public LearnManager LM;
        public List<int> GetToCheckList(CurrentLearn Motion)
        {
            List<int> ReturnList = UseSpecialMotions ? new List<int>() { 0, 1, 2, 3 } : new List<int>() { 0, (int)Motion };
            if (!UseFalseMotions)
                ReturnList.RemoveAt(0);
            return ReturnList;
        }
        public List<SingleFrameRestrictionValues> GetRestrictionsForMotions(CurrentLearn FrameDataMotion, MotionRestriction RestrictionsMotion)
        {
            List<SingleFrameRestrictionValues> ReturnValue = new List<SingleFrameRestrictionValues>();
            List<int> ToCheck = GetToCheckList(FrameDataMotion);
            for (int i = 0; i < ToCheck.Count; i++)//motions
                for (int j = 0; j < LM.MovementList[ToCheck[i]].Motions.Count; j++)//set
                    if((UseSpecialMotions && UseOnlyTrueMotions && !MotionAssign.instance.InsideTrueMotions(j, ToCheck[i] - 1)) == false)
                        for (int k = PastFrameLookup; k < LM.MovementList[ToCheck[i]].Motions[j].Infos.Count; k++)//frame
                        {
                            List<float> OutputRestrictions = new List<float>();
                            for (int l = 0; l < RestrictionsMotion.Restrictions.Count; l++)
                            {
                                float Value = RestrictionManager.RestrictionDictionary[RestrictionsMotion.Restrictions[l].restriction].Invoke(RestrictionsMotion.Restrictions[l], LM.MovementList[ToCheck[i]].GetRestrictionInfoAtIndex(j, k - PastFrameLookup), LM.MovementList[ToCheck[i]].GetRestrictionInfoAtIndex(j, k));
                                //if (Value < RegressionSystem.instance.SmallestInput)
                                //Value = RegressionSystem.instance.SmallestInput;
                                OutputRestrictions.Add(Value);
                            }

                            ReturnValue.Add(new SingleFrameRestrictionValues(OutputRestrictions, ToCheck[i] == (int)FrameDataMotion && LM.MovementList[ToCheck[i]].Motions[j].AtFrameState(k)));
                        }

            return ReturnValue;
        }
    }
}

