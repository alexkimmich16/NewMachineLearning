using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System;

//using Unity.Mathematics;

namespace RestrictionSystem
{
    

    public enum Condition
    {
        Time = 0,
        Distance = 1,
        Restriction = 2,
    }
    public delegate bool ConditionWorksAndAdd(SingleConditionInfo Condition, SingleInfo CurrentFrame, SingleInfo PastFrame, bool NewState, Side side);
    public delegate void OnNewMotionState(Side side, bool NewState, int Index);
    
    public class ConditionManager : SerializedMonoBehaviour
    {
        public static Dictionary<Condition, ConditionWorksAndAdd> ConditionDictionary = new Dictionary<Condition, ConditionWorksAndAdd>(){
            {Condition.Time, TimeWorksAndAdd},
            {Condition.Distance, DistanceWorksAndAdd},
            {Condition.Restriction, RestrictionWorksAndAdd},
        };

        public static ConditionManager instance;
        private void Awake() { instance = this; }
        public MotionSettings RestrictionSettings;
        [ListDrawerSettings(ShowIndexLabels = true, ListElementLabelName = "Motion")] public List<MotionConditionInfo> MotionConditions;

        public static bool TimeWorksAndAdd(SingleConditionInfo Condition, SingleInfo CurrentFrame, SingleInfo PastFrame, bool NewState, Side side)
        {
            if (Condition.LastState[(int)side] == false && NewState == true)
                Condition.StartTime[(int)side] = Time.realtimeSinceStartup;
            else if (NewState == true && Condition.LastState[(int)side] == true)
            {
                Condition.Value[(int)side] = Time.realtimeSinceStartup - Condition.StartTime[(int)side];
                return Condition.Value[(int)side] > Condition.Amount;
            }
            return false;
        }
        public static bool RestrictionWorksAndAdd(SingleConditionInfo Condition, SingleInfo CurrentFrame, SingleInfo PastFrame, bool NewState, Side side)
        {
            if (NewState == true && Condition.LastState[(int)side] == true)
            {
                RestrictionTest RestrictionType = RestrictionManager.RestrictionDictionary[Condition.restriction.restriction];
                float RawRestrictionValue = RestrictionType.Invoke(Condition.restriction, CurrentFrame, PastFrame);
                float Correctness = Condition.restriction.GetValue(RawRestrictionValue);
                return Correctness == 1;
            }
            return false;
        }
        public static bool DistanceWorksAndAdd(SingleConditionInfo Condition, SingleInfo CurrentFrame, SingleInfo PastFrame, bool NewState, Side side)
        {
            if (Condition.LastState[(int)side] == false && NewState == true)
                Condition.StartPos[(int)side] = CurrentFrame.HandPos;
            else if (NewState == true && Condition.LastState[(int)side] == true)
            {
                Condition.Value[(int)side] = Vector3.Distance(Condition.StartPos[(int)side], CurrentFrame.HandPos);
                return Condition.Value[(int)side] > Condition.Amount;
                //Debug.Log("ongoing: " + (Vector3.Distance(Condition.StartPos, CurrentFrame.HandPos) > Condition.Amount));
            }
            return false;
        }
        public void PassValue(bool State, CurrentLearn Motion, Side side)
        {
            //Debug.Log("Pass: " + State);
            MotionConditions[(int)Motion - 1].PassValueToAll(State, side);
        }
    }



    
    [Serializable]
    public class MotionConditionInfo
    {
        public string Motion;
        public List<int> CurrentStage = new List<int>() { 0, 0 };
        public bool ResetOnMax;
        public List<bool> WaitingForFalse = new List<bool>() { false, false};
        [ListDrawerSettings(ShowIndexLabels = true, ListElementLabelName = "Label")] public List<ConditionList> ConditionLists;

        public event OnNewMotionState OnNewState;
        [Serializable]
        public class ConditionList
        {
            public string Label;
            [ListDrawerSettings(ShowIndexLabels = true, ListElementLabelName = "Label")] public List<SingleConditionInfo> SingleConditions;
        }

        public void ResetAll(Side side)
        {
            //Debug.Log("")
            if (ConditionLists.Count != 0)
            {
                for (int i = 0; i < CurrentStage[(int)side]; i++)
                {
                    OnNewState?.Invoke(Side.right, false, i);
                    for (int j = 0; j < ConditionLists[i].SingleConditions.Count; j++)//all conditions that have passed
                    {
                        SingleConditionInfo info = ConditionLists[i].SingleConditions[j];
                        //if (info.LastState[(int)side] == true)

                        info.Value[(int)side] = 0f;
                        info.LastState[(int)side] = false;
                    }
                }
            }
            else if(WaitingForFalse[(int)side] == true)
            {
                OnNewState?.Invoke(Side.right, false, 0);
            }
            WaitingForFalse[(int)side] = false;
            //Debug.Log("CurrentStage: " + CurrentStage[(int)side]);

            CurrentStage[(int)side] = 0;
        }

        public void PassValueToAll(bool State, Side side)
        {
            if(State == false || WaitingForFalse[(int)side] == true)
            {
                //if(WaitingForFalse[(int)side] == true)

                
                if(State == false)
                {
                    ResetAll(side);
                }
                    
                //Debug.Log("trigger1");
                return;
            }


            bool AllWorkingSoFar = true;
            if(ConditionLists.Count != 0)
            {
                for (int i = 0; i < ConditionLists[CurrentStage[(int)side]].SingleConditions.Count; i++)
                {
                    SingleConditionInfo info = ConditionLists[CurrentStage[(int)side]].SingleConditions[i];
                    ConditionWorksAndAdd WorkingConditionAndUpdate = ConditionManager.ConditionDictionary[info.condition];

                    //SingleInfo CurrentFrame = MotionEditor.instance.display.GetFrameInfo(false);
                    //SingleInfo PastFrame = MotionEditor.instance.display.GetFrameInfo(true);

                    bool Working = WorkingConditionAndUpdate.Invoke(info, PastFrameRecorder.instance.GetControllerInfo(side), PastFrameRecorder.instance.PastFrame(side), State, side); ///velocity error here
                    //bool Working = WorkingConditionAndUpdate.Invoke(info, MotionEditor.instance.display.GetFrameInfo(false), MotionEditor.instance.display.GetFrameInfo(true), State, side); ///velocity error here
                    if (Working == false)
                        AllWorkingSoFar = false;
                    info.LastState[(int)side] = State;
                    //Debug.Log("once: " + Working);
                }
            }
            

            if (AllWorkingSoFar) //ready to move to next
            {
                OnNewState?.Invoke(side, true, CurrentStage[(int)side]);
                //Debug.Log(CurrentStage + " < " + (ConditionLists.Count - 1));
                if (CurrentStage[(int)side] < ConditionLists.Count - 1)
                {
                    CurrentStage[(int)side] += 1;
                }
                else
                {
                    
                    if (ResetOnMax)// potentially problematic
                    {
                        ResetAll(side);
                    }
                    else
                    {
                        WaitingForFalse[(int)side] = true;
                    }
                }
            }
        }
    }
    [Serializable]
    public class SingleConditionInfo
    {
        public string Label;
        //public bool Active;
        public Condition condition;
        [ShowIf("HasAmount")] public float Amount;
        [ShowIf("condition", Condition.Restriction)] public SingleRestriction restriction;

        ///reset on false?
        [FoldoutGroup("Values")] public List<bool> LastState = new List<bool>() { false, false};
        [FoldoutGroup("Values")] public List<float> StartTime = new List<float>() { 0f, 0f };
        [FoldoutGroup("Values")] public List<Vector3> StartPos = new List<Vector3>() { Vector3.zero, Vector3.zero };
        [FoldoutGroup("Values")] public List<float> Value = new List<float>() { 0, 0 };
        private bool HasAmount() { return condition == Condition.Distance || condition == Condition.Time; }

        
    }
}

