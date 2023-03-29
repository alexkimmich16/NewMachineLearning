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
        

        [FoldoutGroup("Test")] public List<float> Totals = new List<float>();
        [FoldoutGroup("Test")] public List<float> OutputValues = new List<float>();
        [FoldoutGroup("Test")] public List<SingleFrameRestrictionValues> RestrictionValues;


        [FoldoutGroup("EngineTest")] public float[][] TestValues;
        [FoldoutGroup("EngineTest")] public int CorrectOnTrue;
        [FoldoutGroup("EngineTest")] public int CorrectOnFalse;
        [FoldoutGroup("EngineTest")] public int InCorrectOnTrue;
        [FoldoutGroup("EngineTest")] public int InCorrectOnFalse;

        [FoldoutGroup("CoefficentStats"), ListDrawerSettings(ShowIndexLabels = true)] public Coefficents RegressionStats;
        
        [FoldoutGroup("CoefficentStats")] public int EachTotalDegree;
        [FoldoutGroup("CoefficentStats")] public MotionRestriction UploadRestrictions;
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

        public static bool ShouldDebug = false;
        public CurrentLearn CurrentMotion;
        
        [FoldoutGroup("Functions"), Button(ButtonSizes.Small)]
        public void PreformRegression()
        {
            for (int motion = 1; motion < Enum.GetValues(typeof(CurrentLearn)).Length; motion++)
            {
                List<SingleFrameRestrictionValues> FrameInfo = BruteForce.instance.GetRestrictionsForMotions((CurrentLearn)motion, UploadRestrictions);

                double[][] InputValues = new double[FrameInfo.Count][];
                for (int i = 0; i < FrameInfo.Count; i++)
                {
                    InputValues[i] = new double[(FrameInfo[0].OutputRestrictions.Count * EachTotalDegree) + 1];
                    for (int j = 0; j < FrameInfo[i].OutputRestrictions.Count; j++)
                    {
                        InputValues[i][0] = 1d;
                        for (int k = 0; k < EachTotalDegree; k++)
                        {
                            double Value = Math.Pow(FrameInfo[i].OutputRestrictions[j], k + 1);
                            if (Value < SmallestInput)
                                Value = SmallestInput;
                            InputValues[i][((j * EachTotalDegree) + 1) + k] = Value;
                        }

                    }
                }
                //Inputs = InputValues;
                //FrameInfo.Select(x => x.AtMotionState ? 1d : 0d).ToArray();
                double[] Output = new double[FrameInfo.Count];
                for (int i = 0; i < Output.Length; i++)
                    Output[i] = FrameInfo[i].AtMotionState ? 1d : 0d;

                double[] Coefficents = new double[(FrameInfo[0].OutputRestrictions.Count * EachTotalDegree) + 1];
                
                int Iterations = 0;
                float CorrectPercent = 0f;
                while (Iterations < 20)
                {
                    double[] Predictions = GetPredictions(InputValues, Coefficents);
                    double[][] CovarianceMatrix = GetCovarianceMatrix(InputValues, Predictions, Iterations >= 2);
                    double[] IterationMatrix = GetIterationMatrix(InputValues, CovarianceMatrix, Output, Predictions);

                    if (CorrectPercent > GetTestRegressionStats(GetCoefficents(Coefficents, IterationMatrix), (CurrentLearn)motion)) //check for decrease
                        break;

                    Coefficents = GetCoefficents(Coefficents, IterationMatrix);
                    CorrectPercent = GetTestRegressionStats(Coefficents, (CurrentLearn)motion);
                    //Debug.Log(CorrectPercent + "% Correct");
                    Iterations += 1;
                }
                Debug.Log(((CurrentLearn)motion).ToString() + " is " +  + CorrectPercent + "% Correct");

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
                RestrictionManager.instance.coefficents.RegressionStats[motion - 1] = newInfo;

                //Coefficents = new double[(FrameInfo[0].OutputRestrictions.Count * EachTotalDegree) + 1];

                //Debug.Log("Iterations: " + Iterations);
                //Debug.Log((CorrectPercent * 100) + "% Correct" + "  Where False= " + ((FalseTrue.x / (FalseTrue.x + FalseTrue.y)) * 100f) + "%");
            }



            double[] GetPredictions(double[][] Values, double[] Coefficents)
            {
                double[] ReturnValue = new double[Values.Length];
                for (int i = 0; i < Values.Length; i++)
                {
                    double Total = -Coefficents[0];
                    for (int j = 1; j < Coefficents.Length; j++)
                        Total -= Values[i][j] * Coefficents[j];
                    //if (i == 0)
                        //Debug.Log(Total);

                    ReturnValue[i] = 1d / (1d + Math.Exp(Total));
                }
                if(ShouldDebug)
                    Predictions = ReturnValue;
                return ReturnValue;
            }
            double[][] GetCovarianceMatrix(double[][] Input, double[] Predictions, bool IsNewType)
            {
                //=MINVERSE(MMULT(TRANSPOSE(DEsign(A2:AA7000) * AG2:AG7000*(1-AG2:AG7000)),DEsign($A$2:$AA$7000)))
                //=MINVERSE(MMULT([1],DEsign($A$2:$AA$7000)))
                //[1] = TRANSPOSE(DEsign(A2:AA7000) * AG2:AG7000*(1-AG2:AG7000))

                DenseMatrix X = DenseMatrix.OfRowArrays(Input);
                DenseVector Y = DenseVector.OfArray(Predictions);

                DenseMatrix LowerMMult;
                if (IsNewType)
                {
                    LowerMMult = DenseMatrix.Create(Predictions.Length, Input[0].Length, 0);
                    for (int i = 0; i < LowerMMult.RowCount; i++)//apply prediction to each
                        for (int j = 0; j < LowerMMult.ColumnCount; j++)
                            LowerMMult[i, j] = Predictions[i] * (1d - Predictions[i]) * Input[i][j];
                }
                else
                {
                    //second = DIAGONAL(AE2:AE7000*(1-AE2:AE7000))
                    DenseMatrix Second = DenseMatrix.Create(Y.Count, Y.Count, 0);
                    for (int i = 0; i < Y.Count; i++)
                        Second[i, i] = Y[i] * (1d - Y[i]);
                    LowerMMult = (DenseMatrix)Second.Multiply(X);
                }
                if (ShouldDebug)
                    FirstLowMult = LowerMMult.Column(2).ToArray();
                
                //Debug.Log(FirstLowMult.Sum());
                //Debug.Log("For Lower:: " + "Columns: " + LowerMMult.ColumnCount + "  Rows: " + LowerMMult.RowCount);


                DenseMatrix XTransposed = (DenseMatrix)X.Transpose();
                DenseMatrix HigherMMult = (DenseMatrix)XTransposed.Multiply(LowerMMult);
                //Debug.Log("For Higher:: " + "Columns: " + HigherMMult.ColumnCount + "  Rows: " + HigherMMult.RowCount);
                DenseMatrix Final = (DenseMatrix)HigherMMult.Inverse();
                if (ShouldDebug)
                {
                    FirstHighMult = HigherMMult.Row(0).ToArray();
                    FirstSingleFinal = Final.Row(0).ToArray();
                    FinalCovarianceMatrix = To2DArray(Final);
                }
                    
                return To2DArray(Final);
            }
            double[] GetIterationMatrix(double[][] Input, double[][] CovarianceMatrix, double[] Outputs, double[] Predictions)
            {
                Matrix<double> X = Matrix<double>.Build.DenseOfColumnArrays(Input);

                //=MMULT(AK11:BL38,MMULT(TRANSPOSE(DEsign(A$2:AA$7000)),($AB$2:$AB$7000-AE$2:AE$7000)))

                //=MMULT(AK11:BL38,MMULT([1],[2]))
                //[1] = TRANSPOSE(DEsign(A$2:AA$7000))
                //[2] = ($AB$2:$AB$7000-AE$2:AE$7000)

                Matrix<double> First = X.Transpose();
                Matrix<double> Second = Matrix<double>.Build.Dense(1, Outputs.Length, 0);
                for (int i = 0; i < Second.ColumnCount; i++)
                    Second[0, i] = Outputs[i] - Predictions[i];

                Matrix<double> LowerMMult = Second.Multiply(First);
                if (ShouldDebug)
                    LowerIteration = LowerMMult.Row(0).ToArray();

                Matrix<double> HigherMMult = LowerMMult.Multiply(Matrix<double>.Build.DenseOfRowArrays(CovarianceMatrix));
                if (ShouldDebug)
                    FinalIterationMatrix = HigherMMult.Row(0).AsArray();
                return HigherMMult.Row(0).AsArray();
            }
            double[] GetCoefficents(double[] PastCoefficents, double[] IterationMatrix)
            {
                double[] NewCoefficents = new double[PastCoefficents.Length];
                for (int i = 0; i < NewCoefficents.Length; i++)
                    NewCoefficents[i] = PastCoefficents[i] + (IterationMatrix[i] * LearnRate);
                return NewCoefficents;
            }

            double[][] To2DArray(Matrix<double> Input)
            {
                double[][] columns = new double[Input.ColumnCount][];
                for (int j = 0; j < Input.ColumnCount; j++)
                {
                    double[] column = new double[Input.RowCount];
                    for (int i = 0; i < Input.RowCount; i++)
                        column[i] = Input[i, j];
                    columns[j] = column;
                }
                return columns;
            }
        }
        public float GetTestRegressionStats(double[] Coefficents, CurrentLearn motion)
        {
            float2 Guesses = new float2(0f, 0f);
            float2 FalseTrue = new float2(0f, 0f);

            List<SingleFrameRestrictionValues> FrameInfo = BruteForce.instance.GetRestrictionsForMotions(motion, UploadRestrictions);
            RestrictionValues = FrameInfo;
            for (int i = 0; i < FrameInfo.Count; i++)
            {
                double Total = Coefficents[0];
                for (int j = 0; j < FrameInfo[0].OutputRestrictions.Count; j++)//each  variable
                    for (int k = 0; k < EachTotalDegree; k++)//powers
                        Total += Mathf.Pow(FrameInfo[i].OutputRestrictions[j], k + 1) * Coefficents[(j * EachTotalDegree) + k + 1];

                //insert formula
                double GuessValue = 1f / (1f + Math.Exp(-Total));
                bool Guess = GuessValue > 0.5f;
                bool Truth = FrameInfo[i].AtMotionState;
                bool Correct = Guess == Truth;
                FalseTrue = new float2(FalseTrue.x + (!Truth ? 1f : 0f), FalseTrue.y + (Truth ? 1f : 0f));
                Guesses = new float2(Guesses.x + (!Correct ? 1f : 0f), Guesses.y + (Correct ? 1f : 0f));
            }
            return Guesses.y / (Guesses.x + Guesses.y) * 100f;
        }
    }
}
