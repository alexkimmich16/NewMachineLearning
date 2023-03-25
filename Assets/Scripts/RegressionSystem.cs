using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using System.Linq;
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


        [FoldoutGroup("EngineTest")] public float[][] TestValues;
        [FoldoutGroup("EngineTest")] public int CorrectOnTrue;
        [FoldoutGroup("EngineTest")] public int CorrectOnFalse;
        [FoldoutGroup("EngineTest")] public int InCorrectOnTrue;
        [FoldoutGroup("EngineTest")] public int InCorrectOnFalse;

        

        [FoldoutGroup("CoefficentStats"), ListDrawerSettings(ShowIndexLabels = true)] public Coefficents RegressionStats;
        
        [FoldoutGroup("CoefficentStats")] public int EachTotalDegree;
        [FoldoutGroup("CoefficentStats")] public MotionRestriction UploadRestrictions;
        [FoldoutGroup("CoefficentStats")] public float LearnRate;
        [FoldoutGroup("CoefficentStats")] public int Iterations;
        [FoldoutGroup("CoefficentStats")] public float[] Coefficents;

        [FoldoutGroup("CoefficentStats")] public double[][] ConverageMatrix;
        [FoldoutGroup("CoefficentStats")] public double[][] InverseDisplay;
        [FoldoutGroup("CoefficentStats")] public double[][] InputValuesDisplay;

        [FoldoutGroup("CoefficentStats")] public double[][] FirstDisplay;
        [FoldoutGroup("CoefficentStats")] public double[][] SecondDisplay;
        [FoldoutGroup("CoefficentStats")] public double[][] ThirdDisplay;


        [FoldoutGroup("Excel")] public int2 ColumnAndRow;
        [FoldoutGroup("Excel")] public CurrentLearn CurrentMotion;



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
        [FoldoutGroup("Functions"), Button(ButtonSizes.Small)]
        public void PreformRegression()
        {
            // Define the input matrices
            List<SingleFrameRestrictionValues> FrameInfo = BruteForce.instance.GetRestrictionsForMotions(CurrentMotion, UploadRestrictions);

            double[][] InputValues = new double[FrameInfo.Count][];
            for (int i = 0; i < FrameInfo.Count; i++)
                InputValues[i] = new double[FrameInfo[0].OutputRestrictions.Count * EachTotalDegree];

            for (int i = 0; i < FrameInfo.Count; i++)
                for (int j = 0; j < FrameInfo[i].OutputRestrictions.Count; j++)
                    for (int k = 0; k < EachTotalDegree; k++)
                        InputValues[i][(j * EachTotalDegree) + k] = Mathf.Pow(FrameInfo[i].OutputRestrictions[j], k + 1);
                        
            InputValuesDisplay = InputValues;

            double[] Output = new double[FrameInfo.Count];
            for (int i = 0; i < Output.Length; i++)
                Output[i] = FrameInfo[i].AtMotionState ? 0 : 1;

            Matrix<double> X = Matrix<double>.Build.DenseOfColumnArrays(InputValues);
            Vector<double> Y = Vector<double>.Build.DenseOfArray(Output);

            Matrix<double> First = X.Transpose();

            //inverse
            Matrix<double> Inverse = Y.ToColumnMatrix().Inverse();
            InverseDisplay = Inverse.AsColumnArrays();
            Matrix<double> Second = Matrix<double>.Build.DenseOfDiagonalArray(Inverse.ToArray());
            SecondDisplay = Second.AsColumnArrays();
            ///1 Matrix<double> Second = Matrix<double>.Build.DenseOfDiagonalVector(Inverse);
            //2Matrix<double> Second = Matrix<double>.Build.DenseIdentity(CoefficentCount) * Y.ToRowMatrix();
            //Second.SetDiagonal(Y.PointwiseMultiply(1 - Y).ToArray());

            //Vector<double> Second = Inverse.SetDiagonal(Y.PointwiseMultiply(1 - Y).ToArray());

            //[1] = TRANSPOSE(DEsign(A2:AA7000)
            //[2] = DIAGONAL(AE2:AE7000*(1-AE2:AE7000))
            //=MINVERSE(MMULT([1],MMULT([2],DEsign(A2:AA7000))))


            //=MINVERSE(MMULT(TRANSPOSE(DEsign(A2:AA7000)),
            //MMULT(
            //DIAGONAL(AE2:AE7000*(1-AE2:AE7000)),
            //DEsign(A2:AA7000))))

            Matrix<double> LowerMMult = X.Multiply(Second);
            Matrix<double> HigherMMult = LowerMMult.Multiply(First);
            Matrix<double> inverse = HigherMMult.Inverse();

            int CoefficentCount = (FrameInfo[0].OutputRestrictions.Count * EachTotalDegree) + 1;
            Debug.Log("0,0 is: " + inverse[0, 0]);
            ConverageMatrix = inverse.AsColumnArrays();

            /*
            Matrix<double> transposeA = X.Transpose();
            Matrix<double> diagonalAE = Matrix<double>.Build.DenseIdentity(CoefficentCount) * Y.ToRowMatrix();
            diagonalAE.SetDiagonal(Y.PointwiseMultiply(1 - Y).ToArray());

            Matrix<double> product = transposeA * diagonalAE * X;
            Matrix<double> inverse = product.Inverse();
            */
        }


        [FoldoutGroup("Functions"), Button(ButtonSizes.Small)]
        public void DoRegression()
        {
            List<SingleFrameRestrictionValues> FrameInfo = BruteForce.instance.GetRestrictionsForMotions(CurrentMotion, UploadRestrictions);

            if (Coefficents.Length == 0)
                Coefficents = new float[FrameInfo[0].OutputRestrictions.Count + 1];
            
            float[][] Inputs = new float[FrameInfo.Count][];
            for (int i = 0; i < Inputs.Length; i++)
            {
                Inputs[i] = new float[FrameInfo[0].OutputRestrictions.Count * EachTotalDegree];
                for (int j = 0; j < FrameInfo[0].OutputRestrictions.Count; j++)
                    for (int k = 0; k < EachTotalDegree; k++)
                        Inputs[i][(j * EachTotalDegree) + k] = Mathf.Pow(FrameInfo[i].OutputRestrictions[j], k + 1);

            }
            TestValues = Inputs;
            float[] targets = FrameInfo.Select(x => x.AtMotionState ? 1f : 0f).ToArray();


            float[] coefficients;
            Train(Inputs, targets, LearnRate, Iterations);
            Coefficents = coefficients;
            TestPercent();
            float TestPercent()
            {
                float2 Guesses = new float2(0f, 0f);
                CorrectOnTrue = 0;
                CorrectOnFalse = 0;
                InCorrectOnTrue = 0;
                InCorrectOnFalse = 0;

                for (int i = 0; i < Inputs.Length; i++)
                {
                    float FinalValue = 0f;
                    for (int j = 0; j < FrameInfo[0].OutputRestrictions.Count; j++)
                        for (int k = 0; k < EachTotalDegree; k++)
                            FinalValue += Mathf.Pow(FrameInfo[i].OutputRestrictions[j], k + 1) * coefficients[j];

                    bool Guess = FinalValue > 0.5f;
                    bool Truth = FrameInfo[i].AtMotionState;
                    bool IsCorrect = Guess == Truth;
                    Guesses = new float2(Guesses.x + (!IsCorrect ? 1f : 0f), Guesses.y + (IsCorrect ? 1f : 0f));

                    CorrectOnTrue += (IsCorrect && Truth) ? 1 : 0;
                    CorrectOnFalse += (IsCorrect && !Truth) ? 1 : 0;
                    InCorrectOnTrue += (!IsCorrect && Truth) ? 1 : 0;
                    InCorrectOnFalse += (!IsCorrect && !Truth) ? 1 : 0;

                }
                float CorrectPercent = (Guesses.y / (Guesses.x + Guesses.y)) * 100f;
                Debug.Log(CorrectPercent + "% Correct");
                return CorrectPercent;
            }

            void Train(float[][] inputs, float[] targets, float learningRate, int numIterations)
            {
                int numInputs = inputs[0].Length;
                coefficients = new float[numInputs + 1];

                for (int i = 0; i < numIterations; i++)
                {
                    float cost = 0;
                    float[] gradient = new float[numInputs + 1];

                    for (int j = 0; j < inputs.Length; j++)
                    {
                        float[] inputWithBias = AddBias(inputs[j]);
                        float prediction = Predict(inputWithBias);
                        float error = targets[j] - prediction;
                        cost += error * error;

                        for (int k = 0; k < inputWithBias.Length; k++)
                        {
                            gradient[k] += error * inputWithBias[k];
                        }
                    }

                    cost /= inputs.Length;

                    for (int j = 0; j < gradient.Length; j++)
                    {
                        gradient[j] /= inputs.Length;
                        coefficients[j] += learningRate * gradient[j];
                    }
                }
            }

            float Predict(float[] input)
            {
                float z = 0;

                for (int i = 0; i < input.Length; i++)
                {
                    z += input[i] * coefficients[i];
                }

                return Sigmoid(z);
            }

            float Sigmoid(float z) { return 1 / (1 + Mathf.Exp(-z)); }

            float[] AddBias(float[] input)
            {
                float[] inputWithBias = new float[input.Length + 1];
                inputWithBias[0] = 1;

                for (int i = 0; i < input.Length; i++)
                {
                    inputWithBias[i + 1] = input[i];
                }

                return inputWithBias;
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
