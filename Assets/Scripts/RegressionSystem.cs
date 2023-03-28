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
        [FoldoutGroup("CoefficentStats")] public int Iterations;
        [FoldoutGroup("CoefficentStats")] public double[] Coefficents;

        [FoldoutGroup("CovarianceMatrix")] public double[] MatrixCheck;
        [FoldoutGroup("CovarianceMatrix")] public double[] FirstLowMult;
        [FoldoutGroup("CovarianceMatrix")] public double[] FirstHighMult;
        [FoldoutGroup("CovarianceMatrix")] public double[] PreDiagonal;
        [FoldoutGroup("CovarianceMatrix")] public double[] FirstSingleDiagonal;
        [FoldoutGroup("CovarianceMatrix")] public double[] FirstSingleFinal;
        [FoldoutGroup("CovarianceMatrix")] public double[][] FinalCovarianceMatrix;
        [FoldoutGroup("CovarianceMatrix")] public double[] Predictions;

        [FoldoutGroup("IterationMatrix")] public double[] LowerIteration;
        [FoldoutGroup("IterationMatrix")] public double[] HigherIteration;
        [FoldoutGroup("IterationMatrix")] public double[] FinalIterationMatrix;


        //[FoldoutGroup("Excel")] public int2 ColumnAndRow;
        public CurrentLearn CurrentMotion;



        //AG11
        //[FoldoutGroup("Functions"), Button(ButtonSizes.Small)]
        /*
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
        */
        //[FoldoutGroup("Functions"), Button(ButtonSizes.Small)]

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
        public void TestRegressionStats(double[] Coefficents)
        {
            float2 Guesses = new float2(0f, 0f);
            float2 FalseTrue = new float2(0f, 0f);

            int MotionNum = (int)CurrentMotion - 1;
            List<SingleFrameRestrictionValues> FrameInfo = BruteForce.instance.GetRestrictionsForMotions(CurrentMotion, UploadRestrictions);
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
            float CorrectPercent = Guesses.y / (Guesses.x + Guesses.y);
            Debug.Log((CorrectPercent * 100) + "% Correct" + "  Where False= " + ((FalseTrue.x / (FalseTrue.x + FalseTrue.y)) * 100f) + "%");
        }

        
        [FoldoutGroup("Functions"), Button(ButtonSizes.Small)]
        public void PreformRegression()
        {

            List<SingleFrameRestrictionValues> FrameInfo = BruteForce.instance.GetRestrictionsForMotions(CurrentMotion, UploadRestrictions);
            
            FrameInfo.RemoveRange(7000, FrameInfo.Count - 7000);
            //Debug.Log(FrameInfo.Count);

            double[][] InputValues = new double[FrameInfo.Count][];
            for (int i = 0; i < FrameInfo.Count; i++)
            {
                InputValues[i] = new double[(FrameInfo[0].OutputRestrictions.Count * EachTotalDegree) + 1];
                for (int j = 0; j < FrameInfo[i].OutputRestrictions.Count; j++)
                {
                    InputValues[i][0] = 1d;
                    for (int k = 0; k < EachTotalDegree; k++)
                        InputValues[i][((j * EachTotalDegree) + 1) + k] = Math.Pow(FrameInfo[i].OutputRestrictions[j], k + 1);
                }
            } 

            double[] Output = new double[FrameInfo.Count];
            for (int i = 0; i < Output.Length; i++)
                Output[i] = FrameInfo[i].AtMotionState ? 1d : 0d;

            Coefficents = new double[(FrameInfo[0].OutputRestrictions.Count * EachTotalDegree) + 1];
            for (int i = 0; i < Iterations; i++)
            {
                double[] Predictions = GetPredictions(InputValues, Coefficents);
                double[][] CovarianceMatrix = GetCovarianceMatrix(InputValues, Predictions, i >= 2);
                double[] IterationMatrix = GetIterationMatrix(InputValues, CovarianceMatrix, Output, Predictions);
                Coefficents = GetCoefficents(Coefficents, IterationMatrix);
            }
            TestRegressionStats(Coefficents);




            double[] GetPredictions(double[][] Values, double[] Coefficents)
            {
                double[] ReturnValue = new double[Values.Length];
                for (int i = 0; i < Values.Length; i++)
                {
                    float Total = (float)-Coefficents[0];
                    for (int j = 1; j < Coefficents.Length - 1; j++)
                    {
                        if (i == 0)
                        {
                            //Debug.Log("Change: " + (Values[i][j]) + " * " + Coefficents[j]);
                            Total -= (float)(Values[i][j] * Coefficents[j]);
                        }
                    }
                        

                    if (i == 0)
                    {
                        Debug.Log(Total);
                    }


                    ReturnValue[i] = 1d / (1d + Mathf.Exp((float)Total));
                }
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

                MatrixCheck = X.Row(0).AsArray();

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
                    FirstSingleDiagonal = Second.Column(0).ToArray();
                    LowerMMult = (DenseMatrix)Second.Multiply(X);
                }
                
                FirstLowMult = LowerMMult.Row(0).ToArray();
                //Debug.Log("For Lower:: " + "Columns: " + LowerMMult.ColumnCount + "  Rows: " + LowerMMult.RowCount);


                DenseMatrix HigherMMult = (DenseMatrix)X.Transpose().Multiply(LowerMMult);
                //Debug.Log("For Higher:: " + "Columns: " + HigherMMult.ColumnCount + "  Rows: " + HigherMMult.RowCount);
                DenseMatrix Final = (DenseMatrix)HigherMMult.Inverse();

                FirstHighMult = HigherMMult.Row(0).ToArray();
                FirstSingleFinal = Final.Row(0).ToArray();
                FinalCovarianceMatrix = To2DArray(Final);

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

                LowerIteration = LowerMMult.Row(0).ToArray();

                Matrix<double> HigherMMult = LowerMMult.Multiply(Matrix<double>.Build.DenseOfRowArrays(CovarianceMatrix));
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
            
            

            /*
            int CoefficentCount = (FrameInfo[0].OutputRestrictions.Count * EachTotalDegree) + 1;
            Debug.Log("0,0 is: " + inverse[0, 0]);
            //ConverageMatrix = inverse.AsColumnArrays();
            FirstSingleFinal = inverse.Row(0).AsArray();
            */
            //[1] = TRANSPOSE(DEsign(A2:AA7000)
            //[2] = DIAGONAL(AE2:AE7000*(1-AE2:AE7000))
            //=MINVERSE(MMULT([1],MMULT([2],DEsign(A2:AA7000))))


            //=MINVERSE(MMULT(TRANSPOSE(DEsign(A2:AA7000)),
            //MMULT(
            //DIAGONAL(AE2:AE7000*(1-AE2:AE7000)),
            //DEsign(A2:AA7000))))


            double[][] To2DArray(Matrix<double> Input)
            {
                double[][] columns = new double[Input.ColumnCount][];
                for (int j = 0; j < Input.ColumnCount; j++)
                {
                    double[] column = new double[Input.RowCount];
                    for (int i = 0; i < Input.RowCount; i++)
                    {
                        column[i] = Input[i, j];
                    }
                    columns[j] = column;
                }
                return columns;
            }
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
