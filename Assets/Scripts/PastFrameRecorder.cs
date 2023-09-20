using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.XR;
using System.Linq;
namespace Athena
{
    public class PastFrameRecorder : SerializedMonoBehaviour
    {
        public static PastFrameRecorder instance;
        private void Awake() { instance = this; }
        [ListDrawerSettings(ShowIndexLabels = true)] public List<AthenaFrame> FrameInfo;

        public int MaxStoreInfo = 10;

        public List<Transform> TestMain;
        public List<Transform> TestCam;
        public List<Transform> TestHand;

        public List<Transform> PlayerHands;
        public Transform Cam;

        public bool DrawDebug;

        public bool[] HandsActive;

        public delegate void OnControllerDisable(Side side);
        public static OnControllerDisable disableController;

        public List<UnityEngine.XR.Interaction.Toolkit.XRController> Controllers;


        public static List<XRNode> DeviceOrder { get { return new List<XRNode>() { XRNode.RightHand, XRNode.LeftHand, XRNode.Head }; } }

        public List<List<XRPreviousValues>> PreviousValues;
        public struct XRPreviousValues
        {
            public Vector3 LastVel;
            public Vector3 LastVelRot;
            public float LastTime;

            public XRPreviousValues(Vector3 LastVel, Vector3 LastVelRot, float LastTime)
            {
                this.LastVel = LastVel;
                this.LastVelRot = LastVelRot;
                this.LastTime = LastTime;
            }
        }

        public static Dictionary<XRNode, Side> XRHands = new Dictionary<XRNode, Side>(){{XRNode.RightHand, Side.right}, { XRNode.LeftHand, Side.left } };

        //public bool[] InvertHand;
        //118.012 to 
        public bool HandActive(Side side) { return HandsActive[(int)side]; }
        
        public List<AthenaFrame> GetFramesList(int Frames) { return Enumerable.Range(0, Frames).Select(x => FrameInfo[x]).ToList(); }
       

        
        
        public AthenaFrame GetControllerInfo(Side side)
        {
            //fill all device info
            
            List<XRNode> Devices = side == Side.right ? new List<XRNode>() { XRNode.RightHand, XRNode.Head } : new List<XRNode>() { XRNode.LeftHand, XRNode.Head };
            List<DeviceInfo> DeviceInfos = new List<DeviceInfo>();
            for (int i = 0; i < Devices.Count; i++)
            {
                XRNode Device = DeviceOrder[i];
                DeviceInfo deviceInfo = new DeviceInfo();

                InputDevices.GetDeviceAtXRNode(Device).TryGetFeatureValue(CommonUsages.devicePosition, out deviceInfo.Pos);
                InputDevices.GetDeviceAtXRNode(Device).TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion quat);
                deviceInfo.Rot = quat.eulerAngles;

                InputDevices.GetDeviceAtXRNode(Device).TryGetFeatureValue(CommonUsages.deviceVelocity, out deviceInfo.velocity);
                InputDevices.GetDeviceAtXRNode(Device).TryGetFeatureValue(CommonUsages.deviceAngularVelocity, out deviceInfo.angularVelocity);

                Controllers[(int)side].inputDevice.TryGetFeatureValue(CommonUsages.deviceAcceleration, out deviceInfo.acceleration);

                if (PreviousValues[(int)side][i].LastTime != 0)
                {
                    float deltaTime = Time.time - PreviousValues[(int)side][i].LastTime;
                    //deviceInfo.acceleration = (deviceInfo.velocity - PreviousValues[(int)side][i].LastVel) / deltaTime;
                    deviceInfo.angularAcceleration = (deviceInfo.angularVelocity - PreviousValues[i][0].LastVelRot) / deltaTime;
                }
                PreviousValues[(int)side][i] = new XRPreviousValues(deviceInfo.velocity, deviceInfo.angularVelocity, Time.time);

                DeviceInfos.Add(deviceInfo);
            }

            Vector3 DistanceFromOrigin = DeviceInfos[1].Pos;
            for (int i = 0; i < DeviceInfos.Count; i++)
                DeviceInfos[i].Pos = DeviceInfos[i].Pos - DistanceFromOrigin;

            return new AthenaFrame(DeviceInfos);
        }
        private void Update()
        {
            FrameInfo.Add(GetControllerInfo(Side.right));
            if (FrameInfo.Count > MaxStoreInfo)
                FrameInfo.RemoveAt(0);

            if (IsReady)
                Runtime.instance.RunModel();
        }
        public static bool IsReady { get { return instance.FrameInfo.Count >= instance.MaxStoreInfo - 1; } }
        private void Start()
        {
            HandsActive = new bool[2];
            InputTracking.trackingLost += TrackingLost;
            InputTracking.trackingAcquired += TrackingFound;
        }
        public void TrackingLost(XRNodeState state)
        {
            if (XRHands.ContainsKey(state.nodeType))
            {
                Side side = XRHands[state.nodeType];
                disableController?.Invoke(side);
                HandsActive[(int)side] = false;
            }
        }
        public void TrackingFound(XRNodeState state)
        {
            if (XRHands.ContainsKey(state.nodeType))
            {
                Side side = XRHands[state.nodeType];
                HandsActive[(int)side] = true;
            }
        }

        //public AthenaFrame PastFrame() { return (side == Side.right) ? RightInfo[RightInfo.Count - FramesAgo()] : LeftInfo[LeftInfo.Count - FramesAgo()]; }
        //public int FramesAgo() { return RestrictionManager.instance.RestrictionSettings.FramesAgo; }
    }
}

