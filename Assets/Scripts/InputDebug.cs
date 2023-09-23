using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Athena;
using Sirenix.OdinInspector;
public class InputDebug : SerializedMonoBehaviour
{
    public static InputDebug instance;
    private void Awake() { instance = this; }

    public List<DeviceTestRef> References;

    public Dictionary<Side, SkinnedMeshRenderer> Hands;
    public List<Material> Materials;


    public float RotMutiplier, AngularVelMutiplier, AngularAccMutiplier;

    public PastFrameRecorder P => PastFrameRecorder.instance;

    [System.Serializable]
    public class DeviceTestRef
    {
        public Transform Pos, Vel, Acc;

        public void Set(DeviceInfo Info)
        {
            Pos.position = Info.Pos;
            Vel.position = Info.velocity;
            //Acc.position = Info.acceleration;
            Pos.rotation = Quaternion.Euler(Info.Rot * InputDebug.instance.RotMutiplier);
            Vel.rotation = Quaternion.Euler(Info.angularVelocity * InputDebug.instance.AngularVelMutiplier);
            //Acc.rotation = Quaternion.Euler(Info.angularAcceleration * InputDebug.instance.AngularAccMutiplier);
        }
    }
    private void Start()
    {
        Runtime.StateChange += RecieveStateChange;
    }

    public void RecieveStateChange(Side side, int Index)
    {
        Hands[side].material = Materials[Index];
    }
    void Update()
    {
        if (PastFrameRecorder.IsReady)
        {
            References[0].Set(P.GetFramesList(Side.right, 1)[0].Devices[0]);
            
            //References[1].Set(P.FrameInfo[^1].Devices[1]);
        }
    }
}
