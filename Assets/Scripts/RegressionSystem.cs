using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Unity.Mathematics;
//using UnityEgnine.math
namespace RestrictionSystem
{
    public class RegressionSystem : SerializedMonoBehaviour
    {
        public RegressionInfo info;
        public List<float> OutputValues;
        [FoldoutGroup("Curve"), Button(ButtonSizes.Small)]
        public void CheckAll()
        {
            OutputValues = new List<float>();
            float2 Guesses = new float2(0f,0f);
            List<SingleFrameRestrictionValues> FrameInfo = BruteForce.instance.GetRestrictionsForMotions(BruteForce.instance.motionGet, RestrictionManager.instance.RestrictionSettings.MotionRestrictions[(int)BruteForce.instance.motionGet - 1]);
            for (int i = 0; i < FrameInfo.Count; i++)
            {
                float Total = 0f;
                for (int j = 0; j < info.Coefficents.Count; j++)//each  variable
                    for (int k = 0; k < info.Coefficents[j].Degrees.Count; k++)//powers
                        Total += Mathf.Pow(info.Coefficents[j].Degrees[k] * FrameInfo[i].OutputRestrictions[j], k + 1);
                Total += info.Intercept;

                
                //insert formula
                float GuessValue = 1f / (1f + Mathf.Exp(-Total));
                OutputValues.Add(GuessValue);
                bool Guess = GuessValue > 0.5f;
                bool Truth = FrameInfo[i].AtMotionState;
                bool Correct = Guess == Truth;
                Guesses = new float2(Guesses.x + (!Correct ? 1f : 0f), Guesses.y + (Correct ? 1f : 0f));
            }
            float CorrectPercent = Guesses.y / (Guesses.x + Guesses.y);
            Debug.Log(CorrectPercent + "% Correct");
        }
        /*
        public bool GetGuess(SingleRestriction restriction, CurrentLearn motionCheck)
        {
            //total of values
            
            
        }
        */
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

