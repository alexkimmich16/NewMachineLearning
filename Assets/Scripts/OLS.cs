using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OLS : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    public float GradientDecent()
    {
        float beta_0 = 0;
        float beta_1 = 0;
        float Input1 = 0;
        float beta_2 = 0;
        float Input2 = 0;
        float beta_3 = 0;
        float Input3 = 0;
        //return Output = beta_0 + beta_1 * Input1 + beta_2 * Input2 + beta_3 * Input3;
        return 0f;
        //estimate beta
    }
    
    /*
    public float GradientDecent()
    {

    }
    */

    /*
    float[] OLS(float[][] X, float[][] Y)
    {
        int n = X.Length; // number of observations
        int p = X[0].Length; // number of independent variables

        // Initialize the design matrix and response vector
        float[][] Xmatrix = new float[n][p + 1];
        float[][] Ymatrix = new float[n][3];
        for (int i = 0; i < n; i++)
        {
            Xmatrix[i][0] = 1; // add intercept term
            for (int j = 1; j <= p; j++)
            {
                Xmatrix[i][j] = X[i][j - 1];
            }
            Ymatrix[i][0] = Y[i][0];
            Ymatrix[i][1] = Y[i][1];
            Ymatrix[i][2] = Y[i][2];
        }

        // Compute the transpose of the design matrix

        float[][] XmatrixTranspose = Matrix.Transpose(Xmatrix);

        // Compute the product of the transpose of the design matrix and the design matrix
        float[][] XTX = Matrix.Multiply(XmatrixTranspose, Xmatrix);

        // Compute the inverse of XTX
        float[][] XTXinv = Matrix.Inverse(XTX);

        // Compute the product of XTXinv and the transpose of the design matrix
        float[][] XTXinvXT = Matrix.Multiply(XTXinv, XmatrixTranspose);

        // Compute the product of XTXinvXT and the response vector
        float[][] beta = Matrix.Multiply(XTXinvXT, Ymatrix);

        // Return the estimated regression coefficients
        return beta[0];
    }
    float[][] X = new float[][] {
        new float[] { 1, 2 },
        new float[] { 3, 4 },
        new float[] { 5, }
    };
    */
}
