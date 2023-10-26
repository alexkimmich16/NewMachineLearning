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

        public const int MaxStoreInfo = 20;

        public List<Transform> PlayerHands;

        public bool DrawDebug;

        public bool[] HandsActive;

        public delegate void ControllerSide(Side side);
        public static ControllerSide disableController;
        public static ControllerSide NewFrame;

        public const int sampleSize = 5;  // for example, considering the last 5 samples

        public static Dictionary<XRNode, Side> XRHands = new Dictionary<XRNode, Side>() { { XRNode.RightHand, Side.right }, { XRNode.LeftHand, Side.left } };

        public List<AthenaFrame> GetFramesList(Side side, int Frames) { return Enumerable.Range(FrameInfo[(int)side].Count - Frames, Frames).Select(x => FrameInfo[(int)side][x]).ToList(); }


        private AthenaFrame GetControllerInfo(Side side)
        {
            List<XRNode> Devices = new List<XRNode>() { side == Side.right ? XRNode.RightHand : XRNode.LeftHand, XRNode.Head };
            List<DeviceInfo> DeviceInfos = new List<DeviceInfo>();

            // Get headset's rotation and position first
            InputDevices.GetDeviceAtXRNode(XRNode.Head).TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 headPosition);
            InputDevices.GetDeviceAtXRNode(XRNode.Head).TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion headRotation);

            // Create an inverse rotation based only on the y-axis to rotate everything to look forward
            Quaternion inverseYRotation = Quaternion.Euler(0, -headRotation.eulerAngles.y, 0);

            for (int i = 0; i < Devices.Count; i++)
            {
                XRNode Device = Devices[i];
                DeviceInfo deviceInfo = new DeviceInfo();

                InputDevices.GetDeviceAtXRNode(Device).TryGetFeatureValue(CommonUsages.devicePosition, out deviceInfo.Pos);
                InputDevices.GetDeviceAtXRNode(Device).TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion quat);
                InputDevices.GetDeviceAtXRNode(Device).TryGetFeatureValue(CommonUsages.deviceVelocity, out deviceInfo.velocity);
                InputDevices.GetDeviceAtXRNode(Device).TryGetFeatureValue(CommonUsages.deviceAngularVelocity, out deviceInfo.angularVelocity);

                // Apply transformations
                deviceInfo.Pos = inverseYRotation * (deviceInfo.Pos - headPosition);
                deviceInfo.velocity = inverseYRotation * deviceInfo.velocity;

                // Adjust rotation for relative to the headset
                quat = inverseYRotation * quat;


                //Acceleraion
                if(FrameInfo[(int)side].Count > sampleSize)
                {
                    AthenaFrame PastFrame = FrameInfo[(int)side][^1];
                    float TimeBetween = PastFrame.frameTime;

                    Vector3 newSample = (deviceInfo.velocity - PastFrame.Devices[i].velocity) / TimeBetween;
                    deviceInfo.AccelerationHold = newSample;
                    List<Vector3> Accelerations = Enumerable.Range(1, sampleSize - 1).Select(x => FrameInfo[(int)side][^x].Devices[i].AccelerationHold).ToList();
                    Accelerations.Add(newSample);

                    Vector3 sum = Vector3.zero;
                    foreach (Vector3 sample in Accelerations)
                        sum += sample;
                    //Debug.Log("Acc2: " + sum.ToString("f5"));
                    Vector3 AverageMovingFilter = sum / Accelerations.Count;
                    deviceInfo.acceleration = AverageMovingFilter;

                    //if(side == Side.right && i == 0)
                        //Debug.Log("Diff: " + (deviceInfo.velocity - PastFrame.Devices[i].velocity).magnitude.ToString("f5") + "  Time: " + TimeBetween + "  Acc: " + deviceInfo.acceleration.magnitude.ToString("f5") + "    Sum: " + sum.magnitude.ToString("f5"));
                }


                if (side == Side.left && i == 0)
                {
                    quat.w = -quat.w;
                    quat.x = -quat.x;
                }

                deviceInfo.Rot = new Vector3(quat.eulerAngles.x / 360f, quat.eulerAngles.y / 360f, quat.eulerAngles.z / 360f);
                //if (Device == XRNode.Head)
                   // deviceInfo.Rot.y = 0;


                // No need to apply the inverseYRotation here since we've adjusted the quaternion already
                // deviceInfo.Rot = inverseYRotation * deviceInfo.Rot;

                DeviceInfos.Add(deviceInfo);
            }
            /*
            Vector3 DistanceFromOrigin = DeviceInfos[1].Pos;
            for (int i = 0; i < DeviceInfos.Count; i++)
                DeviceInfos[i].Pos = DeviceInfos[i].Pos - DistanceFromOrigin;
            */
            return new AthenaFrame(DeviceInfos);
        }
        
        private void Update()
        {
            //both sides
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
        public static bool IsReady { get { return instance.FrameInfo[0].Count >= MaxStoreInfo - 1; } }
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

    [System.Serializable]
    public class AthenaFrame
    {
        public List<DeviceInfo> Devices = new List<DeviceInfo>(2);
        public float frameTime;
        public List<float> AsInputs()
        {
            ///Restrictions.GetValue

            //List<float> Inputs = Devices.SelectMany(x => x.AsFloats()).ToList();
            List<float> Inputs = new List<float>();
            Inputs.AddRange(Devices[0].AsFloats(XRNode.RightHand));
            Inputs.AddRange(Devices[1].AsFloats(XRNode.Head));
            //Inputs.Add(frameTime);
            return Inputs;
        }
        public AthenaFrame(List<DeviceInfo> Devices)
        {
            this.Devices = Devices;
            frameTime = Time.deltaTime;
        }
    }

    [System.Serializable]
    public class DeviceInfo
    {
        public Vector3 Pos;
        public Vector3 Rot;

        public Vector3 velocity;
        public Vector3 angularVelocity;

        public Vector3 acceleration;


        [HideInInspector] public Vector3 AccelerationHold;

        public Vector3 GetValue(AthenaValue value)
        {
            if(value == AthenaValue.Pos)return Pos;
            if(value == AthenaValue.Rot) return Rot;
            if(value == AthenaValue.Vel)return velocity;
            if(value == AthenaValue.AngVel)return angularVelocity;
            if(value == AthenaValue.Acc)return acceleration;

            Debug.LogError("Couldn't Find: " + value.ToString());
            return Vector3.zero;
        }

        public List<float> AsFloats(XRNode node)
        {
            //Vector3
            return new List<Vector3>() { Pos, Rot, velocity, angularVelocity, acceleration }.SelectMany(vec => new[] { vec.x, vec.y, vec.z }).ToList();
            /*
            if (node == XRNode.Head)
            {
                return new List<Vector3>() { velocity, angularVelocity }.SelectMany(vec => new[] { vec.x, vec.y, vec.z }).ToList();
            }
            else
            {
                return new List<Vector3>() { Pos, Rot, velocity, angularVelocity, acceleration }.SelectMany(vec => new[] { vec.x, vec.y, vec.z }).ToList();
            }
            */
        }
    }

    public enum AthenaValue
    {
        Pos = 0,
        Rot = 1,
        Vel = 2,
        AngVel = 3,
        Acc = 4,
    }
}

