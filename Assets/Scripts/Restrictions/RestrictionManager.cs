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
        public Coefficents coefficents;
        
        public void TriggerFrameEvents(List<bool> Sides)
        {
            PastFrameRecorder PR = PastFrameRecorder.instance;
            for (int i = 0; i < 2; i++)
            {
                if (Sides[0] == false && Sides[1] == false)
                    return;
                for (int j = 1; j < coefficents.RegressionStats.Count + 1; j++)
                {
                    //Debug.Log("test");
                    bool Works = MotionWorks(PR.PastFrame((Side)i), PastFrameRecorder.instance.GetControllerInfo((Side)i), (CurrentLearn)j);
                    if (Sides[i] == true)
                        ConditionManager.instance.PassValue(Works, (CurrentLearn)j, (Side)i);
                }
            }
        }

        public bool MotionWorks(SingleInfo Frame1, SingleInfo Frame2, CurrentLearn motionType)
        {
            List<float> TestValues = new List<float>();
            for (int i = 0; i < RestrictionSettings.MotionRestrictions[0].Restrictions.Count; i++)
            {
                TestValues.Add(RestrictionDictionary[RestrictionSettings.MotionRestrictions[0].Restrictions[i].restriction].Invoke(RestrictionSettings.MotionRestrictions[0].Restrictions[i], Frame1, Frame2));
            }

            float Total = 0f;
            for (int j = 0; j < coefficents.RegressionStats[(int)motionType - 1].Coefficents.Count; j++)//each  variable
                for (int k = 0; k < coefficents.RegressionStats[(int)motionType - 1].Coefficents[j].Degrees.Count; k++)//powers
                    Total += Mathf.Pow(TestValues[j], k + 1) * coefficents.RegressionStats[(int)motionType - 1].Coefficents[j].Degrees[k];

            Total += coefficents.RegressionStats[(int)motionType - 1].Intercept;
            //insert formula
            float GuessValue = 1f / (1f + Mathf.Exp(-Total));
            bool Guess = GuessValue > 0.5f;
            bool Correct = Guess;
            return Correct;
        }
        public static Vector3 EliminateAxis(List<Axis> AllAxis, Vector3 Value) { return new Vector3(AllAxis.Contains(Axis.X) ? Value.x : 0, AllAxis.Contains(Axis.Y) ? Value.y : 0, AllAxis.Contains(Axis.Z) ? Value.z : 0); }
        #region Values
        public static float VelocityMagnitude(SingleRestriction restriction, SingleInfo frame1, SingleInfo frame2)
        {
            float Distance = Vector3.Distance(EliminateAxis(restriction.UseAxisList, frame1.HandPosType(restriction.UseLocalHandPos)), EliminateAxis(restriction.UseAxisList, frame2.HandPosType(restriction.UseLocalHandPos)));
            float Speed = Distance / (frame2.SpawnTime - frame1.SpawnTime);
            //restriction.Value = Speed;
            return Speed;
        }
        public static float VelocityDirection(SingleRestriction restriction, SingleInfo frame1, SingleInfo frame2)
        {
            Vector3 ForwardInput = restriction.checkType == CheckType.Other ? restriction.OtherDirection : restriction.checkType == CheckType.Head ? frame2.HeadRot : frame2.HandRotType(restriction.UseLocalHandRot);
            Vector3 forwardDir = EliminateAxis(restriction.UseAxisList, Quaternion.Euler(ForwardInput + restriction.Offset) * restriction.Direction);

            float AngleDistance = Vector3.Angle(EliminateAxis(restriction.UseAxisList, (frame2.HandPosType(restriction.UseLocalHandPos) - frame1.HandPosType(restriction.UseLocalHandPos)).normalized), forwardDir);
            if (restriction.ShouldDebug)
            {
                //Debug.DrawLine(frame2.HandPos, frame2.HandPos + EliminateAxis(restriction.UseAxisList, (frame2.HandPosType(restriction.UseLocalHandPos) - frame1.HandPosType(restriction.UseLocalHandPos)).normalized) * DebugRestrictions.instance.LineLength, Color.yellow);
                //Debug.DrawLine(frame2.HandPos, frame2.HandPos + (forwardDir * DebugRestrictions.instance.LineLength), Color.red);
            }
            //restriction.Value = AngleDistance;
            return AngleDistance;
        }
        public static float HandFacing(SingleRestriction restriction, SingleInfo frame1, SingleInfo frame2)
        {
            Vector3 HandDir = EliminateAxis(restriction.UseAxisList, (Quaternion.Euler(frame2.HandRotType(restriction.UseLocalHandRot) + restriction.Offset) * restriction.Direction));
            Vector3 OtherDir = EliminateAxis(restriction.UseAxisList, restriction.checkType == CheckType.Other ? restriction.OtherDirection : -(frame2.HandPosType(restriction.UseLocalHandPos)).normalized);
            if (restriction.ShouldDebug)
            {
                //Debug.DrawLine(frame2.HandPos, frame2.HandPos + (OtherDir * DebugRestrictions.instance.LineLength), Color.yellow);
                //Debug.DrawLine(frame2.HandPos, frame2.HandPos + (HandDir * DebugRestrictions.instance.LineLength), Color.red);
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

