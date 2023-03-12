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
    }
    [System.Serializable]
    public class MotionRestriction
    {
        public string Motion;
        [ListDrawerSettings(ListElementLabelName = "Label", ShowIndexLabels = true)] public List<SingleRestriction> Restrictions;
        public MotionRestriction(MotionRestriction All)
        {
            this.Motion = All.Motion;
            this.Restrictions = new List<SingleRestriction>(All.Restrictions);
        }
        public MotionRestriction(string Motion, List<SingleRestriction> Restrictions)
        {
            this.Motion = Motion;
            this.Restrictions = new List<SingleRestriction>(Restrictions);
        }
        
    }
    [Serializable]
    public class SingleRestriction
    {
        public string Label;
        [ShowIf("ShowOld")] public bool Active;

        private static bool ShowOld = false;
        
        public Restriction restriction;


        [ShowIf("RequiresCheckType")] public CheckType checkType;
        [ShowIf("checkType", CheckType.Other)] public Vector3 OtherDirection;
        private bool RequiresCheckType() { return CheckTypeRestrictions.Contains(restriction); }
        public static List<Restriction> CheckTypeRestrictions = new List<Restriction>() { Restriction.VelocityInDirection, Restriction.HandFacing };

        [ShowIf("RequiresLocalPosOption")] public bool UseLocalHandPos;
        private bool RequiresLocalPosOption() { return LocalHandPosRestrictions.Contains(restriction); }
        public static List<Restriction> LocalHandPosRestrictions = new List<Restriction>() { Restriction.VelocityInDirection, Restriction.HandFacing, Restriction.VelocityThreshold };

        [ShowIf("RequiresLocalRotOption")] public bool UseLocalHandRot;
        private bool RequiresLocalRotOption() { return LocalHandRotRestrictions.Contains(restriction); }
        public static List<Restriction> LocalHandRotRestrictions = new List<Restriction>() { Restriction.VelocityInDirection, Restriction.HandFacing};



        [ShowIf("ShowOld")] public Vector3 Offset;
        [ShowIf("RequiresOffset")] public Vector3 Direction;
        private bool RequiresOffset() { return RequiresOffsetRestrictions.Contains(restriction); }
        public static List<Restriction> RequiresOffsetRestrictions = new List<Restriction>() { Restriction.VelocityInDirection, Restriction.HandFacing };

        [ShowIf("RequiresAxisList")] public List<Axis> UseAxisList;
        private bool RequiresAxisList() { return AxisListRestrictions.Contains(restriction); }
        public static List<Restriction> AxisListRestrictions = new List<Restriction>() { Restriction.HandHeadDistance, Restriction.VelocityThreshold, Restriction.HandFacing, Restriction.VelocityInDirection };

        [ShowIf("ShowOld")] public float Weight;
        [ShowIf("ShowOld")] public float MaxFalloff;
        [ShowIf("ShowOld")] public float MaxSafe;
        [ShowIf("ShowOld")] public float MinSafe;
        [ShowIf("ShowOld")] public float MinFalloff;
        public void SetOutputValue(int Index, float Value)
        {
            if (Index == 0)
                MaxSafe = Value;
            if (Index == 1)
                MinSafe = Value;
            if (Index == 2)
                MaxFalloff = Value;
            if (Index == 3)
                MinFalloff = Value;
            if (Index == 4)
                Weight = Value;
        }

        [ShowIf("ShowOld")] public bool ShouldDebug;
        [ReadOnly, ShowIf("ShowOld")] public float Value;
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

