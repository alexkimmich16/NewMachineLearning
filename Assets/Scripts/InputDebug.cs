using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Athena;
public class InputDebug : MonoBehaviour
{
    public List<DeviceTestRef> References;
    [System.Serializable]
    public class DeviceTestRef
    {
        public Transform Pos, Vel, Acc;

        public void Set(DeviceInfo Info)
        {
            Pos.position = Info.Pos;
            Vel.position = Info.velocity;
            Acc.position = Info.acceleration;
            Pos.rotation = Quaternion.Euler(Info.Rot);
            Vel.rotation = Quaternion.Euler(Info.angularVelocity);
            Acc.rotation = Quaternion.Euler(Info.angularAcceleration);
        }
    }
    
    public PastFrameRecorder P => PastFrameRecorder.instance;


    // Update is called once per frame
    void Update()
    {
        if (PastFrameRecorder.IsReady)
        {
            References[0].Set(P.FrameInfo[^1].Devices[0]);
        }
    }
}
