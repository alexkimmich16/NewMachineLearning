using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RestrictionSystem;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using Sirenix.OdinInspector;
using System.Linq;


public enum Side
{
    right = 0,
    left = 1,
}
public enum Spell
{
    Nothing = 0,
    Fireball = 1,
    Flames = 2,
    SideParry = 3,
    UpParry = 4,
}

namespace Athena
{
    public class Athena : SerializedMonoBehaviour
    {
        public static Athena instance;
        private void Awake() { instance = this; }

        [FoldoutGroup("Movements")] public List<AthenaSpell> Movements;

        public int MotionCount() { return Movements.Count; }
        public int MovementCount(Spell Spell) { return Movements[(int)Spell].Motions.Count; }

        public int FrameCount(Spell Spell, int Motion) { return Movements[(int)Spell].Motions[Motion].Infos.Count; }
        public AthenaFrame AtFrameInfo(Spell Spell, int Motion, int Frame) { return Movements[(int)Spell].Motions[Motion].Infos[Frame]; }
        public int TrueRangeCount(Spell Spell, int Motion) { return Movements[(int)Spell].Motions[Motion].TrueRanges.Count; }
        public bool FrameWorks(Spell Spell, int Motion, int Frame) { return Movements[(int)Spell].Motions[Motion].AtFrameState(Frame); }

    }

    [System.Serializable]
    public class AthenaSpell : ScriptableObject
    {
        [ListDrawerSettings(Expanded = false, ShowIndexLabels = true)] public List<AthenaMotion> Motions;
    }

    [System.Serializable]
    public class AthenaMotion
    {
        public bool AtFrameState(int Frame) { return TrueRanges.Any(range => Frame >= range.x && Frame <= range.y); }

        public List<AthenaFrame> Infos;
        public List<Vector2> TrueRanges;
        [HideInInspector] public int TrueIndex;
        [HideInInspector] public int PlayCount;

        public static List<Vector2> ConvertToRange(List<bool> Values)
        {
            List<Vector2> ranges = new List<Vector2>();
            bool Last = false;
            int Start = 0;
            if (Values.All(state => state == false))
            {
                return new List<Vector2> { new Vector2(-1f, -1f) };
            }

            for (int i = 0; i < Values.Count; i++)
            {
                if (Values[i] != Last)//onchange
                {
                    if (Last == false)
                    {
                        Start = i;
                    }
                    else if (Last == true)
                    {
                        //range is start to i
                        ranges.Add(new Vector2(Start, i - 1));
                    }

                    Last = Values[i];
                }
                else if (Last == true && i == Values.Count - 1)
                {
                    ranges.Add(new Vector2(Start, i - 1));
                }
            }
            return ranges;
        }
    }

    public class AthenaFrame
    {
        public List<DeviceInfo> Devices;
        public float frameTime;
        public List<float> AsInputs()
        {
            List<float> Inputs = Devices.SelectMany(x => x.AsFloats()).ToList();
            Inputs.Add(frameTime);
            return Inputs;
        }
        public AthenaFrame(List<DeviceInfo> Devices)
        {
            this.Devices = Devices;
        }
    }

    public class DeviceInfo
    {
        public Vector3 Pos;
        public Vector3 Rot;

        public Vector3 velocity;
        public Vector3 angularVelocity;

        public Vector3 acceleration;
        public Vector3 angularAcceleration;

        public List<float> AsFloats()
        {
            //Vector3
            if (true)
                return new List<Vector3>() { Pos, Rot, velocity, angularVelocity, acceleration, angularAcceleration }.SelectMany(vec => new[] { vec.x, vec.y, vec.z }).ToList();
            else
                return new List<Vector3>() { Pos, Rot, velocity, angularVelocity, acceleration, angularAcceleration }.SelectMany(vec => new[] { vec.x, vec.y, vec.z }).ToList();
        }

        //public List<float> () { return new List<Vector3>() { Pos, Rot, velocity, angularVelocity, acceleration, angularAcceleration }.SelectMany(vec => new[] { vec.x, vec.y, vec.z }).ToList(); }
    }
}
