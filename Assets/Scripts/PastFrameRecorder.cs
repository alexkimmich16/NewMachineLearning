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
        [ListDrawerSettings(ShowIndexLabels = true)] public List<List<AthenaFrame>> FrameInfo;

        public int MaxStoreInfo = 10;

        public List<Transform> TestMain;
        public List<Transform> TestCam;
        public List<Transform> TestHand;

        public List<Transform> PlayerHands;
        public Transform Cam;

        public bool DrawDebug;

        public bool[] HandsActive;

        public delegate void ControllerSide(Side side);
        public static ControllerSide disableController;
        public static ControllerSide NewFrame;

        //public int SmoothingFrames = 3;

        public List<UnityEngine.XR.Interaction.Toolkit.XRController> Controllers;




        public static List<XRNode> DeviceOrder { get { return new List<XRNode>() { XRNode.RightHand, XRNode.LeftHand, XRNode.Head }; } }

        public static Dictionary<XRNode, Side> XRHands = new Dictionary<XRNode, Side>(){{XRNode.RightHand, Side.right}, { XRNode.LeftHand, Side.left } };

        //public bool[] InvertHand;
        //118.012 to 
        public bool HandActive(Side side) { return HandsActive[(int)side]; }
        
        public List<AthenaFrame> GetFramesList(Side side, int Frames) { return Enumerable.Range(FrameInfo[(int)side].Count - Frames, Frames).Reverse().Select(x => FrameInfo[(int)side][x]).ToList(); }
    
        
        private AthenaFrame GetControllerInfo(Side side)
        {
            //fill all device info
            //vel raw, acc raw, 
            List<XRNode> Devices = side == Side.right ? new List<XRNode>() { XRNode.RightHand, XRNode.Head } : new List<XRNode>() { XRNode.LeftHand, XRNode.Head };
            List<DeviceInfo> DeviceInfos = new List<DeviceInfo>();
            for (int i = 0; i < Devices.Count; i++)
            {
                XRNode Device = DeviceOrder[i];
                DeviceInfo deviceInfo = new DeviceInfo();

                InputDevices.GetDeviceAtXRNode(Device).TryGetFeatureValue(CommonUsages.devicePosition, out deviceInfo.Pos);
                InputDevices.GetDeviceAtXRNode(Device).TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion quat);
                

                InputDevices.GetDeviceAtXRNode(Device).TryGetFeatureValue(CommonUsages.deviceVelocity, out deviceInfo.velocity);
                InputDevices.GetDeviceAtXRNode(Device).TryGetFeatureValue(CommonUsages.deviceAngularVelocity, out deviceInfo.angularVelocity);

                //choose hand or head
                //InputDevice accDevice = i == 0 ? Controllers[(int)side].inputDevice : InputDevices.GetDeviceAtXRNode(Device);
                //accDevice.TryGetFeatureValue(CommonUsages.deviceAcceleration, out deviceInfo.acceleration);
                /*
                if (FrameInfo[(int)side].Count > SmoothingFrames)
                {
                    AthenaFrame PastFrame = FrameInfo[(int)side][^SmoothingFrames];


                    float TimeBetween = Time.time - PastFrame.frameTime;
                    deviceInfo.acceleration = (deviceInfo.velocity - PastFrame.Devices[i].velocity) / TimeBetween;
                    deviceInfo.angularAcceleration = (deviceInfo.angularVelocity - PastFrame.Devices[i].angularVelocity) / TimeBetween;
                }
                */

                if(side == Side.left)
                {
                    deviceInfo.Pos.x = -deviceInfo.Pos.x;
                    quat.w = -quat.w;
                    quat.x = -quat.x;
                    deviceInfo.velocity.x = -deviceInfo.velocity.x;
                    //deviceInfo.acceleration.x = -deviceInfo.acceleration.x;

                    //OR -w, -x, y, z 
                    deviceInfo.angularVelocity.x = -deviceInfo.angularVelocity.x;
                    //deviceInfo.angularAcceleration.x = -deviceInfo.angularAcceleration.x;

                    

                }

                deviceInfo.Rot = quat.eulerAngles;

                //PreviousValues[(int)side][i] = new XRPreviousValues(deviceInfo.velocity, deviceInfo.angularVelocity, Time.time);

                DeviceInfos.Add(deviceInfo);
            }

            Vector3 DistanceFromOrigin = DeviceInfos[1].Pos;
            for (int i = 0; i < DeviceInfos.Count; i++)
                DeviceInfos[i].Pos = DeviceInfos[i].Pos - DistanceFromOrigin;

            return new AthenaFrame(DeviceInfos);
        }
        private void Update()
        {
            for (int i = 0; i < FrameInfo.Count; i++)
            {
                FrameInfo[i].Add(GetControllerInfo((Side)i));
                if (FrameInfo[i].Count > MaxStoreInfo)
                    FrameInfo[i].RemoveAt(0);
                NewFrame?.Invoke((Side)i);
            }
            if (IsReady)
                Runtime.instance.RunModel();
        }
        public static bool IsReady { get { return instance.FrameInfo[0].Count >= instance.MaxStoreInfo - 1; } }
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

