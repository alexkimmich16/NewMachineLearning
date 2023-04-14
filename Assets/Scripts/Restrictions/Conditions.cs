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
       
    }

}
    
