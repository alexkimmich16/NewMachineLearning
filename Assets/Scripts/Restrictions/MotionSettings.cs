using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System;
namespace RestrictionSystem
{
    
    [CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/MotionSettings", order = 2)]
    public class MotionSettings : SerializedScriptableObject
    {
        public float Iteration;

        [ListDrawerSettings(ShowIndexLabels = true, ListElementLabelName = "Motion")] public List<MotionRestriction> MotionRestrictions;

        /*
        public void ResetUnneccicaryInfo()
        {
            for (int i = 0; i < MotionConditions.Count; i++)
            {
                MotionConditions[i].CurrentStage = new List<int>() {0,0 };
                MotionConditions[i].WaitingForFalse = new List<bool>() { false, false };
                for (int j = 0; j < MotionConditions[i].ConditionLists.Count; j++)
                {
                    for (int k = 0; k < MotionConditions[i].ConditionLists[j].SingleConditions.Count; k++)
                    {
                        MotionConditions[i].ConditionLists[j].SingleConditions[k].LastState = new List<bool>() { false, false };
                        MotionConditions[i].ConditionLists[j].SingleConditions[k].StartTime = new List<float>() { 0f, 0f } ;
                        MotionConditions[i].ConditionLists[j].SingleConditions[k].StartPos = new List<Vector3>() { Vector3.zero, Vector3.zero };
                        MotionConditions[i].ConditionLists[j].SingleConditions[k].Value = new List<float>() { 0f, 0f };
                    }
                        
                }
            }
        }
        */
    }
    [System.Serializable]
    public class MotionRestriction
    {
        public MotionRestriction(MotionRestriction All)
        {
            this.Motion = All.Motion;
            this.Restrictions = new List<SingleRestriction>(All.Restrictions);
        }
        public string Motion;

        [ListDrawerSettings(ListElementLabelName = "Label")]
        public List<SingleRestriction> Restrictions;
    }
    [Serializable]
    public class SingleRestriction
    {
        public string Label;
        public bool Active;
        public float Weight;
        public Restriction restriction;
        [ShowIf("RequiresCheckType")] public CheckType checkType;
        private bool RequiresCheckType() { return restriction == Restriction.VelocityInDirection || restriction == Restriction.HandFacing; }
        [ShowIf("checkType", CheckType.Other)]
        public Vector3 OtherDirection;


        public float MaxSafe;
        public float MinSafe;
        public float MinFalloff;
        public float MaxFalloff;

        private bool RequiresOffset() { return restriction == Restriction.VelocityInDirection || restriction == Restriction.HandFacing; }
        private bool RequiresAxisList() { return restriction == Restriction.HandHeadDistance || restriction == Restriction.VelocityThreshold || restriction == Restriction.HandFacing || restriction == Restriction.VelocityInDirection; }


        [ShowIf("RequiresOffset")] public Vector3 Offset;
        [ShowIf("RequiresOffset")] public Vector3 Direction;

        [ShowIf("RequiresAxisList")] public List<Axis> UseAxisList;

        public bool ShouldDebug;
        [ReadOnly] public float Value;
        public float GetValue(float Input)
        {
            Value = Input;
            if (Input < MaxSafe && Input > MinSafe)
                return 1f;
            else if (Input < MinFalloff || Input > MaxFalloff)
                return 0f;
            else
            {
                bool IsLowSide = Input > MinFalloff && Input < MinSafe;
                float DistanceValue = IsLowSide ? 1f - Remap(Input, new Vector2(MinFalloff, MinSafe)) : Remap(Input, new Vector2(MaxSafe, MaxFalloff));
                return DistanceValue;
            }
            float Remap(float Input, Vector2 MaxMin) { return (Input - MaxMin.x) / (MaxMin.y - MaxMin.x); }
        }
        public string Title { get { return Label; } }
    }
}

