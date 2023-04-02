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
    public struct RegressionInfo
    {
        public float Intercept;
        public List<DegreeList> Coefficents;

        [System.Serializable]
        public struct DegreeList
        {
            public List<float> Degrees;
        }

        public float[] GetCoefficents()
        {
            float[] ReturnValue = new float[(this.Coefficents.Count * this.Coefficents[0].Degrees.Count) + 1];
            ReturnValue[0] = Intercept;
            for (int i = 0; i < this.Coefficents.Count; i++)
                for (int j = 0; j < this.Coefficents[i].Degrees.Count; j++)
                    ReturnValue[j] = Coefficents[i].Degrees[j];

            return ReturnValue;
        }
    }
}



