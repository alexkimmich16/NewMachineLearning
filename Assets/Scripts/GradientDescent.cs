using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
public class GradientDescent : SerializedMonoBehaviour
{
    /*
    [Range(0f,1f)]public float learningRate = 0.1f;
    public int Iterations = 1000;
    public float[] weights;

    [FoldoutGroup("BruteForce"), Button(ButtonSizes.Small)]
    public void GetNewIterations()
    {
        // Start the optimization
        for (int i = 0; i < Iterations; i++)
        {
            float[] gradient = ComputeGradient(weights);
            for (int j = 0; j < 4; j++)
            {
                weights[j] += learningRate * gradient[j];
            }
        }
    }
    void Start()
    {
        // Initialize the weights randomly
        weights = new float[4];
        for (int i = 0; i < 4; i++)
        {
            weights[i] = Random.value;
        }
    }
    
    float[] ComputeGradient(float[] weights)
    {
        //definte real function
        
        
        
        
        // Compute the gradient of the function here
        float[] gradient = new float[4];
        float sum = 0;
        for (int i = 0; i < 4; i++)
        {
            sum += weights[i] * weights[i];
        }
        float magnitude = Mathf.Sqrt(sum);
        for (int i = 0; i < 4; i++)
        {
            gradient[i] = 2 * weights[i] / (2 * magnitude);
        }
        return gradient;
    }
    */


    public float x = 2f;
    public float y = 2f;
    public float alpha = 0.1f;
    [Button(ButtonSizes.Small)]
    public void LearnOneStep()
    {
        //x = x - (alpha * 2f * x);
        float XVal = x - (alpha * 2f * x);
        Debug.Log(XVal);
        //x = 2 - (0.1f * 4f);
        y = y - alpha * (2f * y);
    }



}
