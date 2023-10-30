using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Athena;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using System;
namespace Athena
{
    public static class Restrictions
    {
        public delegate List<float> RestrictionTest(AthenaFrame Frame, RestrictionListItem Settings);
        public static Dictionary<RestrictionType, RestrictionTest> RestrictionDictionary = new Dictionary<RestrictionType, RestrictionTest>(){
            {RestrictionType.Magnitude, Magnitude},
            {RestrictionType.DirectionCompare, DirectionCompare},
            {RestrictionType.Distance, Distance},
            {RestrictionType.RawData, RawData},
        };



        #region Values
        public static List<float> Magnitude(AthenaFrame Frame, RestrictionListItem Settings)
        {
            Vector3 ReturnValue = Settings.ReferenceValue(Frame, 0);
            return new List<float> { ReturnValue.magnitude };
        }

        public static List<float> DirectionCompare(AthenaFrame Frame, RestrictionListItem Settings)
        {
            Vector3 Value1 = Settings.ReferenceValue(Frame, 0).normalized;
            Vector3 Value2 = Settings.ReferenceValue(Frame, 1).normalized;

            //float DotValue = (Vector3.Dot(Value1, Value2) + 1f) / 2f;
            float Angle = Vector3.Angle(Value1, Value2) / 180f;
            return new List<float> { Angle };
        }
        public static List<float> Distance(AthenaFrame Frame, RestrictionListItem Settings)
        {
            Vector3 Value1 = Settings.ReferenceValue(Frame, 0);
            Vector3 Value2 = Settings.ReferenceValue(Frame, 1);

            float Distance = Vector3.Distance(Value1, Value2);
            return new List<float> { Distance };
        }
        public static List<float> RawData(AthenaFrame Frame, RestrictionListItem Settings)
        {
            Vector3 Value1 = Settings.ReferenceValue(Frame, 0);

            List<float> values = new List<float>();
            if (!Settings.InActiveAxis.Contains(RestrictionListItem.Axis.X)) { values.Add(Value1.x); }
            if (!Settings.InActiveAxis.Contains(RestrictionListItem.Axis.Y)) {values.Add(Value1.y); }
            if (!Settings.InActiveAxis.Contains(RestrictionListItem.Axis.Z)) {values.Add(Value1.z);}

            
            return values;
        }
        #endregion
    }

    public enum DeviceType
    {
        Controller = 0,
        HeadSet = 1,
        WorldDirection = 2,
    }
    public enum RestrictionType
    {
        Magnitude = 0,
        DirectionCompare = 1,
        Distance = 2,
        RawData = 3,
    }
    public enum Direction
    {
        Forward = 0,
        Left = 1,
        Right = 2,
        Up = 3,
        Down = 4,
        Back = 5,
    }



    [System.Serializable]
    public struct RestrictionListItem
    {
        public RestrictionType restriction;

        public DeviceType Device1;
        public AthenaValue TestVal1;
        [ShowIf("UseDirection")] public Direction Dir1;

        [ShowIf("UseDevice2")] public DeviceType Device2;
        [ShowIf("UseDevice2")] public AthenaValue TestVal2;
        [ShowIf("UseDirection")] public Direction Dir2;
        private bool UseDevice2 { get { return restriction != RestrictionType.Magnitude && restriction != RestrictionType.RawData; } }
        private bool UseDirection { get { return restriction == RestrictionType.DirectionCompare; } }
        public enum Axis { X, Y, Z }
        public List<Axis> InActiveAxis;
        

        public Vector3 AxisCut(Vector3 Input) { return new Vector3(!InActiveAxis.Contains(Axis.X) ? Input.x : 0f, !InActiveAxis.Contains(Axis.Y) ? Input.y : 0f, !InActiveAxis.Contains(Axis.Z) ? Input.z : 0f); }
        public Vector3 ReferenceValue(AthenaFrame Frame, int index)
        {
            DeviceType deviceType = index == 0 ? Device1 : Device2;

            if(deviceType == DeviceType.WorldDirection)
            {
                Direction AdjustDirection = index == 0 ? Dir1 : Dir2;
                return Directions[AdjustDirection];
            }
            Vector3 RawValue = index == 0 ? Frame.Devices[(int)Device1].GetValue(TestVal1) : Frame.Devices[(int)Device2].GetValue(TestVal2);
            if(restriction == RestrictionType.DirectionCompare)
            {

                Direction AdjustDirection = index == 0 ? Dir1 : Dir2;
                if (AdjustDirection != Direction.Forward)
                {
                    Quaternion RawRotation = Quaternion.Euler(RawValue);
                    RawValue = RawRotation * Directions[AdjustDirection];


                    //RawValue = RawValue.normalized;
                    //RawValue = (Quaternion.Euler(RawValue * 360f) * AdjustDirection).normalized;
                    //RawValue = Vector3.Cross(RawValue, AdjustDirection).normalized;
                }



            }
                

            return AxisCut(RawValue);
        }

        public static Dictionary<Direction, Vector3> Directions = new Dictionary<Direction, Vector3>(){
        { Direction.Forward, Vector3.forward },
        { Direction.Left, Vector3.left },
        { Direction.Right, Vector3.right },
        { Direction.Up, Vector3.up },
        { Direction.Down, Vector3.down },
        { Direction.Back, Vector3.back },
        };


        public List<float> GetValue(AthenaFrame Frame)
        {
            List<float> Values = Restrictions.RestrictionDictionary[restriction].Invoke(Frame, this);
            for (int i = 0; i < Values.Count; i++)
                Values[i] = (float)Math.Round(Values[i], Runtime.PrintDecimals);

            return Values;
        }


    }

}

