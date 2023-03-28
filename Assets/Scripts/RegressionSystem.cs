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
        [FoldoutGroup("CoefficentStats")] public Single[] Coefficents;

        [FoldoutGroup("CovarianceMatrix")] public Single[] MatrixCheck;
        [FoldoutGroup("CovarianceMatrix")] public Single[] FirstLowMult;
        [FoldoutGroup("CovarianceMatrix")] public Single[] FirstHighMult;
        [FoldoutGroup("CovarianceMatrix")] public Single[] PreDiagonal;
        [FoldoutGroup("CovarianceMatrix")] public Single[] FirstSingleDiagonal;
        [FoldoutGroup("CovarianceMatrix")] public Single[] XTransposedDisplay;
        [FoldoutGroup("CovarianceMatrix")] public Single[] FirstSingleFinal;
        [FoldoutGroup("CovarianceMatrix")] public Single[][] FinalCovarianceMatrix;
        [FoldoutGroup("CovarianceMatrix")] public Single[] Predictions;

        [FoldoutGroup("IterationMatrix")] public Single[] LowerIteration;
        [FoldoutGroup("IterationMatrix")] public Single[] HigherIteration;
        [FoldoutGroup("IterationMatrix")] public Single[] FinalIterationMatrix;


        //[FoldoutGroup("Excel")] public int2 ColumnAndRow;
        public CurrentLearn CurrentMotion;


        
        
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
        public void TestRegressionStats(Single[] Coefficents)
        {
            float2 Guesses = new float2(0f, 0f);
            float2 FalseTrue = new float2(0f, 0f);

            int MotionNum = (int)CurrentMotion - 1;
            List<SingleFrameRestrictionValues> FrameInfo = BruteForce.instance.GetRestrictionsForMotions(CurrentMotion, UploadRestrictions);
            RestrictionValues = FrameInfo;
            for (int i = 0; i < FrameInfo.Count; i++)
            {
                Single Total = Coefficents[0];
                for (int j = 0; j < FrameInfo[0].OutputRestrictions.Count; j++)//each  variable
                    for (int k = 0; k < EachTotalDegree; k++)//powers
                        Total += Mathf.Pow(FrameInfo[i].OutputRestrictions[j], k + 1) * Coefficents[(j * EachTotalDegree) + k + 1];

                //insert formula
                Single GuessValue = 1f / (1f + Mathf.Exp(-Total));
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

            Single[][] InputValues = new Single[FrameInfo.Count][];
            for (int i = 0; i < FrameInfo.Count; i++)
            {
                InputValues[i] = new Single[(FrameInfo[0].OutputRestrictions.Count * EachTotalDegree) + 1];
                for (int j = 0; j < FrameInfo[i].OutputRestrictions.Count; j++)
                {
                    InputValues[i][0] = 1f;
                    for (int k = 0; k < EachTotalDegree; k++)
                        InputValues[i][((j * EachTotalDegree) + 1) + k] = Mathf.Pow(FrameInfo[i].OutputRestrictions[j], k + 1);
                }
            } 

            Single[] Output = new Single[FrameInfo.Count];
            for (int i = 0; i < Output.Length; i++)
                Output[i] = FrameInfo[i].AtMotionState ? 1f : 0f;

            Coefficents = new Single[(FrameInfo[0].OutputRestrictions.Count * EachTotalDegree) + 1];
            for (int i = 0; i < Iterations; i++)
            {
                Single[] Predictions = GetPredictions(InputValues, Coefficents);
                Single[][] CovarianceMatrix = GetCovarianceMatrix(InputValues, Predictions, i >= 2);
                Single[] IterationMatrix = GetIterationMatrix(InputValues, CovarianceMatrix, Output, Predictions);
                Coefficents = GetCoefficents(Coefficents, IterationMatrix);
            }
            TestRegressionStats(Coefficents);




            Single[] GetPredictions(Single[][] Values, Single[] Coefficents)
            {
                Single[] ReturnValue = new Single[Values.Length];
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


                    ReturnValue[i] = 1f / (1f + Mathf.Exp(Total));
                }
                Predictions = ReturnValue;
                return ReturnValue;
            }
            Single[][] GetCovarianceMatrix(Single[][] Input, Single[] Predictions, bool IsNewType)
            {
                //=MINVERSE(MMULT(TRANSPOSE(DEsign(A2:AA7000) * AG2:AG7000*(1-AG2:AG7000)),DEsign($A$2:$AA$7000)))
                //=MINVERSE(MMULT([1],DEsign($A$2:$AA$7000)))
                //[1] = TRANSPOSE(DEsign(A2:AA7000) * AG2:AG7000*(1-AG2:AG7000))
                Matrix<Single> X = Matrix<Single>.Build.DenseOfRowArrays(Input);
                Vector<Single> Y = Vector<Single>.Build.DenseOfArray(Predictions);

                MatrixCheck = X.Row(0).AsArray();

                Matrix<Single> LowerMMult;
                if (IsNewType)
                {
                    LowerMMult = Matrix<Single>.Build.Dense(Predictions.Length, Input[0].Length, 0);
                    for (int i = 0; i < LowerMMult.RowCount; i++)//apply prediction to each
                        for (int j = 0; j < LowerMMult.ColumnCount; j++)
                            LowerMMult[i, j] = Predictions[i] * (1f - Predictions[i]) * Input[i][j];
                }
                else
                {
                    //second = DIAGONAL(AE2:AE7000*(1-AE2:AE7000))
                    Matrix<Single> Second = Matrix<Single>.Build.Dense(Y.Count, Y.Count, 0);
                    for (int i = 0; i < Y.Count; i++)
                        Second[i, i] = Y[i] * (1f - Y[i]);
                    FirstSingleDiagonal = Second.Column(0).ToArray();
                    LowerMMult = Second.Multiply(X);
                }
                
                FirstLowMult = LowerMMult.Row(0).ToArray();
                //Debug.Log("For Lower:: " + "Columns: " + LowerMMult.ColumnCount + "  Rows: " + LowerMMult.RowCount);


                Matrix<Single> XTransposed = X.Transpose();
                XTransposedDisplay = XTransposed.Row(1).ToArray();

                Matrix<Single> HigherMMult = XTransposed.Multiply(LowerMMult);
                //Debug.Log("For Higher:: " + "Columns: " + HigherMMult.ColumnCount + "  Rows: " + HigherMMult.RowCount);
                Matrix<Single> Final = HigherMMult.Inverse();

                FirstHighMult = HigherMMult.Row(0).ToArray();
                FirstSingleFinal = Final.Row(0).ToArray();
                FinalCovarianceMatrix = To2DArray(Final);

                return To2DArray(Final);
            }
            Single[] GetIterationMatrix(Single[][] Input, Single[][] CovarianceMatrix, Single[] Outputs, Single[] Predictions)
            {
                Matrix<Single> X = Matrix<Single>.Build.DenseOfColumnArrays(Input);

                //=MMULT(AK11:BL38,MMULT(TRANSPOSE(DEsign(A$2:AA$7000)),($AB$2:$AB$7000-AE$2:AE$7000)))

                //=MMULT(AK11:BL38,MMULT([1],[2]))
                //[1] = TRANSPOSE(DEsign(A$2:AA$7000))
                //[2] = ($AB$2:$AB$7000-AE$2:AE$7000)

                Matrix<Single> First = X.Transpose();
                Matrix<Single> Second = Matrix<Single>.Build.Dense(1, Outputs.Length, 0);
                for (int i = 0; i < Second.ColumnCount; i++)
                    Second[0, i] = Outputs[i] - Predictions[i];

                Matrix<Single> LowerMMult = Second.Multiply(First);

                LowerIteration = LowerMMult.Row(0).ToArray();

                Matrix<Single> HigherMMult = LowerMMult.Multiply(Matrix<Single>.Build.DenseOfRowArrays(CovarianceMatrix));
                FinalIterationMatrix = HigherMMult.Row(0).AsArray();
                return HigherMMult.Row(0).AsArray();
            }
            Single[] GetCoefficents(Single[] PastCoefficents, Single[] IterationMatrix)
            {
                Single[] NewCoefficents = new Single[PastCoefficents.Length];
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


            Single[][] To2DArray(Matrix<Single> Input)
            {
                Single[][] columns = new Single[Input.ColumnCount][];
                for (int j = 0; j < Input.ColumnCount; j++)
                {
                    Single[] column = new Single[Input.RowCount];
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