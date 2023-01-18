using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System;
using Unity.Mathematics;

namespace RestrictionSystem
{
    

    public enum Condition
    {
        Time = 0,
        Distance = 1,
        ActiveFor = 2,
    }
    public delegate bool ConditionWorksAndAdd(SingleConditionInfo Condition, SingleInfo CurrentFrame, bool NewState);
    public delegate void OnNewMotionState(EditSide side, bool NewState, int Index);
    
    public class ConditionManager : SerializedMonoBehaviour
    {
        public static Dictionary<Condition, ConditionWorksAndAdd> ConditionDictionary = new Dictionary<Condition, ConditionWorksAndAdd>(){
            {Condition.Time, TimeWorksAndAdd},
            {Condition.Distance, DistanceAdd},
        };

        public static ConditionManager instance;
        private void Awake() { instance = this; }
        [ListDrawerSettings(ShowIndexLabels = true, ListElementLabelName = "Motion")] public List<MotionConditionInfo> MotionConditions;

        public static bool TimeWorksAndAdd(SingleConditionInfo Condition, SingleInfo CurrentFrame, bool NewState)
        {
            if (Condition.LastState == false && NewState == true)
            {
                Condition.StartTime = Time.realtimeSinceStartup;
            }
            else if (NewState == true && Condition.LastState == true)
            {
                Condition.Value = Time.realtimeSinceStartup - Condition.StartTime;
                if (Time.realtimeSinceStartup - Condition.StartTime > Condition.Amount)
                {
                    return true;
                }
            }
            return false;
        }
        public static bool DistanceAdd(SingleConditionInfo Condition, SingleInfo CurrentFrame, bool NewState)
        {
            if (Condition.LastState == false && NewState == true)
            {
                Debug.Log("restart");
                Condition.StartPos = CurrentFrame.HandPos;
            }
            else if (NewState == true && Condition.LastState == true)
            {
                
                Condition.Value = Vector3.Distance(Condition.StartPos, CurrentFrame.HandPos);
                Debug.Log("ongoing: " + (Vector3.Distance(Condition.StartPos, CurrentFrame.HandPos) > Condition.Amount));
                if (Vector3.Distance(Condition.StartPos, CurrentFrame.HandPos) > Condition.Amount)
                {
                    return true;
                }
                    
            }
            return false;
        }
        public void PassValue(bool State, CurrentLearn Motion)
        {
            //Debug.Log((int)Motion - 1);
            MotionConditions[(int)Motion - 1].PassValueToAll(State);
        }
    }



    
    [Serializable]
    public struct MotionConditionInfo
    {
        public string Motion;
        public int CurrentStage;
        public bool ResetOnMax;
        [ListDrawerSettings(ShowIndexLabels = true, ListElementLabelName = "Label")] public List<ConditionList> ConditionLists;

        public event OnNewMotionState OnNewState;
        [Serializable]
        public struct ConditionList
        {
            public string Label;
            [ListDrawerSettings(ShowIndexLabels = true, ListElementLabelName = "Label")] public List<SingleConditionInfo> SingleConditions;
        }

        public void ResetAll()
        {
            for (int i = 0; i < ConditionLists[CurrentStage].SingleConditions.Count; i++)
            {
                SingleConditionInfo info = ConditionLists[CurrentStage].SingleConditions[i];
                info.Value = 0f;
                info.LastState = false;
                OnNewState(EditSide.right, false, i);
            }
        }

        public void PassValueToAll(bool State)
        {
            Debug.Log("pass");
            bool AllWorkingSoFar = true;
            for (int i = 0; i < ConditionLists[CurrentStage].SingleConditions.Count; i++)
            {
                SingleConditionInfo info = ConditionLists[CurrentStage].SingleConditions[i];

                ConditionWorksAndAdd WorkingConditionAndUpdate = ConditionManager.ConditionDictionary[info.condition];

                SingleInfo CurrentFrame = MotionEditor.instance.display.GetFrameInfo();
                bool Working = WorkingConditionAndUpdate.Invoke(info, CurrentFrame, State);
                if (Working == false)
                    AllWorkingSoFar = false;
                info.LastState = State;
                Debug.Log("once: " + Working);
            }

            if (AllWorkingSoFar) //ready to move to next
            {
                ///what when get to top?
                Debug.Log("Done");
                OnNewState?.Invoke(EditSide.right, true, CurrentStage);
                if (CurrentStage < ConditionLists.Count - 1)//0 1 == true, 1, 1 == false
                    CurrentStage += 1;
                else if(ResetOnMax)// potentially problematic
                {
                    ResetAll();
                    CurrentStage = 0;
                }
                     

            }

        }
    }
    [Serializable]
    public class SingleConditionInfo
    {
        public string Label;
        public bool Active;
        public Condition condition;
        [ShowIf("HasAmount")] public float Amount;


        ///reset on false?
        [ReadOnly] public bool LastState;
        [ReadOnly] public float StartTime;
        [ReadOnly] public Vector3 StartPos;

        private bool HasAmount() { return condition == Condition.Distance || condition == Condition.Time; }

        [ReadOnly] public float Value;
    }
}

