using System.Collections.Generic;
using Sirenix.OdinInspector;
using System.Linq;
using UnityEngine;
using Unity.Mathematics;
namespace RestrictionSystem
{
    public class RestrictionStatManager : SerializedMonoBehaviour
    {
        public static RestrictionStatManager instance;
        private void Awake() { instance = this; }

        [FoldoutGroup("Info")] public bool UseSpecialMotions;
        [FoldoutGroup("Info")] public bool UseFalseMotions;
        [FoldoutGroup("Info"), ShowIf("UseSpecialMotions")] public bool OnlyOtherTrueMotions;

        public int FrameBreak;
        private int CurrentBreak;
        //public int CanBreak { get { } }

        [FoldoutGroup("Debug"), Button(ButtonSizes.Small)]
        public void CollectDebug()
        {
            List<int> CheckList = GetToCheckList(motion);
            CurrentCheckList = CheckList;
            Debug = new List<List<int>>(MovementControl.instance.MotionCount());


            for (int i = 0; i < MovementControl.instance.MotionCount(); i++)
            {
                Debug.Add(new List<int>());
                for (int j = 0; j < MovementControl.instance.Movements[i].Motions.Count; j++)
                {
                    if (CanUseMotion(CheckList, i, j, motion))
                    {
                        Debug[i].Add(j);
                    }
                }


            }
        }
        [FoldoutGroup("Debug")] public Spell motion;
        [FoldoutGroup("Debug"), ReadOnly] public List<List<int>> Debug;
        [FoldoutGroup("Debug"), ReadOnly] public List<int> CurrentCheckList;
        
        

        public List<int> GetToCheckList(Spell Motion)
        {
            List<int> ReturnList = UseSpecialMotions ? Enumerable.Range(0, MovementControl.instance.MotionCount()).ToList() : new List<int>() { 0, (int)Motion };
            if (!UseFalseMotions)
                ReturnList.Remove(0);
            return ReturnList;
        }
        public List<SingleFrameRestrictionValues> GetRestrictionsForMotions(Spell FrameDataMotion, MotionRestriction RestrictionsMotion)
        {
            List<SingleFrameRestrictionValues> ReturnValue = new List<SingleFrameRestrictionValues>();
            List<int> MotionsToCheck = GetToCheckList(FrameDataMotion);

            int FramesAgo = RestrictionManager.instance.RestrictionSettings.FramesAgo;
            

            for (int i = 0; i < MotionsToCheck.Count; i++)//motions
                for (int j = 0; j < MovementControl.instance.Movements[MotionsToCheck[i]].Motions.Count; j++)//set
                    if(CanUseMotion(MotionsToCheck, MotionsToCheck[i], j, FrameDataMotion))
                        for (int k = FramesAgo; k < MovementControl.instance.Movements[MotionsToCheck[i]].Motions[j].Infos.Count; k++)//frame
                        {
                            if (MotionsToCheck[i] != (int)FrameDataMotion)
                            {
                                CurrentBreak += 1;
                            }
                            if (MotionsToCheck[i] == (int)FrameDataMotion || CurrentBreak == FrameBreak)
                            {
                                if (CurrentBreak == FrameBreak)
                                    CurrentBreak = 0;


                                List<float> OutputRestrictions = new List<float>();
                                for (int l = 0; l < RestrictionsMotion.Restrictions.Count; l++)
                                {
                                    float Value = RestrictionManager.RestrictionDictionary[RestrictionsMotion.Restrictions[l].restriction].Invoke(RestrictionsMotion.Restrictions[l], MovementControl.instance.Movements[MotionsToCheck[i]].GetRestrictionInfoAtIndex(j, k - FramesAgo), MovementControl.instance.Movements[MotionsToCheck[i]].GetRestrictionInfoAtIndex(j, k));
                                    //if (Value < RegressionSystem.instance.SmallestInput)
                                    //Value = RegressionSystem.instance.SmallestInput;
                                    OutputRestrictions.Add(Value);
                                }

                                ReturnValue.Add(new SingleFrameRestrictionValues(OutputRestrictions, MotionsToCheck[i] == (int)FrameDataMotion && MovementControl.instance.Movements[MotionsToCheck[i]].Motions[j].AtFrameState(k)));
                            }
                            
                        }

            return ReturnValue;
        }
        bool CanUseMotion(List<int> MotionsToCheck, int MotionClass, int MotionIndex, Spell FrameDataMotion)
        {
            bool MotionWorks = MotionsToCheck.Contains(MotionClass);
            bool IndexWorks = (MotionAssign.instance.InsideTrueMotions(MotionIndex, MotionClass - 1) || !OnlyOtherTrueMotions) || (int)FrameDataMotion == MotionClass;
            return MotionWorks && IndexWorks;

            //(UseSpecialMotions && AllowOtherTrueIndex && !MotionAssign.instance.InsideTrueMotions(j, MotionsToCheck[i] - 1)) == false
        }
    }
}

