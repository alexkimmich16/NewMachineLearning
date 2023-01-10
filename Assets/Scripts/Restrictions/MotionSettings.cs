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

        [Range(0f, 1f)] public float WeightedValueThreshold = 0.8f;


        [ListDrawerSettings(ListElementLabelName = "Label")]
        public List<SingleRestriction> Restrictions;

        public string Title { get { return Motion; } }

    }
    [Serializable]
    public class SingleRestriction
    {
        public string Label;
        public bool Active = true;
        [ShowIf("Active"), Range(0f, 1f)] public float Weight = 1f;
        public Restriction restriction;
        [ShowIf("restriction", Restriction.VelocityInDirection)] public VelocityType CheckType;
        public float MaxSafe;
        public float MinSafe;
        public float MinFalloff;
        public float MaxFalloff;

        

        private bool RequiresOffset() { return restriction == Restriction.VelocityInDirection || restriction == Restriction.HandFacingHead; }


        [ShowIf("RequiresOffset")] public Vector3 Offset;
        [ShowIf("RequiresOffset")] public Vector3 Direction;

        [ShowIf("restriction", Restriction.HandFacingHead)] public bool ExcludeHeight;
        //[ShowIf("VelocityInHandOrHead")] public Vector3 ForwardDirection;

        //[ShowIf("restriction", Restriction.HandFacingHead)] public Axis UseAxis;


        [ShowIf("restriction", Restriction.HandHeadDistance)] public List<Axis> UseAxisList = new List<Axis>() { Axis.X, Axis.Y, Axis.Z };

        public bool ShouldDebug;
        [ReadOnly] public float Value;
        public float GetValue(float Input)
        {
            //if()
            Value = Input;
            //Debug.Log("Label: " + Label);
            if (Input < MaxSafe && Input > MinSafe)
                return 1f;
            else if (Input < MinFalloff || Input > MaxFalloff)
                return 0f;
            else
            {
                bool IsLowSide = Input > MinFalloff && Input < MinSafe;
                float DistanceValue = IsLowSide ? 1f - Remap(Input, new Vector2(MinFalloff, MinSafe)) : Remap(Input, new Vector2(MaxSafe, MaxFalloff));
                return DistanceValue;
                //input falloff value -> chart to get the true value
                //compair to restriction to get falloff value
            }
            float Remap(float Input, Vector2 MaxMin) { return (Input - MaxMin.x) / (MaxMin.y - MaxMin.x); }
        }
        public string Title { get { return Label; } }
    }
}

