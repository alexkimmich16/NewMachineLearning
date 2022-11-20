using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestAssigner : MonoBehaviour
{
    public learningState CurrentState;
    public List<LearningAgent> Tests;
    public List<LearningAgent> Learning;
    void Awake()
    {
        for (int i = 0; i < Tests.Count; i++)
        {
            Tests[i].state = learningState.Testing;
            LearnManager.instance.LearnReached += Tests[i].LearnStep;
        }
            
        for (int i = 0; i < Learning.Count; i++)
        {
            Learning[i].state = learningState.Learning;
            LearnManager.instance.LearnReached += Learning[i].LearnStep;
        }
            
        /*
        if (CurrentState == CurrentState)
            for (int i = 0; i < Learning.Count; i++)
                
        else if(CurrentState == CurrentState)
            for (int i = 0; i < Tests.Count; i++)
          */      

        //for (int i = 0; i < Learning.Count; i++)
        //Learning[i].IsActive = false;
    }
    
}
