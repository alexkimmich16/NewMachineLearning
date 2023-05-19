using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System;
namespace RestrictionSystem
{
    [CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/MotionSettings", order = 2), Serializable]
    public class MotionSettings : SerializedScriptableObject
    {
        public int FramesAgo;
        [ListDrawerSettings(ShowIndexLabels = true)] public List<RegressionInfo> Coefficents;
        [ListDrawerSettings(ShowIndexLabels = true, ListElementLabelName = "Motion")] public List<MotionRestriction> MotionRestrictions;
        [ListDrawerSettings(ShowIndexLabels = true)] public List<FrameLogicInfo> LogicInfo;
        [ListDrawerSettings(ShowIndexLabels = true, ListElementLabelName = "Motion")] public List<MotionConditionInfo> MotionConditions;
    }
    [System.Serializable]
    public struct RegressionInfo
    {
        public float Intercept;
        public List<DegreeList> Coefficents;

        [System.Serializable]
        public struct DegreeList
        {
            public List<float> Degrees;
        }
        public float[] GetCoefficents()
        {
            float[] ReturnValue = new float[(Coefficents.Count * Coefficents[0].Degrees.Count) + 1];
            ReturnValue[0] = Intercept;
            for (int i = 0; i < Coefficents.Count; i++)
                for (int j = 0; j < Coefficents[i].Degrees.Count; j++)
                    ReturnValue[(i * Coefficents[i].Degrees.Count) + j + 1] = Coefficents[i].Degrees[j];

            return ReturnValue;
        }
    }
    [System.Serializable]
    public class FrameLogicInfo
    {
        public bool FrameLogicEnabled = true;
        [ShowIf("FrameLogicEnabled")]public float MostTime = 0.5f;
        [ShowIf("FrameLogicEnabled") ,Range(0, 100f)] public float Spread = 0f;
        [Range(0f, 1f)] public float CutoffValue = 0.5f;
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
        public MotionRestriction()
        {

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
        public static List<Restriction> LocalHandPosRestrictions = new List<Restriction>() { Restriction.VelocityInDirection, Restriction.HandFacing, Restriction.VelocityThreshold, Restriction.TimedAngleChange, Restriction.AngleChange };

        [ShowIf("RequiresLocalRotOption")] public bool UseLocalHandRot;
        private bool RequiresLocalRotOption() { return LocalHandRotRestrictions.Contains(restriction); }
        public static List<Restriction> LocalHandRotRestrictions = new List<Restriction>() { Restriction.VelocityInDirection, Restriction.HandFacing, Restriction.TimedAngleChange, Restriction.AngleChange };



        [ShowIf("ShowOld")] public Vector3 Offset;
        [ShowIf("RequiresOffset")] public Vector3 Direction;
        private bool RequiresOffset() { return RequiresOffsetRestrictions.Contains(restriction); }
        public static List<Restriction> RequiresOffsetRestrictions = new List<Restriction>() { Restriction.VelocityInDirection, Restriction.HandFacing };

        [ShowIf("RequiresAxisList")] public List<Axis> UseAxisList;
        private bool RequiresAxisList() { return AxisListRestrictions.Contains(restriction); }
        public static List<Restriction> AxisListRestrictions = new List<Restriction>() { Restriction.HandHeadDistance, Restriction.VelocityThreshold, Restriction.HandFacing, Restriction.VelocityInDirection, Restriction.TimedAngleChange, Restriction.AngleChange };

        [ShowIf("ShowOld")] public bool ShouldDebug;
        [ReadOnly, ShowIf("ShowOld")] public float Value;
        public string Title { get { return Label; } }
    }
}

