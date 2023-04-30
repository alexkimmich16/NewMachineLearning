using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.XR;
namespace RestrictionSystem
{
    public class PastFrameRecorder : SerializedMonoBehaviour
    {
        public static PastFrameRecorder instance;
        private void Awake() { instance = this; }
        [ListDrawerSettings(ShowIndexLabels = true, Expanded = true)] public List<SingleInfo> RightInfo;
        [ListDrawerSettings(ShowIndexLabels = true, Expanded = true)] public List<SingleInfo> LeftInfo;

        public int MaxStoreInfo = 10;
        public int FramesAgo = 10;

        public List<Transform> TestMain;
        public List<Transform> TestCam;
        public List<Transform> TestHand;

        public List<Transform> PlayerHands;
        public Transform Cam;

        public List<bool> UseSides;

        public bool OverrideSides;

        public bool DrawDebug;

        //public bool[] InvertHand;
        //118.012 to 
        public SingleInfo GetControllerInfo(Side side)
        {
            ResetStats();
            Vector3 CamPos = Cam.localPosition;
            TestCam[(int)side].position = Vector3.zero;
            TestHand[(int)side].position = TestHand[(int)side].position - CamPos;

            float YDifference = -Cam.localRotation.eulerAngles.y;
            if (side == Side.left)
            {
                // Calculate the offset between the HMD and the left controller
                Vector3 offset = TestHand[(int)side].position - TestCam[(int)side].position;

                // Mirror the offset along the X axis
                offset = new Vector3(-offset.x, offset.y, offset.z);

                // Apply the mirrored offset to the left controller's position
                TestHand[(int)side].position = TestCam[(int)side].position + offset;

                // Mirror the left controller's rotation along the Y and Z axes
                Quaternion mirroredRotation = new Quaternion(TestHand[(int)side].rotation.x, -TestHand[(int)side].rotation.y, -TestHand[(int)side].rotation.z, TestHand[(int)side].rotation.w);

                // Apply the mirrored rotation to the left controller
                TestHand[(int)side].rotation = mirroredRotation;
                YDifference = -YDifference; // * -1f
            }
            
            //Debug.Log("side: " + side + "  YDifference: " + YDifference);
            Vector3 UncenteredHandPos = TestHand[(int)side].position;
            Vector3 UncenteredHandRot = TestHand[(int)side].rotation.eulerAngles;

            TestMain[(int)side].rotation = Quaternion.Euler(0f, YDifference, 0f);

            if (side == Side.left)
            {
                TestCam[(int)side].eulerAngles = new Vector3(TestCam[(int)side].eulerAngles.x, 0f, TestCam[(int)side].eulerAngles.z);
            }

            if (DrawDebug)
            {
                Debug.DrawRay(TestHand[(int)side].position, Quaternion.Euler(TestHand[(int)side].rotation.eulerAngles) * Vector3.forward, Color.blue);
                Debug.DrawRay(TestHand[(int)side].position, Quaternion.Euler(TestHand[(int)side].rotation.eulerAngles) * Vector3.right, Color.green);
                Debug.DrawRay(TestHand[(int)side].position, Quaternion.Euler(TestHand[(int)side].rotation.eulerAngles) * Vector3.up, Color.red);
            }
            
            //TestCam[(int)side].localRotation = Cam.localRotation;
            return new SingleInfo(TestHand[(int)side].position, TestHand[(int)side].rotation.eulerAngles, TestCam[(int)side].position, TestCam[(int)side].rotation.eulerAngles, UncenteredHandPos, UncenteredHandRot);

            void ResetStats()
            {
                TestMain[(int)side].position = Vector3.zero;
                TestMain[(int)side].rotation = Quaternion.identity;
                TestMain[(int)side].localScale = new Vector3(1f, 1f, 1f);

                SetEqual(Cam, TestCam[(int)side]);
                SetEqual(PlayerHands[(int)side].transform, TestHand[(int)side]);
                void SetEqual(Transform Info, Transform Set)
                {
                    Set.localPosition = Info.localPosition;
                    Set.localRotation = Info.localRotation;
                }
            }
        }
        private void Update()
        {
            ManageLists();
        }
        public static bool IsReady() { return instance.RightInfo.Count >= instance.MaxStoreInfo - 1; }
        public void ManageLists()
        {
            InputDevices.GetDeviceAtXRNode(XRNode.Head).TryGetFeatureValue(CommonUsages.isTracked, out bool HeadsetActive);
            InputDevices.GetDeviceAtXRNode(XRNode.RightHand).TryGetFeatureValue(CommonUsages.isTracked, out bool RightHandActive);
            InputDevices.GetDeviceAtXRNode(XRNode.LeftHand).TryGetFeatureValue(CommonUsages.isTracked, out bool LeftHandActive);
            if(!OverrideSides)
                UseSides = new List<bool>() { RightHandActive && HeadsetActive, LeftHandActive && HeadsetActive };

            RightInfo.Add(GetControllerInfo(Side.right));
            if (RightInfo.Count > MaxStoreInfo)
                RightInfo.RemoveAt(0);

            LeftInfo.Add(GetControllerInfo(Side.left));
            if (LeftInfo.Count > MaxStoreInfo)
                LeftInfo.RemoveAt(0);

            if (RightInfo.Count > FramesAgo)
                RestrictionManager.instance.TriggerFrameEvents();
        }
        public SingleInfo PastFrame(Side side) { return (side == Side.right) ? RightInfo[RightInfo.Count - FramesAgo] : LeftInfo[LeftInfo.Count - FramesAgo]; }
    }
    [System.Serializable]
    public class SingleInfo
    {
        public Vector3 HandPosType(bool IsLocal) { return IsLocal ? HandPos : UncenteredHandPos; }
        public Vector3 HandRotType(bool IsLocal) { return IsLocal ? HandRot: UncenteredHandRot; }
        
        public Vector3 HeadPos, HeadRot, HandPos, HandRot, UncenteredHandPos, UncenteredHandRot;
        public float SpawnTime;
        public SingleInfo(Vector3 HandPosStat, Vector3 HandRotStat, Vector3 HeadPosStat, Vector3 HeadRotStat, Vector3 UncenteredHandPos, Vector3 UncenteredHandRot)
        {
            SpawnTime = Time.timeSinceLevelLoad;
            HeadPos = HeadPosStat;
            HeadRot = HeadRotStat;
            HandPos = HandPosStat;
            HandRot = HandRotStat;
            this.UncenteredHandRot = UncenteredHandRot;
            this.UncenteredHandPos = UncenteredHandPos;
        }
        public SingleInfo(Vector3 HandPosStat, Vector3 HandRotStat, Vector3 HeadPosStat, Vector3 HeadRotStat, Vector3 UncenteredHandPos, Vector3 UncenteredHandRot, float SetTime)
        {
            SpawnTime = SetTime;
            HeadPos = HeadPosStat;
            HeadRot = HeadRotStat;
            HandPos = HandPosStat;
            HandRot = HandRotStat;
            this.UncenteredHandRot = UncenteredHandRot;
            this.UncenteredHandPos = UncenteredHandPos;
        }
    }
}

