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
    public delegate void OnNewMotionState(Side side, bool NewState, int Index, int Level);

    public class ConditionManager : SerializedMonoBehaviour
    {
        public static ConditionManager instance;
        private void Awake() { instance = this; }
        public MotionSettings conditions;

        [HideInInspector]public ConditionProgress[,] ConditionStats = new ConditionProgress[2, 0];

        [Serializable]
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
        public void DisableSide(Side side)
        {
            for (int i = 0; i < conditions.MotionConditions.Count; i++)
                DisableMotionSequence((MotionState)(i + 1), side);
        }
        private void Start()
        {
            PastFrameRecorder.disableController += DisableSide;
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
        
        public void PassValue(bool State, MotionState Motion, Side side)
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

            if (NowState.ExitIfAny && NowState.ExitIfConditions != null)
                if(NowState.ExitIfConditions.Count > 0)
                {
                    double[] RawInputs = GetRawInputs(NowState.ExitIfConditions, Holder.StartInfo, PastFrameRecorder.instance.GetControllerInfo(side), PastFrameRecorder.instance.PastFrame(side));
                    int CoefficentStart = NowState.SingleConditions.Count * 2;
                    for (int i = 0; i < RawInputs.Length; i++)
                    {
                        if(RawInputs[i] > NowState.Coefficents[CoefficentStart + (i * 2)] && RawInputs[i] < NowState.Coefficents[CoefficentStart + (i * 2) + 1])
                        {
                            SequenceReset();
                            return;

                        }
                    }
                }
                    



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
            void SequenceReset() { DisableMotionSequence(Motion, side); }
            bool AtMax() { return Holder.SequenceState == Condition.Sequences.Count - 1; }
            ConditionStats[(int)side, (int)Motion - 1] = Holder;
        }
        public void DisableMotionSequence(MotionState Motion, Side side)
        {
            ConditionProgress Holder = ConditionStats[(int)side, (int)Motion - 1];
            MotionConditionInfo Condition = conditions.MotionConditions[(int)Motion - 1];

            Holder.Reset();
            for (int i = 0; i < Condition.Sequences.Count; i++)
            {
                Condition.DoEvent(side, false, i, Condition.CastLevel);
            }
        }
        public bool TestCondition(SingleSequenceState SequenceCondition, SingleInfo Point, SingleInfo Last, SingleInfo Now)//onyl call if motion works
        {
            if (SequenceCondition == null)
                return true;
            
            
            double[] RawInputs = GetRawInputs(SequenceCondition.SingleConditions, Point, Last, Now);

            if (SequenceCondition.RegressionBased)
            {
                int Degrees = (SequenceCondition.Coefficents.Length - 1) / SequenceCondition.SingleConditions.Count;
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

        double[] GetRawInputs(List<SingleConditionInfo> Trials, SingleInfo Point, SingleInfo Last, SingleInfo Now)
        {
            double[] RawInputs = new double[Trials.Count];
            for (int i = 0; i < Trials.Count; i++)
            {
                SingleInfo Frame1 = Trials[i].frameType == UseFrameType.LastPoint ? Point : Last;
                SingleInfo Frame2 = Trials[i].frameType == UseFrameType.LastPoint ? Now : Now; //current

                RawInputs[i] = ConditionDictionary[Trials[i].condition].Invoke(Trials[i].restriction, Frame1, Frame2);
            }
            return RawInputs;
        }
    }
    [Serializable]
    public class SingleSequenceState
    {
        public string StateToActivate;
        public bool RegressionBased;
        public bool NewFrameOnDone;
        public bool ExitIfAny;
        public WaitType waitType;
        public double[] Coefficents;

       [Range(0f,1f), ShowIf("RegressionBased")] public float CutoffValue;
        [ListDrawerSettings(ShowIndexLabels = true, ListElementLabelName = "Label")] public List<SingleConditionInfo> SingleConditions;
        [ListDrawerSettings(ShowIndexLabels = true, ListElementLabelName = "Label"), ShowIf("ExitIfAny")] public List<SingleConditionInfo> ExitIfConditions;
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