using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using System.Linq;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System;

namespace RestrictionSystem
{
    public class RegressionSystem : SerializedMonoBehaviour
    {
        public static RegressionSystem instance;
        private void Awake() { instance = this; }
        
        [FoldoutGroup("Test")] public List<SingleFrameRestrictionValues> RestrictionValues;


        [FoldoutGroup("EngineTest")] public float[][] TestValues;
        [FoldoutGroup("EngineTest")] public int CorrectOnTrue;
        [FoldoutGroup("EngineTest")] public int CorrectOnFalse;
        [FoldoutGroup("EngineTest")] public int InCorrectOnTrue;
        [FoldoutGroup("EngineTest")] public int InCorrectOnFalse;

        //[FoldoutGroup("CoefficentStats"), ListDrawerSettings(ShowIndexLabels = true)] public Coefficents RegressionStats;
        
        [FoldoutGroup("CoefficentStats")] public int EachTotalDegree;
        //[FoldoutGroup("CoefficentStats")] public MotionRestriction UploadRestrictions;
        [FoldoutGroup("CoefficentStats"), Range(0,2)] public float LearnRate;
        [FoldoutGroup("CoefficentStats")] public float SmallestInput = 0.001f;
        [FoldoutGroup("CoefficentStats")] public double[] Coefficents;

        [FoldoutGroup("CovarianceMatrix"), ShowIf("ShouldDebug")] public double[][] Inputs;
        [FoldoutGroup("CovarianceMatrix"), ShowIf("ShouldDebug")] public double[] FirstLowMult;
        [FoldoutGroup("CovarianceMatrix"), ShowIf("ShouldDebug")] public double[] FirstHighMult;
        [FoldoutGroup("CovarianceMatrix"), ShowIf("ShouldDebug")] public double[] FirstSingleFinal;
        [FoldoutGroup("CovarianceMatrix"), ShowIf("ShouldDebug")] public double[][] FinalCovarianceMatrix;
        [FoldoutGroup("CovarianceMatrix"), ShowIf("ShouldDebug")] public double[] Predictions;

        [FoldoutGroup("IterationMatrix"), ShowIf("ShouldDebug")] public double[] LowerIteration;
        [FoldoutGroup("IterationMatrix"), ShowIf("ShouldDebug")] public double[] FinalIterationMatrix;

        [FoldoutGroup("SaveRestrictions")] public MotionRestriction RestrictionStorage;

        public static bool ShouldDebug = false;

        public delegate void DoPreformRegression();
        public static event DoPreformRegression OnPreformRegression;
        public void OriginRecalculate()
        {
            //change true/false motions
            MotionAssign.instance.PreformLock();
            //recalculate
            PreformRegression((CurrentLearn)MotionEditor.instance.MotionType);
        }
        [FoldoutGroup("Functions"), Button(ButtonSizes.Small)]
        public void PreformRegressionAll()
        {
            for (int motion = 1; motion < Enum.GetValues(typeof(CurrentLearn)).Length; motion++)
                PreformRegression((CurrentLearn)motion);
        }
        [FoldoutGroup("Functions"), Button(ButtonSizes.Small)]
        public void PreformRegressionCurrent() { PreformRegression((CurrentLearn)MotionEditor.instance.MotionType); }

        public void PreformRegression(CurrentLearn Motion)
        {
            List<SingleFrameRestrictionValues> FrameInfo = RestrictionStatManager.instance.GetRestrictionsForMotions(Motion, RestrictionManager.instance.RestrictionSettings.MotionRestrictions[(int)Motion - 1]);

            

            

            LogisticRegression Regression = new LogisticRegression(GetInputValues(FrameInfo), GetOutputValues(FrameInfo), EachTotalDegree);
            double[] Coefficents = Regression.Coefficents;
            int Iterations = Regression.Iterations;
            float CorrectPercent = Regression.CorrectPercent() * 100f;


            Debug.Log((Motion).ToString() + " is " + CorrectPercent + "% Correct at iterations: " + Iterations);

            RegressionInfo newInfo = new RegressionInfo();
            newInfo.Intercept = (float)Coefficents[0];
            newInfo.Coefficents = new List<RegressionInfo.DegreeList>();
            for (int i = 0; i < FrameInfo[0].OutputRestrictions.Count; i++)
            {
                RegressionInfo.DegreeList newDegree = new RegressionInfo.DegreeList();
                newDegree.Degrees = new List<float>();
                for (int j = 0; j < EachTotalDegree; j++)
                {
                    newDegree.Degrees.Add((float)Coefficents[(i * EachTotalDegree) + j + 1]);
                }
                newInfo.Coefficents.Add(newDegree);
            }

            RestrictionManager.instance.RestrictionSettings.Coefficents[(int)Motion - 1] = newInfo;
            OnPreformRegression?.Invoke();
        }
        public static double[][] GetInputValues(List<SingleFrameRestrictionValues> FrameInfo)//[framenum][values]
        {
            double[][] InputValues = new double[FrameInfo.Count][];//[framenum][values]
            int EachTotalDegree = RegressionSystem.instance.EachTotalDegree;
            for (int i = 0; i < FrameInfo.Count; i++)
            {
                InputValues[i] = new double[(FrameInfo[0].OutputRestrictions.Count * EachTotalDegree) + 1];
                for (int j = 0; j < FrameInfo[i].OutputRestrictions.Count; j++)
                {
                    InputValues[i][0] = 1d;
                    for (int k = 0; k < EachTotalDegree; k++)
                    {
                        double Value = Math.Pow(FrameInfo[i].OutputRestrictions[j], k + 1);
                        //if (Value < SmallestInput)
                        //Value = SmallestInput;
                        InputValues[i][(j * EachTotalDegree) + 1 + k] = Value;
                    }

                }
            }
            return InputValues;
        }
        public static double[] GetOutputValues(List<SingleFrameRestrictionValues> FrameInfo)
        {
            double[] Output = new double[FrameInfo.Count];
            for (int i = 0; i < Output.Length; i++)
                Output[i] = FrameInfo[i].AtMotionState ? 1d : 0d;
            return Output;
        }
        
        public float GetTestRegressionStats(double[] Coefficents, CurrentLearn motion)
        {
            float2 Guesses = new float2(0f, 0f);
            float2 FalseTrue = new float2(0f, 0f);

            List<SingleFrameRestrictionValues> FrameInfo = RestrictionStatManager.instance.GetRestrictionsForMotions(motion, RestrictionManager.instance.RestrictionSettings.MotionRestrictions[(int)motion - 1]);
            RestrictionValues = FrameInfo;
            for (int i = 0; i < FrameInfo.Count; i++)
            {
                double Total = Coefficents[0];
                for (int j = 0; j < FrameInfo[0].OutputRestrictions.Count; j++)//each  variable
                    for (int k = 0; k < EachTotalDegree; k++)//powers
                        Total += Mathf.Pow(FrameInfo[i].OutputRestrictions[j], k + 1) * Coefficents[(j * EachTotalDegree) + k + 1];

                //insert formula
                double GuessValue = 1f / (1f + Math.Exp(-Total));
                bool Guess = GuessValue > RestrictionManager.instance.RestrictionSettings.CutoffValues[(int)motion - 1];
                bool Truth = FrameInfo[i].AtMotionState;
                bool Correct = Guess == Truth;
                FalseTrue = new float2(FalseTrue.x + (!Truth ? 1f : 0f), FalseTrue.y + (Truth ? 1f : 0f));
                Guesses = new float2(Guesses.x + (!Correct ? 1f : 0f), Guesses.y + (Correct ? 1f : 0f));
            }
            //Debug.Log(RestrictionManager.instance.RestrictionSettings.Coefficents[1].Coefficents[0].Degrees[0]);
            return Guesses.y / (Guesses.x + Guesses.y) * 100f;
        }
        
    }


    
}
