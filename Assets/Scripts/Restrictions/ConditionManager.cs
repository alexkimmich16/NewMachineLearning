using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System;
using System.Linq;
using Unity.Mathematics;
namespace RestrictionSystem
{
    public enum Condition
    {
        Time = 0,
        Distance = 1,
        Restriction = 2,
    }
    public delegate bool ConditionWorksAndAdd(SingleConditionInfo Condition, SingleInfo CurrentFrame, SingleInfo PastFrame, bool NewState, Side side);
    public delegate void OnNewMotionState(Side side, bool NewState, int Index, int Level);

    public class ConditionManager : SerializedMonoBehaviour
    {
        public static ConditionManager instance;
        private void Awake() { instance = this; }
        public MotionSettings conditions;

        [HideInInspector]public ConditionProgress[,] ConditionStats = new ConditionProgress[2, 0];

        [System.Serializable]
        public class ConditionProgress//represents all sequences within motion
        {
            public bool Active() { return StartInfo != null; }
            public int SequenceState;
            public SingleInfo StartInfo;

            public void Reset()
            {
                SequenceState = 0;
                StartInfo = null;
            }
        }

        private void Start()
        {
            ConditionStats = new ConditionProgress[2, conditions.MotionConditions.Count];
            for (int i = 0; i < 2; i++)
                for (int j = 0; j < conditions.MotionConditions.Count; j++)
                    ConditionStats[i, j] = new ConditionProgress();
        }

        public static Dictionary<Condition, RestrictionTest> ConditionDictionary = new Dictionary<Condition, RestrictionTest>(){
            {Condition.Time, Time},
            {Condition.Distance, Distance},
            {Condition.Restriction, Restriction},
        };
        public static float Time(SingleRestriction restriction, SingleInfo frame1, SingleInfo frame2) { return frame2.SpawnTime - frame1.SpawnTime; }
        public static float Distance(SingleRestriction restriction, SingleInfo frame1, SingleInfo frame2) { return Vector3.Distance(frame1.HandPos, frame2.HandPos); }
        public static float Restriction(SingleRestriction restriction, SingleInfo frame1, SingleInfo frame2) { return RestrictionManager.RestrictionDictionary[restriction.restriction].Invoke(restriction, frame2, frame1); }
        public void PassValue(bool State, CurrentLearn Motion, Side side)
        {
            ConditionProgress Holder = ConditionStats[(int)side, (int)Motion - 1];
            MotionConditionInfo Condition = conditions.MotionConditions[(int)Motion - 1];

            if (State != Holder.Active())
            {
                if(Condition.Sequences.Count == 0)
                {
                    Condition.DoEvent(side, State, Holder.SequenceState, Condition.CastLevel);
                    Holder.StartInfo = State ? PastFrameRecorder.instance.GetControllerInfo(side) : null;
                    return;
                }
                else
                {
                    if(Holder.SequenceState == 0 || Condition.Sequences[Holder.SequenceState].waitType == WaitType.Sequence)
                    {
                        Holder.StartInfo = State ? PastFrameRecorder.instance.GetControllerInfo(side) : null;
                        if(State == false)
                            SequenceReset();
                    }
                }
                   
                

            }
            if (Condition.Sequences.Count == 0)
                return;
            if (Holder.Active() == false)
                return;

            SingleSequenceState NowState = Condition.Sequences[Holder.SequenceState];
            bool Works = TestCondition(NowState, Holder.StartInfo, PastFrameRecorder.instance.GetControllerInfo(side), PastFrameRecorder.instance.PastFrame(side));
            //Debug.Log(Motion.ToString() + " " + State + "  First: " + (Holder.StartInfo != null) + "  NUm: " + Holder.SequenceState + "  Works: " + Works);
            
            //Debug.Log("mot: " + Motion.ToString() + "  1");
            if (NowState.waitType == WaitType.Sequence)
            {
                if (Works && State == true)
                {
                    Progress();
                }
            }
            else if (NowState.waitType == WaitType.UntilConditionMet)
            {
                //do nothing until condition = true, upon which: progress
                if (Works == true)
                {
                    Progress();
                }
            }


            void Progress()
            {
                Condition.DoEvent(side, true, Holder.SequenceState, Condition.CastLevel);
                //never for Empty Sequences
                if (Holder.SequenceState < Condition.Sequences.Count - 1)
                {
                    if (NowState.NewFrameOnDone)
                        Holder.StartInfo = PastFrameRecorder.instance.GetControllerInfo(side);

                    Holder.SequenceState += 1;
                }
                else if (Condition.ResetOnMax && AtMax())
                    SequenceReset();
            }
            void SequenceReset()
            {
                // Debug.Log("Reset");
                Holder.Reset();
                for (int i = 0; i < Condition.Sequences.Count; i++)
                {
                    Condition.DoEvent(side, false, i, Condition.CastLevel);
                }
            }
            bool AtMax() { return Holder.SequenceState == Condition.Sequences.Count - 1; }
            ConditionStats[(int)side, (int)Motion - 1] = Holder;
        }
        public bool TestCondition(SingleSequenceState SequenceCondition, SingleInfo Point, SingleInfo Last, SingleInfo Now)//onyl call if motion works
        {
            if (SequenceCondition == null)
                return true;
            int Degrees = (SequenceCondition.Coefficents.Length - 1) / SequenceCondition.SingleConditions.Count;
            double[] RawInputs = new double[SequenceCondition.SingleConditions.Count];
            for (int i = 0; i < SequenceCondition.SingleConditions.Count; i++)
            {
                SingleInfo Frame1 = SequenceCondition.SingleConditions[i].frameType == UseFrameType.LastPoint ? Point : Last;
                SingleInfo Frame2 = SequenceCondition.SingleConditions[i].frameType == UseFrameType.LastPoint ? Now : Now; //current

                RawInputs[i] = ConditionDictionary[SequenceCondition.SingleConditions[i].condition].Invoke(SequenceCondition.SingleConditions[i].restriction, Frame1, Frame2);
            }

            if (SequenceCondition.RegressionBased)
            {
                double Total = SequenceCondition.Coefficents[0];
                for (int j = 0; j < RawInputs.Length; j++)//each  variable
                    for (int k = 0; k < Degrees; k++)
                        Total += math.pow(RawInputs[j], k + 1) * SequenceCondition.Coefficents[(j * Degrees) + k + 1];
                return 1f / (1f + Math.Exp(-Total)) > SequenceCondition.CutoffValue;
            }
            else
            {
                return Enumerable.Range(0, RawInputs.Length).All(i => SequenceCondition.Coefficents[(i * 2)] < RawInputs[i] && SequenceCondition.Coefficents[(i * 2) + 1] > RawInputs[i]);
            }

        }
    }
    /*
    public enum ConditionType
    {
        Sequence = 0,
        Prohibit = 1,
    }
    */
    [Serializable]
    public class SingleSequenceState
    {
        public string StateToActivate;
        public bool RegressionBased;
        public bool NewFrameOnDone;
        public WaitType waitType;
        public double[] Coefficents;

       [Range(0f,1f), ShowIf("RegressionBased")] public float CutoffValue;
        [ListDrawerSettings(ShowIndexLabels = true, ListElementLabelName = "Label")] public List<SingleConditionInfo> SingleConditions;
    }
    public enum WaitType
    {
        Sequence = 0,
        UntilConditionMet = 1,
    }
    public enum UseFrameType
    {
        LastPoint = 0,
        FramesAgo = 1,
    }
    [Serializable]
    public class MotionConditionInfo
    {
        public string Motion;
        public static bool ShowRuntime = false;
        public bool ResetOnMax;
        [HideInInspector]public int CastLevel;
        

        [ListDrawerSettings(ShowIndexLabels = true, ListElementLabelName = "StateToActivate")] public List<SingleSequenceState> Sequences;
        public event OnNewMotionState OnNewState;
        public void DoEvent(Side side, bool Newstate, int Index, int Level) { OnNewState?.Invoke(side, Newstate, Index, Level); }
    }
    [Serializable]
    public class SingleConditionInfo
    {
        /// <summary>
        /// tasked with tracking variables and patterns over time
        /// </summary>
        
        
        public string Label;
        public UseFrameType frameType;
        //public bool Active;
        public Condition condition;
        //public double[] Coefficents;
        //[ShowIf("condition", Condition.ConsecutiveFrames)] public bool WaitSide;
        [ShowIf("condition", Condition.Restriction)] public SingleRestriction restriction;
        ///reset on false?
        ///
        //private static bool ShowValues = true;

        //[FoldoutGroup("Values"), ShowIf("ShowValues")] public List<bool> LastState = new List<bool>() { false, false };
        //[FoldoutGroup("Values"), ShowIf("ShowValues")] public List<float> StartTime = new List<float>() { 0f, 0f };
        //[FoldoutGroup("Values"), ShowIf("ShowValues")] public List<Vector3> StartPos = new List<Vector3>() { Vector3.zero, Vector3.zero };
        //[FoldoutGroup("Values"), ShowIf("ShowValues")] public List<float> Value = new List<float>() { 0, 0 };
    }
}