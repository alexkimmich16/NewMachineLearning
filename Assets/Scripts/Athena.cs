using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RestrictionSystem;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using Sirenix.OdinInspector;
using System.Linq;

public class Athena : SerializedMonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

[System.Serializable]
public class AthenaSpell : ScriptableObject
{
    [ListDrawerSettings(Expanded = false, ShowIndexLabels = true)] public List<AthenaMotion> Motions;
    //public Vector2 Punishment = new Vector2(-1, -1);
    //public Vector2 Reward = new Vector2(1,1);

    //x is i guessed false
    public DeviceInfo GetRestrictionInfoAtIndex(int Motion, int Frame) { return Motions[Motion].Infos[Frame]; }
}

[System.Serializable]
public class AthenaMotion
{
    //[HideInInspector]
    public List<DeviceInfo> Infos;
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
    public DeviceInfo[] Devices;
    
    public List<float> AsInputs() { return Devices.SelectMany(x => x.AsFloats()).ToList(); }
    public AthenaFrame(DeviceInfo[] Devices)
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


    public List<float> AsFloats() { return new List<Vector3>() { Pos, Rot, velocity, angularVelocity, acceleration, angularAcceleration }.SelectMany(vec => new[] { vec.x, vec.y, vec.z }).ToList(); }
}