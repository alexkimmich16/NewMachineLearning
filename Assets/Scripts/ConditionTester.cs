using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using RestrictionSystem;
using System.Linq;
using System;
public class ConditionTester : SerializedMonoBehaviour
{
    //INVOLVES WHEN TO CAST
    //SHOULD INCLUDE BOTH INSTANT AND MEANWHILE DEMOS
    public static ConditionTester instance;
    private void Awake() { instance = this; }

    [FoldoutGroup("Object")] public GameObject[] SpawnPrefab;
    [FoldoutGroup("Object")] public float KillTime;

    [FoldoutGroup("LogisticStats")] public int FramesPastFinal = 3;
    [FoldoutGroup("LogisticStats")] public int Degrees = 1;
    [FoldoutGroup("LogisticStats")] public int EarlyAllow = 1;
    [FoldoutGroup("LogisticStats")] public int LateAllow = 1;
    [FoldoutGroup("LogisticStats")] public bool Progressive;
    [FoldoutGroup("LogisticStats")] public int EachProgressiveAdd;
    [FoldoutGroup("CalculateInfo")] public int SequenceTry;

    [FoldoutGroup("Testing")] public double[][] TestInputs;
    [FoldoutGroup("Testing")] public double[] TestOutputs;

    public CurrentLearn Motion() { return MotionEditor.instance.MotionType; }

    ///involves 2 things:
    ///getting correct coefficents
    /// testing

    ///TESTS EVERY COMBINATION WITHIN MOTION
    [Button(ButtonSizes.Small)]
    public void CalculateCoefficents()
    {
        if (ConditionManager.instance.conditions.MotionConditions[(int)Motion() - 1].conditionType == ConditionType.Prohibit)
            return;
        MotionConditionInfo Condition = ConditionManager.instance.conditions.MotionConditions[(int)Motion() - 1];
        SingleSequenceState SequenceCondition = Condition.ConditionLists[SequenceTry];
        List<List<double>> Inputs = new List<List<double>>();
        List<double> Outputs = new List<double>();
        for (int i = 0; i < LearnManager.instance.MovementList[(int)Motion()].Motions.Count; i++)//for each start and end(for loop +=2 each time)
        {
            //Debug.Log("AllowedEnter: " + MotionAssign.instance.TrueMotions[(int)Motion() - 1].Any(Vector => (i >= Vector.x && i <= Vector.y)));
            if (MotionAssign.instance.TrueMotions[(int)Motion() - 1].Any(Vector => (i >= Vector.x && i <= Vector.y)))
            {
                Motion motion = LearnManager.instance.MovementList[(int)Motion()].Motions[i];

                int Start = (int)motion.TrueRanges[0].x;
                int End = (int)motion.TrueRanges[motion.TrueRanges.Count - 1].y;
                
                for (int j = Start; j < Start + EarlyAllow + 1; j++)
                {
                    for (int k = j + EachProgressiveAdd; k < End + FramesPastFinal + 1; k += ToAdd(k, End, End + FramesPastFinal + 1))
                    {
                        //Debug.Log("CallTimes for inbetween frames: " + (End - Start));
                        if(i == 0)
                        {
                            //Debug.Log("")
                            //Debug.Log("J: " + j + "  K: " + k + " WORKS: " + ((j <= Start + EarlyAllow && j >= Start) && (k >= End && k <= End + LateAllow)));
                        }
                            
                        //Debug.Log("K: " + k + "  END: " + End);
                        SingleInfo StartInfo = motion.Infos[j];
                        SingleInfo EndInfo = motion.Infos[k];
                        List<double> InsertList = new List<double>();

                        for (int l = 0; l < SequenceCondition.SingleConditions.Count; l++)
                        {
                            InsertList.Add(ConditionManager.ConditionDictionary[SequenceCondition.SingleConditions[l].condition].Invoke(SequenceCondition.SingleConditions[l].restriction, StartInfo, EndInfo));
                        }
                        Inputs.Add(InsertList);

                        Outputs.Add(((j <= Start + EarlyAllow && j >= Start) && (k >= End && k <= End + LateAllow)) ? 1d : 0d);
                    }

                }
            }
            //Debug.Log("Inputs: " + Inputs.Count);
        }
        int ToAdd(int Current, int Threshold1, int Threshold2)
        {
            
            if (Current < Threshold1)
            {
                //Debug.Log("PT1");
                if (Current + EachProgressiveAdd <= Threshold1)
                    return EachProgressiveAdd;
                //Debug.Log("PT2");
                if (Current + EachProgressiveAdd > Threshold1)//
                    return (Threshold1 - Current);

            }
            //Debug.Log("PT3");


            return 1;
        }
        TestInputs = Inputs.Select(x => x.ToArray()).ToArray();
        TestOutputs = Outputs.ToArray();
        LogisticRegression logisticRegression = new LogisticRegression(Inputs.Select(x => x.ToArray()).ToArray(), Outputs.ToArray(), Degrees);
        SequenceCondition.Coefficents = logisticRegression.Coefficents;

        //Debug.Log(logisticRegression.PercentComplexString());
        Debug.Log(logisticRegression.PercentSimpleString());

        //Debug.Log("Coefficents: " + logisticRegression.Coefficents[0] + ", " + logisticRegression.Coefficents[1] + ", " + logisticRegression.Coefficents[2] + ", ");
    }
    private void Start()
    {
        ConditionManager.instance.conditions.MotionConditions[(int)CurrentLearn.Fireball - 1].OnNewState += FireballTest;
        ConditionManager.instance.conditions.MotionConditions[(int)CurrentLearn.Flames - 1].OnNewState += FlameTest;
        ConditionManager.instance.conditions.MotionConditions[(int)CurrentLearn.FlameBlock - 1].OnNewState += FireBlockTest;
    }
    public void FireballTest(Side side, bool NewState, int Index, int Level)
    {
        if (MotionEditor.instance.TestAllMotions.isOn == false && Motion() == CurrentLearn.Fireball)
        {
            //if (NewState == true)
                //Debug.Log("EventCalled: " + side.ToString() + "  " + Index);
            if (NewState == true)
            {
                GameObject SpawnedObject = Instantiate(SpawnPrefab[Index], PastFrameRecorder.instance.PlayerHands[(int)side].transform.position, PastFrameRecorder.instance.PlayerHands[(int)side].transform.rotation);
                Destroy(SpawnedObject, KillTime);
            }
        }
    }
    public void FlameTest(Side side, bool NewState, int Index, int Level)
    {
        if (MotionEditor.instance.TestAllMotions.isOn == false && Motion() == CurrentLearn.Flames)
        {
            //if (NewState == true)
            //Debug.Log("EventCalled: " + side.ToString() + "  " + Index);
            /*
            if (NewState == true)
            {
                GameObject SpawnedObject = Instantiate(SpawnPrefab[Index], PastFrameRecorder.instance.PlayerHands[(int)side].transform.position, PastFrameRecorder.instance.PlayerHands[(int)side].transform.rotation);
                Destroy(SpawnedObject, KillTime);
            }
            */
        }
    }
    public void FireBlockTest(Side side, bool NewState, int Index, int Level)
    {
        if (MotionEditor.instance.TestAllMotions.isOn == false && Motion() == CurrentLearn.FlameBlock)
        {
            //if (NewState == true)
            //Debug.Log("EventCalled: " + side.ToString() + "  " + Index);
            /*
            if (NewState == true)
            {
                GameObject SpawnedObject = Instantiate(SpawnPrefab[Index], PastFrameRecorder.instance.PlayerHands[(int)side].transform.position, PastFrameRecorder.instance.PlayerHands[(int)side].transform.rotation);
                Destroy(SpawnedObject, KillTime);
            }
            */
        }
    }
}
