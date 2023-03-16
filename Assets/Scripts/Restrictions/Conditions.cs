using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
namespace RestrictionSystem
{
    [CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/Conditions", order = 3)]
    public class Conditions : SerializedScriptableObject
    {
        [ListDrawerSettings(ShowIndexLabels = true, ListElementLabelName = "Motion")] public List<MotionConditionInfo> MotionConditions;

        public void ResetConditions()
        {
            for (int i = 0; i < MotionConditions.Count; i++)
            {
                MotionConditions[i].CurrentStage = new List<int>();
                for (int j = 0; j < 2; j++)
                    MotionConditions[i].CurrentStage.Add(0);

                MotionConditions[i].WaitingForFalse = new List<bool>();
                for (int j = 0; j < 2; j++)
                    MotionConditions[i].WaitingForFalse.Add(false);
            }
        }
    }
}
    
