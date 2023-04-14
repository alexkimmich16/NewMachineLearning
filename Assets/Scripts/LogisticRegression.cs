using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using Unity.Mathematics;
using System;
using System.Linq;
using RestrictionSystem;
public struct LogisticRegression
{
    public int Iterations;

    public float4 Guesses;
    public double[] Coefficents;
    public float Total() { return Guesses.w + Guesses.x + Guesses.y + Guesses.z; }
    public string PercentComplexString() {return "Correct: "  + CorrectPercent() + "  Wrong: " + InCorrectPercent() + "  True: " + OnTruePercent() + "  False: " + OnFalsePercent(); }
    public string PercentSimpleString() {return "CorrectOnTrue: " + CorrectOnTruePercent() + "/" + IncorrectOnTruePercent() + "   CorrectOnFalse: " + CorrectOnFalsePercent() + "/" + IncorrectOnFalsePercent(); }
    public float CorrectOnTrue() { return Guesses.w; }
    public float IncorrectOnTrue() { return Guesses.x; }
    public float CorrectOnFalse() { return Guesses.y; }
    public float IncorrectOnFalse() { return Guesses.z; }

    public float CorrectOnTruePercent() { return CorrectOnTrue() / (CorrectOnTrue() + IncorrectOnTrue()) * 100f; }
    public float IncorrectOnTruePercent() { return IncorrectOnTrue() / (CorrectOnTrue() + IncorrectOnTrue()) * 100f; }
    public float CorrectOnFalsePercent() { return CorrectOnFalse() / (CorrectOnFalse() + IncorrectOnFalse()) * 100f; }
    public float IncorrectOnFalsePercent() { return IncorrectOnFalse() / (CorrectOnFalse() + IncorrectOnFalse()) * 100f; }


    public float CorrectPercent() { return (CorrectOnTrue() + CorrectOnFalse()) / Total() * 100f; }
    public float InCorrectPercent() { return (IncorrectOnTrue() + IncorrectOnFalse()) / Total() * 100f; }
    public float OnTruePercent() { return (CorrectOnTrue() + IncorrectOnTrue()) / Total() * 100f; }
    public float OnFalsePercent() { return (CorrectOnFalse() + IncorrectOnFalse()) / Total() * 100f; }

    public static float4 GetRegressionGuesses(double[][] InputValues, double[] Output, int EachTotalDegree, double[] Coefficents)
    {
        //no first 0 passed
        //no powers passed
        float4 Guesses = float4.zero;
        for (int i = 0; i < InputValues.Length; i++)//INPUT VALUES ALREADY CONTAIN POWER
        {
            double Total = Coefficents[0];
            for (int j = 0; j < InputValues[0].Length; j++)//each  variable
                for (int k = 0; k < EachTotalDegree; k++)
                {
                    //Debug.Log(j + "  " + k);
                   // Debug.Log(InputValues[0].Length + "  " + EachTotalDegree + "  " + Coefficents.Length);
                    Total += math.pow(InputValues[i][j], k + 1) * Coefficents[(j * EachTotalDegree) + k + 1];
                }
                    

            //insert formula
            double GuessValue = 1f / (1f + Math.Exp(-Total));
            bool Guess = GuessValue > 0.5f;
            bool IsTrue = Output[i] == 1d;
            bool Correct = Guess == IsTrue;

            Guesses.w += (Correct && IsTrue) ? 1f : 0f;
            Guesses.x += (!Correct && IsTrue) ? 1f : 0f;
            Guesses.y += (Correct && !IsTrue) ? 1f : 0f;
            Guesses.z += (!Correct && !IsTrue) ? 1f : 0;
        }
        return Guesses;
    }
    public double EXPValue(double[] RawInput, double[] Coefficents)
    {
        int Degrees = (Coefficents.Length - 1) / RawInput.Length;

        double Total = Coefficents[0];
        for (int j = 0; j < RawInput.Length; j++)//each  variable
            for (int k = 0; k < Degrees; k++)
            {
                //Debug.Log(j + "  " + k);
                // Debug.Log(InputValues[0].Length + "  " + EachTotalDegree + "  " + Coefficents.Length);
                Total += math.pow(RawInput[j], k + 1) * Coefficents[(j * Degrees) + k + 1];
            }
        return 1f / (1f + Math.Exp(-Total));
    }
    public bool Works(double[] RawInput, double[] Coefficents)
    {
        int Degrees = (Coefficents.Length - 1) / RawInput.Length;

        double Total = Coefficents[0];
        for (int j = 0; j < RawInput.Length; j++)//each  variable
            for (int k = 0; k < Degrees; k++)
            {
                //Debug.Log(j + "  " + k);
                // Debug.Log(InputValues[0].Length + "  " + EachTotalDegree + "  " + Coefficents.Length);
                Total += math.pow(RawInput[j], k + 1) * Coefficents[(j * Degrees) + k + 1];
            }
        return 1f / (1f + Math.Exp(-Total)) > 0.5f;
    }
    public LogisticRegression(double[][] InputValues, double[] Output, int EachTotalDegree, double[] Coefficents)//for ONLY TESTING
    {
        this.Coefficents = new double[InputValues[0].Length];
        Guesses = GetRegressionGuesses(InputValues, Output, EachTotalDegree, Coefficents);
        Iterations = 0;
    }
    public LogisticRegression(double[][] InputValues, double[] Output, int EachTotalDegree)//for calculating
    {
        //at one at 0 of each
        //add powers

        //adjust inputvalues
        double[][] AdjustedInput = new double[InputValues.Length][];

        int InputCount = InputValues[0].Length;
        for (int i = 0; i < InputValues.Length; i++)
        {
            AdjustedInput[i] = new double[(EachTotalDegree * InputValues[i].Length) + 1];
            AdjustedInput[i][0] = 1d;
            for (int j = 0; j < InputCount; j++)
            {
                for (int k = 0; k < EachTotalDegree; k++)
                {
                    AdjustedInput[i][(j * EachTotalDegree) + k + 1] = Math.Pow(InputValues[i][j], k + 1);
                }
                    
            }
        }

        Coefficents = new double[AdjustedInput[0].Length];

        Guesses = int4.zero;
        Iterations = 0;
        float CorrectPercent = 0f;
        while (Iterations < 20)
        {
            double[] Predictions = GetPredictions(AdjustedInput, Coefficents);
            double[][] CovarianceMatrix = GetCovarianceMatrix(AdjustedInput, Predictions, Iterations >= 2);
            double[] IterationMatrix = GetIterationMatrix(AdjustedInput, CovarianceMatrix, Output, Predictions);
            Coefficents = GetCoefficents(Coefficents, IterationMatrix);
            
            //Debug.Log("Interation: " + Iterations + "  CorrectPercent: " + this.CorrectPercent() * 100f + "   WrongPercent: " + InCorrectPercent() * 100f + "   OnFalsePercent: " + OnFalsePercent() * 100f + "   OnTruePercent: " + OnTruePercent() * 100f);
            //Debug.Log("Interation: " + Iterations + "  CorrectPercent: " + this.CorrectPercent() + "   WrongPercent: " + InCorrectPercent() + "   OnFalsePercent: " + OnFalsePercent() + "   OnTruePercent: " + OnTruePercent());

            if (CorrectPercent > GetRegressionRating(out float4 NonUseGuesses, Coefficents) && Iterations > 2) //check for decrease
                break;


            CorrectPercent = GetRegressionRating(out Guesses, Coefficents);
            //Debug.Log(CorrectPercent + "% Correct");
            Iterations += 1;
        }

        float GetRegressionRating(out float4 Guesses, double[] Coefficents)
        {
            Guesses = GetRegressionGuesses(InputValues, Output, EachTotalDegree, Coefficents);
            return (Guesses.w + Guesses.y) / (Guesses.w + Guesses.x + Guesses.y + Guesses.z) * 100f;
        }
    }

    private double[] GetPredictions(double[][] Values, double[] Coefficents)
    {
        double[] ReturnValue = new double[Values.Length];
        for (int i = 0; i < Values.Length; i++)
        {
            double Total = -Coefficents[0];
            for (int j = 1; j < Coefficents.Length; j++)
                Total -= Values[i][j] * Coefficents[j];
            ReturnValue[i] = 1d / (1d + Math.Exp(Total));
        }
        return ReturnValue;
    }
    private double[][] GetCovarianceMatrix(double[][] Input, double[] Predictions, bool IsNewType)
    {
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
            DenseMatrix Second = DenseMatrix.Create(Y.Count, Y.Count, 0);
            for (int i = 0; i < Y.Count; i++)
                Second[i, i] = Y[i] * (1d - Y[i]);
            LowerMMult = (DenseMatrix)Second.Multiply(X);
        }

        DenseMatrix XTransposed = (DenseMatrix)X.Transpose();
        DenseMatrix HigherMMult = (DenseMatrix)XTransposed.Multiply(LowerMMult);
        //Debug.Log("For Higher:: " + "Columns: " + HigherMMult.ColumnCount + "  Rows: " + HigherMMult.RowCount);
        DenseMatrix Final = (DenseMatrix)HigherMMult.Inverse();

        return To2DArray(Final);
    }
    private double[] GetIterationMatrix(double[][] Input, double[][] CovarianceMatrix, double[] Outputs, double[] Predictions)
    {
        Matrix<double> X = Matrix<double>.Build.DenseOfColumnArrays(Input);

        Matrix<double> First = X.Transpose();
        Matrix<double> Second = Matrix<double>.Build.Dense(1, Outputs.Length, 0);
        for (int i = 0; i < Second.ColumnCount; i++)
            Second[0, i] = Outputs[i] - Predictions[i];

        Matrix<double> LowerMMult = Second.Multiply(First);
        Matrix<double> HigherMMult = LowerMMult.Multiply(Matrix<double>.Build.DenseOfRowArrays(CovarianceMatrix));

        return HigherMMult.Row(0).AsArray();
    }
    private double[] GetCoefficents(double[] PastCoefficents, double[] IterationMatrix)
    {
        double[] NewCoefficents = new double[PastCoefficents.Length];
        for (int i = 0; i < NewCoefficents.Length; i++)
            NewCoefficents[i] = PastCoefficents[i] + (IterationMatrix[i] * 1f);
        return NewCoefficents;
    }
    private double[][] To2DArray(Matrix<double> Input)
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
    public double[] GetDoubleCoefficents() { return GetCoefficents().Select(f => (double)f).ToArray(); }
    public float[] GetCoefficents()
    {
        float[] ReturnValue = new float[(Coefficents.Count * Coefficents[0].Degrees.Count) + 1];
        ReturnValue[0] = Intercept;
        for (int i = 0; i < Coefficents.Count; i++)
            for (int j = 0; j < Coefficents[i].Degrees.Count; j++)
                ReturnValue[(i * Coefficents[i].Degrees.Count) + j + 1] = Coefficents[i].Degrees[j];

        return ReturnValue;
    }
}
