using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RestrictionSystem;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using Sirenix.OdinInspector;
using System.Linq;
namespace System.Runtime.CompilerServices
{
    internal class IsExternalInit { }
}
public class DebugInputs : SerializedMonoBehaviour
{
    public class XRValues
    {
        public Vector3 Pos;
        public Quaternion Rot;
        public Vector3 Vel;
        public Vector3 VelRot;
        public Vector3 Acc;
        public Vector3 AccRot;

        private Vector3 LastVel;
        private Vector3 LastVelRot;
        private float LastTime;

        public XRValues(XRNode Device)
        {
            InputDevices.GetDeviceAtXRNode(Device).TryGetFeatureValue(CommonUsages.devicePosition, out Pos);
            InputDevices.GetDeviceAtXRNode(Device).TryGetFeatureValue(CommonUsages.deviceRotation, out Rot);
            InputDevices.GetDeviceAtXRNode(Device).TryGetFeatureValue(CommonUsages.deviceVelocity, out Vel);
            InputDevices.GetDeviceAtXRNode(Device).TryGetFeatureValue(CommonUsages.deviceAngularVelocity, out VelRot);



            if (Vel != Vector3.zero)
            {
                float deltaTime = Time.time - LastTime;
                Acc = (Vel - LastVel) / deltaTime;
                AccRot = (VelRot - LastVelRot) / deltaTime;
            }
            

            // Use acceleration here...

            this.LastVel = Vel;
            this.LastVelRot = VelRot;
            this.LastTime = Time.time;

            //InputDevices.GetDeviceAtXRNode(Device).TryGetFeatureValue(CommonUsages.deviceAcceleration, out Acc);
            //InputDevices.GetDeviceAtXRNode(Device).TryGetFeatureValue(CommonUsages.deviceAngularAcceleration, out AccRot);

        }
    }
    
    [System.Serializable]
    public struct XRReferences
    {
        public Transform Hand;
        public Transform Vel;
        public Transform Acc;

    }
    public XRReferences RightHandRef;
    public XRReferences LeftHandRef;
    public XRReferences HeadRef;

    public XRValues RightHand;
    public XRValues LeftHand;
    public XRValues Head;


    //[System.Serializable]
    //public record XRValues(Vector3 Pos, Quaternion Rot, Vector3 Vel, Vector3 Acc);
   

    // Update is called once per frame
    void Update()
    {
        /*
        List<InputDevice> devices = new List<InputDevice>();
        InputDeviceCharacteristics rightHandCharacteristics = InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller;
        InputDevices.GetDevicesWithCharacteristics(rightHandCharacteristics, devices);
        InputDevice rightHandDevice = devices.FirstOrDefault(x => x.TryGetFeatureValue(CommonUsages.deviceAcceleration, out Vector3 velocity));
        bool Works = rightHandDevice.TryGetFeatureValue(CommonUsages.deviceAcceleration, out Vector3 velocity);
        Debug.Log(Works);

        //LeftHand = new XRValues(LeftPos, LeftRot, LeftVel, LeftAcc);
        
        //Head = new XRValues(HeadPos, HeadRot, HeadVel, HeadAcc);
        */
        //DebugDevice(LeftHand, LeftHandRef);
        RightHand = new XRValues(XRNode.RightHand);
        DebugDevice(RightHand, RightHandRef);

        //Debug.Log(RightAcc);
        //DebugDevice(Head, HeadRef);
    }

    public void DebugDevice(XRValues XRDevice, XRReferences Ref)
    {
        Ref.Hand.position = XRDevice.Pos;
        Ref.Hand.rotation = XRDevice.Rot;
        Ref.Vel.position = XRDevice.Vel;
        Ref.Vel.rotation = Quaternion.Euler(XRDevice.VelRot);


        Ref.Acc.position = XRDevice.Acc;
        Ref.Acc.rotation = Quaternion.Euler(XRDevice.AccRot);
    }
}
