using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System;
using System.Linq;
namespace RestrictionSystem
{
    public enum Restriction
    {
        VelocityThreshold = 0,
        VelocityInDirection = 1,
        HandFacing = 2,
        HandHeadDistance = 3,
        HandToHeadAngle = 4,
    }
    
    public enum Axis
    {
        X = 0,
        Y = 1,
        Z = 2,
    }
    public enum CheckType
    {
        Head = 0,
        Hand = 1,
        Other = 2,
    }
    public enum Side
    {
        right = 0,
        left = 1,
    }
    public enum CurrentLearn
    {
        Nothing = 0,
        Fireball = 1,
        Flames = 2,
        FlameBlock = 3,
    }



    public delegate float RestrictionTest(SingleRestriction restriction, SingleInfo frame1, SingleInfo frame2);
    public class RestrictionManager : SerializedMonoBehaviour
    {
        public static RestrictionManager instance;
        private void Awake() { instance = this; }

        public static Dictionary<Restriction, RestrictionTest> RestrictionDictionary = new Dictionary<Restriction, RestrictionTest>(){
            {Restriction.VelocityThreshold, VelocityMagnitude},
            {Restriction.VelocityInDirection, VelocityDirection},
            {Restriction.HandFacing, HandFacing},
            {Restriction.HandHeadDistance, HandHeadDistance},
            {Restriction.HandToHeadAngle, HandToHeadAngle},
        };
        public MotionSettings RestrictionSettings;

        public void TriggerFrameEvents(List<bool> Sides)
        {
            PastFrameRecorder PR = PastFrameRecorder.instance;
            for (int i = 0; i < 2; i++)
            {
                List<CurrentLearn> WorkingMotions = AllWorkingMotions(PR.PastFrame((Side)i), PastFrameRecorder.instance.GetControllerInfo((Side)i));
                if (Sides[i] == true)
                    for (int j = 1; j < RestrictionSettings.MotionRestrictions.Count + 1; j++)
                        ConditionManager.instance.PassValue(WorkingMotions.Contains((CurrentLearn)j), (CurrentLearn)j, (Side)i);
            }
        }
        public static bool MotionWorks(SingleInfo frame1, SingleInfo frame2, MotionRestriction restriction)
        {
            float TotalWeightValue = 0f;//all working weights
            for (int i = 0; i < restriction.Restrictions.Count; i++)
            {
                RestrictionTest RestrictionType = RestrictionDictionary[restriction.Restrictions[i].restriction];
                float RawRestrictionValue = RestrictionType.Invoke(restriction.Restrictions[i], frame1, frame2);
                restriction.Restrictions[i].Value = RawRestrictionValue;
                float RestrictionValue = restriction.Restrictions[i].GetValue(RawRestrictionValue);

                if (restriction.Restrictions[i].Active)
                    TotalWeightValue += RestrictionValue * restriction.Restrictions[i].Weight;
            }
            return TotalWeightValue >= (float)restriction.Restrictions.Count * 0.85f;
        }
        public List<CurrentLearn> AllWorkingMotions(SingleInfo frame1, SingleInfo frame2) { return Enumerable.Range(0, RestrictionSettings.MotionRestrictions.Count).Where(t => MotionWorks(frame1, frame2, RestrictionSettings.MotionRestrictions[t])).Select(t => (CurrentLearn)(t + 1)).ToList(); }
        public static Vector3 EliminateAxis(List<Axis> AllAxis, Vector3 Value) { return new Vector3(AllAxis.Contains(Axis.X) ? Value.x : 0, AllAxis.Contains(Axis.Y) ? Value.y : 0, AllAxis.Contains(Axis.Z) ? Value.z : 0); }
        #region Values
        public static float VelocityMagnitude(SingleRestriction restriction, SingleInfo frame1, SingleInfo frame2)
        {
            
            float Distance = Vector3.Distance(EliminateAxis(restriction.UseAxisList, frame1.HandPos), EliminateAxis(restriction.UseAxisList, frame2.HandPos));
            float Speed = Distance / (frame2.SpawnTime - frame1.SpawnTime);
            //restriction.Value = Speed;
            return Speed;
        }
        public static float VelocityDirection(SingleRestriction restriction, SingleInfo frame1, SingleInfo frame2)
        {
            Vector3 ForwardInput = restriction.checkType == CheckType.Other ? restriction.OtherDirection : restriction.checkType == CheckType.Head ? frame2.HeadRot : frame2.HandRot;
            Vector3 forwardDir = EliminateAxis(restriction.UseAxisList, Quaternion.Euler(ForwardInput + restriction.Offset) * restriction.Direction);

            float AngleDistance = Vector3.Angle(EliminateAxis(restriction.UseAxisList, (frame2.HandPos - frame1.HandPos).normalized), forwardDir);
            if (restriction.ShouldDebug)
            {
                Debug.DrawLine(frame2.HandPos, frame2.HandPos + EliminateAxis(restriction.UseAxisList, (frame2.HandPos - frame1.HandPos).normalized) * DebugRestrictions.instance.LineLength, Color.yellow);
                Debug.DrawLine(frame2.HandPos, frame2.HandPos + (forwardDir * DebugRestrictions.instance.LineLength), Color.red);
            }
            //restriction.Value = AngleDistance;
            return AngleDistance;
        }
        public static float HandFacing(SingleRestriction restriction, SingleInfo frame1, SingleInfo frame2)
        {
            Vector3 HandDir = EliminateAxis(restriction.UseAxisList, (Quaternion.Euler(frame2.HandRot + restriction.Offset) * restriction.Direction));
            Vector3 OtherDir = EliminateAxis(restriction.UseAxisList, restriction.checkType == CheckType.Other ? restriction.OtherDirection : (-frame2.HandPos).normalized);
            if (restriction.ShouldDebug)
            {
                Debug.DrawLine(frame2.HandPos, frame2.HandPos + (OtherDir * DebugRestrictions.instance.LineLength), Color.yellow);
                Debug.DrawLine(frame2.HandPos, frame2.HandPos + (HandDir * DebugRestrictions.instance.LineLength), Color.red);
            }
            //restriction.Value = Vector3.Angle(HandDir, OtherDir);
            return Vector3.Angle(HandDir, OtherDir);
        }
        public static float HandHeadDistance(SingleRestriction restriction, SingleInfo frame1, SingleInfo frame2)
        {
            Vector3 HeadPos = EliminateAxis(restriction.UseAxisList, frame2.HeadPos);
            Vector3 HandPos = EliminateAxis(restriction.UseAxisList, frame2.HandPos);
            //restriction.Value = Vector3.Distance(HeadPos, HandPos);
            return Vector3.Distance(HeadPos, HandPos);
        }
        public static float HandToHeadAngle(SingleRestriction restriction, SingleInfo frame1, SingleInfo frame2)
        {
            Vector3 targetDir = new Vector3(frame2.HandPos.x, 0, frame2.HandPos.z).normalized;
            Quaternion quat = Quaternion.Euler(new Vector3(frame2.HeadPos.x, 0, frame2.HeadPos.z));
            Vector3 forwardDir = (quat * Vector3.forward).normalized;
            float Angle = frame2.HeadRot.y + Vector3.SignedAngle(targetDir, forwardDir, Vector3.up) + 180f;
            //Offset
            if (Angle > 360 || Angle < -360)
                Angle += Angle > 360 ? -360 : 360;
            //restriction.Value = Angle;
            return Angle;
            
        }
        #endregion
    }
}

