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

        [FoldoutGroup("Info")] public bool UseSpecialMotions;
        [FoldoutGroup("Info")] public bool UseFalseMotions;
        [FoldoutGroup("Info"), ShowIf("UseSpecialMotions")] public bool OnlyOtherTrueMotions;

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
            List<int> MotionsToCheck = GetToCheckList(FrameDataMotion);

            int FramesAgo = RestrictionManager.instance.RestrictionSettings.FramesAgo;
            bool CanUseMotion(int MotionClass, int MotionIndex)
            {
                bool MotionWorks = MotionsToCheck.Contains(MotionClass);
                bool IndexWorks = (MotionAssign.instance.InsideTrueMotions(MotionIndex, MotionClass - 1) || !OnlyOtherTrueMotions) || (int)FrameDataMotion == MotionClass;
                return MotionWorks && IndexWorks;

                //(UseSpecialMotions && AllowOtherTrueIndex && !MotionAssign.instance.InsideTrueMotions(j, MotionsToCheck[i] - 1)) == false
            }

            for (int i = 0; i < MotionsToCheck.Count; i++)//motions
                for (int j = 0; j < LM.MovementList[MotionsToCheck[i]].Motions.Count; j++)//set
                    if(CanUseMotion(MotionsToCheck[i], j))
                        for (int k = FramesAgo; k < LM.MovementList[MotionsToCheck[i]].Motions[j].Infos.Count; k++)//frame
                        {
                            List<float> OutputRestrictions = new List<float>();
                            for (int l = 0; l < RestrictionsMotion.Restrictions.Count; l++)
                            {
                                float Value = RestrictionManager.RestrictionDictionary[RestrictionsMotion.Restrictions[l].restriction].Invoke(RestrictionsMotion.Restrictions[l], LM.MovementList[MotionsToCheck[i]].GetRestrictionInfoAtIndex(j, k - FramesAgo), LM.MovementList[MotionsToCheck[i]].GetRestrictionInfoAtIndex(j, k));
                                //if (Value < RegressionSystem.instance.SmallestInput)
                                //Value = RegressionSystem.instance.SmallestInput;
                                OutputRestrictions.Add(Value);
                            }

                            ReturnValue.Add(new SingleFrameRestrictionValues(OutputRestrictions, MotionsToCheck[i] == (int)FrameDataMotion && LM.MovementList[MotionsToCheck[i]].Motions[j].AtFrameState(k)));
                        }

            return ReturnValue;
        }
    }
}

