using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/LearningStats", order = 1)]
public class LearningStats : SerializedScriptableObject
{
    public float Complexity;
    public float TrialDuration;
}
