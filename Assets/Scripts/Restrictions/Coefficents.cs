using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
namespace RestrictionSystem
{
    [CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/Coefficents", order = 3)]
    public class Coefficents : SerializedScriptableObject
    {
        [ListDrawerSettings(ShowIndexLabels = true)] public List<RegressionInfo> RegressionStats;
    }

    [System.Serializable]
    public class RegressionInfo
    {
        public float Intercept;
        public List<DegreeList> Coefficents;

        [System.Serializable]
        public class DegreeList
        {
            public List<float> Degrees;
        }
    }
}



