using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RestrictionSystem;
using System;
using Sirenix.OdinInspector;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
public class VariableTest : SerializedMonoBehaviour
{
    /*
    public SideInfo RightInfo;
    //explore variables
    [Serializable]
    public class SideInfo
    {
        public Vector3 Velocity;
        public Vector3 Acceleration;
        public Vector3 WorldPosition;
        public Vector3 LocalPosition;

        public SideInfo()
        {
            WorldPosition = PastFrameRecorder.instance.PlayerHands[0].position - PastFrameRecorder.instance.Cam.position;
            LocalPosition = PastFrameRecorder.instance.PlayerHands[0].localPosition - PastFrameRecorder.instance.Cam.localPosition;
            InputDevices.GetDeviceAtXRNode(XRNode.RightHand).TryGetFeatureValue(CommonUsages.deviceVelocity, out Velocity);
            InputDevices.GetDeviceAtXRNode(XRNode.RightHand).TryGetFeatureValue(CommonUsages.deviceAcceleration, out Acceleration);
            
        }
        
    }
    private void Update()
    {
        RightInfo = new SideInfo();
    }
    */

}
