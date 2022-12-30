using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/PastTrialInfo", order = 1)]
public class PastTrialHolder : ScriptableObject
{
    public List<SinglePastTrialInfo> AllInfoList = new List<SinglePastTrialInfo>();
}
[System.Serializable]
public class SinglePastTrialInfo
{
    public int Complexity;
    public float TrialDuration, GridFadeSpeed, OutputValue;
    public SinglePastTrialInfo(int ComplexityStat, float TrialDurationStat, float GridFadeSpeedStat, float OutputValueStat)
    {
        Complexity = ComplexityStat;
        TrialDuration = TrialDurationStat;
        GridFadeSpeed = GridFadeSpeedStat;
        OutputValue = OutputValueStat;
    }
}
