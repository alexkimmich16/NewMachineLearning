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
                MotionConditions[i].WaitingForFalse = new List<bool>();
                for (int j = 0; j < 2; j++)
                {
                    MotionConditions[i].CurrentStage.Add(0);
                    MotionConditions[i].WaitingForFalse.Add(false);
                }

                for (int j = 0; j < MotionConditions[i].ConditionLists.Count; j++)
                {
                    for (int k = 0; k < MotionConditions[i].ConditionLists[j].SingleConditions.Count; k++)
                    {
                        MotionConditions[i].ConditionLists[j].SingleConditions[k].Value = new List<float> { 0f, 0f };
                        MotionConditions[i].ConditionLists[j].SingleConditions[k].StartTime = new List<float> { 0f, 0f };
                        MotionConditions[i].ConditionLists[j].SingleConditions[k].LastState = new List<bool> { false, false };
                        MotionConditions[i].ConditionLists[j].SingleConditions[k].StartPos = new List<Vector3> { Vector3.zero, Vector3.zero };
                    }
                        
                }

                for (int j = 0; j < MotionConditions[i].ProhibitList.Count; j++)
                {
                    MotionConditions[i].ProhibitList[j].Value = new List<float> { 0f, 0f };
                    MotionConditions[i].ProhibitList[j].StartTime = new List<float> { 0f, 0f };
                    MotionConditions[i].ProhibitList[j].LastState = new List<bool> { false, false };
                    MotionConditions[i].ProhibitList[j].StartPos = new List<Vector3> { Vector3.zero, Vector3.zero };

                }
            }
        }
    }
}
    
