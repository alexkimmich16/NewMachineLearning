using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System;
namespace RestrictionSystem
{
    public enum Condition
    {
        Time = 0,
        Distance = 1,
        Restriction = 2,
        ConsistantDirection = 3,
        ConsecutiveFrames = 4,
    }
    public delegate bool ConditionWorksAndAdd(SingleConditionInfo Condition, SingleInfo CurrentFrame, SingleInfo PastFrame, bool NewState, Side side);
    public delegate void OnNewMotionState(Side side, bool NewState, int Index, int Level);

    public class ConditionManager : SerializedMonoBehaviour
    {
        /// <summary>
        /// acts to allow both probiting of true/false, as well as option for spell sequences
        /// </summary>
        
        public static ConditionManager instance;
        private void Awake() { instance = this; }
        public Conditions conditions;

        public static Dictionary<Condition, ConditionWorksAndAdd> ConditionDictionary = new Dictionary<Condition, ConditionWorksAndAdd>(){
            {Condition.Time, TimeWorksAndAdd},
            {Condition.Distance, DistanceWorksAndAdd},
            {Condition.Restriction, RestrictionWorksAndAdd},
            {Condition.ConsistantDirection, StraightWorksAndAdd},
            {Condition.ConsecutiveFrames, ConsecutiveWorksAndAdd},
        };
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
                Condition.Value[(int)side] = RawRestrictionValue;
                return RawRestrictionValue > Condition.Range.x && RawRestrictionValue < Condition.Range.y;
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
        public static bool StraightWorksAndAdd(SingleConditionInfo Condition, SingleInfo CurrentFrame, SingleInfo PastFrame, bool NewState, Side side)
        {
            if (Condition.LastState[(int)side] == false && NewState == true)
                Condition.StartPos[(int)side] = CurrentFrame.HandPos;
            else if (NewState == true && Condition.LastState[(int)side] == true)
            {
                //end rotation is similar to start rotation
                //end position is parrelel to start rotation's forward
                ///two seperate??


                Condition.Value[(int)side] = Vector3.Distance(Condition.StartPos[(int)side], CurrentFrame.HandPos);
                return Condition.Value[(int)side] > Condition.Amount;
                //Debug.Log("ongoing: " + (Vector3.Distance(Condition.StartPos, CurrentFrame.HandPos) > Condition.Amount));
            }
            return false;
        }
        public static bool ConsecutiveWorksAndAdd(SingleConditionInfo Condition, SingleInfo CurrentFrame, SingleInfo PastFrame, bool NewState, Side side)///may require time instead of frames in future
        {
            if (Condition.LastState[(int)side] == Condition.WaitSide)
                Condition.StartTime[(int)side] = 1f;

            if (Condition.StartTime[(int)side] == 0f)
                return true;

            if (Condition.LastState[(int)side] == Condition.WaitSide)
            {
                Condition.Value[(int)side] = 0f;
                return true;
            }
            else
            {
                Condition.Value[(int)side] += 1f;
            }
                

            if (Condition.Value[(int)side] >= Condition.Amount)
            {
                Condition.Value[(int)side] = 0f;
                Condition.StartTime[(int)side] = 0f;
                return true;
            }
            return false;
        }
        public void PassValue(bool State, CurrentLearn Motion, Side side)
        {
            //Debug.Log("Pass: " + State);
            conditions.MotionConditions[(int)Motion - 1].PassValueToAll(State, side);
        }

        private void Start()
        {
            conditions.ResetConditions();
        }
    }



    public enum ConditionType
    {
        Sequence = 0,
        Prohibit = 1,
    }
    [Serializable]
    public class MotionConditionInfo
    {
        public string Motion;
        public ConditionType conditionType;
        public static bool ShowRuntime = false;
        public int CastLevel;
        

        [ListDrawerSettings(ShowIndexLabels = true, ListElementLabelName = "StateToActivate"), ShowIf("conditionType", ConditionType.Sequence)] public List<ConditionList> ConditionLists;
        [ListDrawerSettings(ShowIndexLabels = true), ShowIf("conditionType", ConditionType.Prohibit)] public List<SingleConditionInfo> ProhibitList;
        public event OnNewMotionState OnNewState;

        [FoldoutGroup("Runtime"), ShowIf("ShowRuntime")] public List<int> CurrentStage = new List<int>() { 0, 0 };
        [FoldoutGroup("Runtime"), ShowIf("ShowRuntime")] public List<bool> WaitingForFalse = new List<bool>() { false, false };
        
        [Serializable]
        public class ConditionList
        {
            public string StateToActivate;
            [ListDrawerSettings(ShowIndexLabels = true, ListElementLabelName = "Label")] public List<SingleConditionInfo> SingleConditions;
        }

        public void ResetAll(Side side)
        {
            //Debug.Log("")
            if (ConditionLists.Count != 0)
            {
                for (int i = 0; i < CurrentStage[(int)side]; i++)
                {
                    OnNewState?.Invoke(side, false, i, CastLevel);
                    for (int j = 0; j < ConditionLists[i].SingleConditions.Count; j++)//all conditions that have passed
                    {
                        SingleConditionInfo info = ConditionLists[i].SingleConditions[j];
                        //if (info.LastState[(int)side] == true)

                        info.Value[(int)side] = 0f;
                        info.LastState[(int)side] = false;
                    }
                }
            }
            else if (WaitingForFalse[(int)side] == true)
            {
                OnNewState?.Invoke(side, false, 0, CastLevel);
            }
            WaitingForFalse[(int)side] = false;
            //Debug.Log("CurrentStage: " + CurrentStage[(int)side]);

            CurrentStage[(int)side] = 0;
        }




        public void PassValueToAll(bool State, Side side)
        {
            if ((State == false || WaitingForFalse[(int)side] == true) && conditionType == ConditionType.Sequence)
            {
                if (State == false)
                {
                    ResetAll(side);
                }

                //Debug.Log("trigger1");
                return;
            }
            if(conditionType == ConditionType.Prohibit)
            {
                //if wait conditions are met, wait for them
                //if(Motion == "flames")
                    //Debug.Log("prohibit: " + Motion + "  State: " + State);
                bool AllWorkingSoFar = true;
                if (ProhibitList.Count != 0)
                    for (int i = 0; i < ProhibitList.Count; i++)
                    {
                        SingleConditionInfo info = ProhibitList[i];
                        ConditionWorksAndAdd WorkingConditionAndUpdate = ConditionManager.ConditionDictionary[info.condition];

                        bool Working = WorkingConditionAndUpdate.Invoke(info, PastFrameRecorder.instance.GetControllerInfo(side), PastFrameRecorder.instance.PastFrame(side), State, side); ///velocity error here

                        if (Working == false)
                            AllWorkingSoFar = false;

                        info.LastState[(int)side] = State;
                        //Debug.Log("once: " + Working);
                    }

                if (WaitingForFalse[(int)side] != State && AllWorkingSoFar)
                {
                    OnNewState?.Invoke(side, State, 0, CastLevel);
                    WaitingForFalse[(int)side] = State;
                }
            }
            else if(conditionType == ConditionType.Sequence)
            {
                bool AllWorkingSoFar = true;
                if (ConditionLists.Count != 0)
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
                    OnNewState?.Invoke(side, true, CurrentStage[(int)side], CastLevel);
                    //Debug.Log(CurrentStage + " < " + (ConditionLists.Count - 1));
                    if (CurrentStage[(int)side] < ConditionLists.Count - 1)
                    {
                        CurrentStage[(int)side] += 1;
                    }
                    else
                    {
                        ResetAll(side);
                    }
                }
            }
            
            
        }
    }
    [Serializable]
    public class SingleConditionInfo
    {
        /// <summary>
        /// tasked with tracking variables and patterns over time
        /// </summary>
        
        
        public string Label;
        //public bool Active;
        public Condition condition;
        [ShowIf("HasAmount")] public float Amount;
        [ShowIf("condition", Condition.ConsecutiveFrames)] public bool WaitSide;
        [ShowIf("condition", Condition.Restriction)] public SingleRestriction restriction;
        [ShowIf("condition", Condition.Restriction)] public Vector2 Range;
        ///reset on false?
        [FoldoutGroup("Values")] public List<bool> LastState = new List<bool>() { false, false };
        [FoldoutGroup("Values")] public List<float> StartTime = new List<float>() { 0f, 0f };
        [FoldoutGroup("Values")] public List<Vector3> StartPos = new List<Vector3>() { Vector3.zero, Vector3.zero };
        [FoldoutGroup("Values")] public List<float> Value = new List<float>() { 0, 0 };
        private bool HasAmount() { return condition == Condition.Distance || condition == Condition.Time || condition == Condition.ConsecutiveFrames ; }


    }
}