using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using Unity.Mathematics;
using System;
using RestrictionSystem;
public struct LogisticRegression
{
    public int Iterations;

    public float4 Guesses;
    public double[] Coefficents;
    public float Total() { return Guesses.w + Guesses.x + Guesses.y + Guesses.z; }

    public float CorrectOnTrue() { return Guesses.w; }
    public float IncorrectOnTrue() { return Guesses.x; }
    public float CorrectOnFalse() { return Guesses.y; }
    public float IncorrectOnFalse() { return Guesses.z; }

    public float CorrectPercent() { return (Guesses.w + Guesses.y) / Total(); }
    public float InCorrectPercent() { return (Guesses.x + Guesses.z) / Total(); }
    public float OnTruePercent() { return (Guesses.w + Guesses.x) / Total(); }
    public float OnFalsePercent() { return (Guesses.y + Guesses.z) / Total(); }

    public float CorrectCount() { return (int)(Guesses.w + Guesses.y); }
    public float InCorrectCount() { return (int)(Guesses.x + Guesses.z); }
    public float HighCount() { return (Guesses.w + Guesses.x); }
    public float LowCount() { return (Guesses.y + Guesses.z); }

    public static float4 GetRegressionGuesses(double[][] InputValues, double[] Output, int EachTotalDegree, double[] Coefficents)
    {
        float4 Guesses = float4.zero;
        for (int i = 0; i < InputValues.Length; i++)//INPUT VALUES ALREADY CONTAIN POWER
        {
            double Total = Coefficents[0];
            if(i == 0)
                Debug.Log("jCount: " + InputValues[0].Length);
            for (int j = 0; j < (InputValues[0].Length - 1) / EachTotalDegree; j++)//each  variable
                for (int k = 0; k < EachTotalDegree; k++)//powers
                {
                    //Debug.Log("J: " + j + "  K: " + k);
                    Total += InputValues[i][(j * EachTotalDegree) + k + 1] * Coefficents[(j * EachTotalDegree) + k + 1];
                }
                    

            //insert formula
            double GuessValue = 1f / (1f + Math.Exp(-Total));
            bool Guess = GuessValue > 0.5f;
            bool Truth = Output[i] == 1d;
            bool Correct = Guess == Truth;

            Guesses += new float4((Correct && Truth) ? 1f : 0f, (!Correct && Truth) ? 1f : 0f, (Correct && !Truth) ? 1f : 0f, (!Correct && !Truth) ? 1f : 0);
        }
        return Guesses;
    }
    public LogisticRegression(double[][] InputValues, double[] Output, int EachTotalDegree, double[] Coefficents)//for ONLY TESTING
    {
        this.Coefficents = new double[InputValues[0].Length];
        Guesses = GetRegressionGuesses(InputValues, Output, EachTotalDegree, Coefficents);
        Iterations = 0;
    }
    public LogisticRegression(double[][] InputValues, double[] Output, int EachTotalDegree)//for calculating
    {
        Coefficents = new double[InputValues[0].Length];

        Guesses = int4.zero;
        Iterations = 0;
        float CorrectPercent = 0f;
        while (Iterations < 20)
        {
            double[] Predictions = GetPredictions(InputValues, Coefficents);
            double[][] CovarianceMatrix = GetCovarianceMatrix(InputValues, Predictions, Iterations >= 2);
            double[] IterationMatrix = GetIterationMatrix(InputValues, CovarianceMatrix, Output, Predictions);
            Coefficents = GetCoefficents(Coefficents, IterationMatrix);
            if (CorrectPercent > GetRegressionRating(out float4 NonUseGuesses, Coefficents) && Iterations != 0) //check for decrease
                break;


            CorrectPercent = GetRegressionRating(out Guesses, Coefficents);
            //Debug.Log(CorrectPercent + "% Correct");
            Iterations += 1;

            /*
            double[] Predictions = GetPredictions(InputValues, Coefficents);
            double[][] CovarianceMatrix = GetCovarianceMatrix(InputValues, Predictions, Iterations >= 2);
            double[] IterationMatrix = GetIterationMatrix(InputValues, CovarianceMatrix, Output, Predictions);

            if (CorrectPercent > GetRegressionRating(out float4 Unused, GetCoefficents(Coefficents, IterationMatrix))) //check for decrease
                break;
            Coefficents = GetCoefficents(Coefficents, IterationMatrix);
            
            CorrectPercent = GetRegressionRating(out Guesses, Coefficents);
            Iterations += 1;
            */
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
