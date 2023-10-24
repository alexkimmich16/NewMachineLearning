using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Athena;
using Sirenix.OdinInspector;
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
            Vector3 Value1 = Settings.ReferenceValue(Frame, 0);
            Vector3 Value2 = Settings.ReferenceValue(Frame, 1);

            float DotValue = Vector3.Dot(Value1.normalized, Value2.normalized);
            return new List<float> { (DotValue + 1f) / 2f };
        }
        public static List<float> Distance(AthenaFrame Frame, RestrictionListItem Settings)
        {
            Vector3 Value1 = Settings.ReferenceValue(Frame, 0);
            Vector3 Value2 = Settings.ReferenceValue(Frame, 1);

            float Distance = Vector3.Distance(Value1.normalized, Value2.normalized);
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
    }
    public enum RestrictionType
    {
        Magnitude = 0,
        DirectionCompare = 1,
        Distance = 2,
        RawData = 3,
    }

    [System.Serializable]
    public struct RestrictionListItem
    {
        public RestrictionType restriction;

        public DeviceType Device1;
        public AthenaValue TestVal1;

        [ShowIf("UseDevice2")] public DeviceType Device2;
        [ShowIf("UseDevice2")] public AthenaValue TestVal2;
        private bool UseDevice2 { get { return restriction != RestrictionType.Magnitude && restriction != RestrictionType.RawData; } }
        public enum Axis { X, Y, Z }
        public List<Axis> InActiveAxis;
        

        public Vector3 AxisCut(Vector3 Input) { return new Vector3(!InActiveAxis.Contains(Axis.X) ? Input.x : 0f, !InActiveAxis.Contains(Axis.Y) ? Input.y : 0f, !InActiveAxis.Contains(Axis.Z) ? Input.z : 0f); }
        public Vector3 ReferenceValue(AthenaFrame Frame, int index)
        {
            Vector3 RawValue = index == 0 ? Frame.Devices[(int)Device1].GetValue(TestVal1) : Frame.Devices[(int)Device2].GetValue(TestVal2);
            return AxisCut(RawValue);
        }


        public List<float> GetValue(AthenaFrame Frame) { return Restrictions.RestrictionDictionary[restriction].Invoke(Frame, this); }
    }
    
}

