using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using MathNet.Numerics.LinearAlgebra;

namespace RestrictionSystem
{
    public class RegressionSystem : SerializedMonoBehaviour
    {
        public static RegressionSystem instance;
        private void Awake() { instance = this; }
        

        [FoldoutGroup("Test")] public List<float> Totals = new List<float>();
        [FoldoutGroup("Test")] public List<float> OutputValues = new List<float>();
        [FoldoutGroup("Test")] public List<SingleFrameRestrictionValues> RestrictionValues;

        [FoldoutGroup("CoefficentStats"), ListDrawerSettings(ShowIndexLabels = true)] public Coefficents RegressionStats;
        [FoldoutGroup("CoefficentStats")] public CurrentLearn CurrentMotion;
        [FoldoutGroup("CoefficentStats")] public int EachTotalDegree;
        [FoldoutGroup("CoefficentStats")] public MotionRestriction UploadRestrictions;
        [FoldoutGroup("CoefficentStats")] public int2 ColumnAndRow;



        //AG11
        [FoldoutGroup("Functions"), Button(ButtonSizes.Small)]
        public void GetCoefficentsFromExcel()
        {
            Debug.Log(float.Parse(SpreadSheet.ReadExcelCell(ColumnAndRow.x, ColumnAndRow.y)));
            RegressionStats.RegressionStats[(int)CurrentMotion - 1].Intercept = float.Parse(SpreadSheet.ReadExcelCell(ColumnAndRow.x, ColumnAndRow.y));
            RegressionStats.RegressionStats[(int)CurrentMotion - 1].Coefficents.Clear();
            int AddCount = 1;
            for (int i = 0; i < UploadRestrictions.Restrictions.Count; i++)
            {
                RegressionInfo.DegreeList AddList = new RegressionInfo.DegreeList();
                AddList.Degrees = new List<float>();
                for (int j = 0; j < EachTotalDegree; j++)
                {
                    string GotString = SpreadSheet.ReadExcelCell(ColumnAndRow.x + AddCount, ColumnAndRow.y);
                    AddList.Degrees.Add(float.Parse(GotString));
                    AddCount += 1;
                }
                RegressionStats.RegressionStats[(int)CurrentMotion - 1].Coefficents.Add(AddList);
            }
        }
        public bool ControllerGuess(out float Value)
        {
            SingleInfo Frame1 = PastFrameRecorder.instance.PastFrame(Side.right);
            SingleInfo Frame2 = PastFrameRecorder.instance.GetControllerInfo(Side.right);

            List<float> TestValues = new List<float>();
            for (int i = 0; i < UploadRestrictions.Restrictions.Count; i++)
            {
                TestValues.Add(RestrictionManager.RestrictionDictionary[UploadRestrictions.Restrictions[i].restriction].Invoke(UploadRestrictions.Restrictions[i], Frame1, Frame2));
            }

            float Total = 0f;
            for (int j = 0; j < RegressionStats.RegressionStats[(int)CurrentMotion - 1].Coefficents.Count; j++)//each  variable
                for (int k = 0; k < RegressionStats.RegressionStats[(int)CurrentMotion - 1].Coefficents[j].Degrees.Count; k++)//powers
                    Total += Mathf.Pow(TestValues[j], k + 1) * RegressionStats.RegressionStats[(int)CurrentMotion - 1].Coefficents[j].Degrees[k];
            
            //Totals.Add(Total);
            Total += RegressionStats.RegressionStats[(int)CurrentMotion - 1].Intercept;
            //insert formula
            float GuessValue = 1f / (1f + Mathf.Exp(-Total));
            //OutputValues.Add(GuessValue);
            Value = GuessValue;
            bool Guess = GuessValue > 0.5f;
            bool Correct = Guess;
            return Correct;
        }
        
        [FoldoutGroup("Functions"), Button(ButtonSizes.Small)]
        public void TestRegression()
        {
            float2 Guesses = new float2(0f,0f);

            int MotionNum = (int)CurrentMotion - 1;
            List<SingleFrameRestrictionValues> FrameInfo = BruteForce.instance.GetRestrictionsForMotions(CurrentMotion, UploadRestrictions);
            RestrictionValues = FrameInfo;
            for (int i = 0; i < FrameInfo.Count; i++)
            {
                float Total = 0f;
                for (int j = 0; j < RegressionStats.RegressionStats[MotionNum].Coefficents.Count; j++)//each  variable
                    for (int k = 0; k < RegressionStats.RegressionStats[MotionNum].Coefficents[j].Degrees.Count; k++)//powers
                        Total += Mathf.Pow(FrameInfo[i].OutputRestrictions[j], k + 1) * RegressionStats.RegressionStats[MotionNum].Coefficents[j].Degrees[k];
                
                Totals.Add(Total);
                Total += RegressionStats.RegressionStats[MotionNum].Intercept;
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
        
    }
    



}
/*
        [FoldoutGroup("Functions"), Button(ButtonSizes.Small)]
        public void GetCoefficents()
        {
            List<SingleFrameRestrictionValues> FrameInfo = BruteForce.instance.GetRestrictionsForMotions(BruteForce.instance.motionGet, RestrictionManager.instance.RestrictionSettings.MotionRestrictions[(int)BruteForce.instance.motionGet - 1]);

            // Initialize a matrix
            Matrix<double> X = Matrix<double>.Build.Dense(FrameInfo.Count, FrameInfo[0].OutputRestrictions.Count, 0);
            Vector<double> Y = Vector<double>.Build.Dense(FrameInfo.Count);
            for (int i = 0; i < FrameInfo.Count; i++)
            {
                Y[i] = FrameInfo[i].AtMotionState ? 1 : 0;
                for (int j = 0; j < FrameInfo[0].OutputRestrictions.Count; j++)
                {
                    X[i, j] = FrameInfo[i].OutputRestrictions[j];
                }
            }


            // Perform multiple linear regression
            Matrix<double> XTX = X.Transpose() * X;
            Matrix<double> XTXInverse = XTX.Inverse();
            Matrix<double> XTY = X.Transpose() * Y;
            Vector<double> coefficients = XTXInverse * XTY;

            // Convert the coefficients vector to a matrix
            Matrix<double> coefficientsMatrix = coefficients.ToColumnMatrix();
            Debug.Log("Coefficients: " + coefficientsMatrix);
        }
        */
