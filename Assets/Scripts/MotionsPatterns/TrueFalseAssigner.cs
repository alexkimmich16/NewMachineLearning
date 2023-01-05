using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Sirenix.OdinInspector;
//using Sirenix.Utilities.Editor;

namespace RestrictionSystem
{
    public class TrueFalseAssigner : SerializedMonoBehaviour
    {
        public static TrueFalseAssigner instance;
        private void Awake() { instance = this; }
        public CurrentLearn MotionType;

        public AllMotions CurrentMotion() { return LearnManager.instance.MovementList[(int)MotionType]; }

        //motion curves are formulas multiplied by their weight then added

        public void PreformMotionState()
        {
            RestrictionManager RM = RestrictionManager.instance;
            for (int i = 0; i < CurrentMotion().Motions.Count; i++)
            {
                List<bool> AllFrames = new List<bool>();
                Motion motion = CurrentMotion().Motions[i];
                motion.TrueRanges.Clear();
                for (int j = 0; j < motion.Infos.Count; j++)
                {
                    bool MeetsQualifications = RestrictionManager.instance.MotionWorks(CurrentMotion().Motions[i].Infos[j], CurrentMotion().Motions[i].Infos[j + 1], RestrictionManager.instance.RestrictionSettings.MotionRestrictions[(int)MotionType]);
                    if (j >= CurrentMotion().Motions[i].Infos.Count - 1)
                        MeetsQualifications = false;
                    AllFrames.Add(MeetsQualifications);
                }
            }


        }

    }
}
