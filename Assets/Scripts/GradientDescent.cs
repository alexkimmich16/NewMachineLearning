using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
public class GradientDescent : SerializedMonoBehaviour
{
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



    /*
    public float sigmoid(float x) { return 1 / (1 + np.exp(-x)); }
    public float loss(float y_pred, float y_true) { return -(y_true * np.log(y_pred) + (1 - y_true) * np.log(1 - y_pred)); }
    public float gradient(X, float y_true, float y_pred) { return np.dot(X.T, y_pred - y_true) / y_true.shape[0] }
    public float predict(X, W)
    {
        float z = Mathf.dot(X, W);
        float y_pred = sigmoid(z);
        return y_pred;
    }

    public void Check()
    {
        //Data
        //List<List<int>> numbers = new List<List<int>>();
        List<List<int>> X = new List<List<int>>() { new List<int>() { 1, 2, 3, 4 }, new List<int>() { 2, 3, 4, 5 }, new List<int>() { 3, 4, 5, 6 }, new List<int>() { 4, 5, 6, 7 } };
        List<List<int>> y = new List<List<int>>() { new List<int>() { 0, 0, 1, 1 } };

        //weights
        W = np.zeros((X.shape[1], 1));

        //Hyperparameters
        float lr = 0.1f; //learning rate;
        int num_iterations = 1000;

        //Gradient Descent
        for (int i = 0; i < num_iterations; i++)
        {
            float y_pred = predict(X, W);
            float cost = loss(y_pred, y).mean();
            float grad = gradient(X, y, y_pred);
            W -= lr * grad;

            if(i % 100 == 0)
                Debug.Log("Iteration {i}, Loss: {cost}");
        }


        Debug.Log("Final Weight: {W}");
    }
    */


}
