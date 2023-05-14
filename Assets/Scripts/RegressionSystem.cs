using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using System.Linq;
using System;

namespace RestrictionSystem
{
    public class RegressionSystem : SerializedMonoBehaviour
    {
        public static RegressionSystem instance;
        private void Awake() { instance = this; }
        
        [FoldoutGroup("Test")] public List<SingleFrameRestrictionValues> RestrictionValues;
        
        [FoldoutGroup("CoefficentStats")] public int EachTotalDegree;
        //[FoldoutGroup("CoefficentStats")] public MotionRestriction UploadRestrictions;
        [FoldoutGroup("CoefficentStats"), Range(0,2)] public float LearnRate;
        [FoldoutGroup("CoefficentStats")] public float SmallestInput = 0.001f;
        [FoldoutGroup("CoefficentStats")] public double[] Coefficents;

        [FoldoutGroup("IterationMatrix"), ShowIf("ShouldDebug")] public double[] LowerIteration;
        [FoldoutGroup("IterationMatrix"), ShowIf("ShouldDebug")] public double[] FinalIterationMatrix;

        [FoldoutGroup("SaveRestrictions")] public MotionRestriction RestrictionStorage;

        public static bool ShouldDebug = false;

        [FoldoutGroup("MultiThreading")] public int Threads;

        public delegate void DoPreformRegression();
        public static event DoPreformRegression OnPreformRegression;

        [FoldoutGroup("GetInfoFunctions"), Button(ButtonSizes.Small)]
        public void GetTotalFrames()
        {
            int2 TrueFalseCount = 0;
            foreach (Motion motion in LearnManager.instance.MovementList[(int)MotionEditor.instance.MotionType].Motions)
                for (int i = 0; i < motion.Infos.Count; i++)
                    TrueFalseCount = new int2(TrueFalseCount.x + (motion.AtFrameState(i) ? 1 : 0), TrueFalseCount.y + (!motion.AtFrameState(i) ? 1 : 0));
            Debug.Log("TotalFrames: " + (TrueFalseCount.x + TrueFalseCount.y));
        }
        [FoldoutGroup("Functions"), Button(ButtonSizes.Small)]public void TestCurrent() { MotionEditor.instance.TestCurrentButton(); }

        [FoldoutGroup("Functions"), Button(ButtonSizes.Small)]
        public void OriginRecalculate()
        {
            //change true/false motions
            MotionAssign.instance.GetTrueMotions();
            MotionAssign.instance.PreformLock();
            ConditionTester.instance.CalculateCoefficents();
            //recalculate
            PreformRegression(MotionEditor.instance.MotionType);
            MotionEditor.instance.TestCurrentButton();
        }
        [FoldoutGroup("Functions"), Button(ButtonSizes.Small)]
        public void RestrictionRecalculate()
        {
            //change true/false motions
            MotionAssign.instance.GetTrueMotions();
            MotionAssign.instance.PreformLock();
            PreformRegression(MotionEditor.instance.MotionType);
            MotionEditor.instance.TestCurrentButton();
            //recalculate

        }
        [FoldoutGroup("Functions"), Button(ButtonSizes.Small)]
        public void ConditionRecalculate()
        {
            //change true/false motions
            MotionAssign.instance.GetTrueMotions();
            MotionAssign.instance.PreformLock();
            ConditionTester.instance.CalculateCoefficents();

        }
        [FoldoutGroup("Functions"), Button(ButtonSizes.Small)]
        public void PreformRegressionAll()
        {
            for (int motion = 1; motion < Enum.GetValues(typeof(MotionState)).Length; motion++)
                PreformRegression((MotionState)motion);
        }
        [FoldoutGroup("Functions"), Button(ButtonSizes.Small)]
        public void PreformRegressionCurrent() { PreformRegression((MotionState)MotionEditor.instance.MotionType); }

        public void PreformRegression(MotionState Motion)
        {
            List<SingleFrameRestrictionValues> FrameInfo = RestrictionStatManager.instance.GetRestrictionsForMotions(Motion, RestrictionManager.instance.RestrictionSettings.MotionRestrictions[(int)Motion - 1]);

            LogisticRegression Regression = new LogisticRegression(GetInputValues(FrameInfo), GetOutputValues(FrameInfo), EachTotalDegree);

            double[] Coefficents = Regression.Coefficents;
            int Iterations = Regression.Iterations;
            float CorrectPercent = Regression.CorrectPercent();
            //myStruct = default(MyStruct);

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
            for (int i = 0; i < FrameInfo.Count; i++)
                InputValues[i] = FrameInfo[i].OutputRestrictions.Select(x => (double)x).ToArray();
            return InputValues;
        }
        public static double[] GetOutputValues(List<SingleFrameRestrictionValues> FrameInfo)
        {
            double[] Output = new double[FrameInfo.Count];
            for (int i = 0; i < Output.Length; i++)
            {
                Output[i] = FrameInfo[i].AtMotionState ? 1d : 0d;
            }
            return Output;
        }
    }
}
