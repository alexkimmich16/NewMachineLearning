using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
namespace RestrictionSystem
{
    public class PastFrameRecorder : SerializedMonoBehaviour
    {
        public static PastFrameRecorder instance;
        private void Awake() { instance = this; }
        [FoldoutGroup("NeatDisplay"), ReadOnly] public List<SingleInfo> RightInfo;
        [FoldoutGroup("NeatDisplay"), ReadOnly] public List<SingleInfo> LeftInfo;

        public int MaxStoreInfo = 10;
        public int FramesAgo = 10;

        public List<Transform> TestMain;
        public List<Transform> TestCam;
        public List<Transform> TestHand;

        public List<Transform> PlayerHands;
        public Transform Cam;

        public List<bool> UseSides;
        public SingleInfo GetControllerInfo(Side side)
        {
            ResetStats();
            Vector3 CamPos = Cam.localPosition;
            TestCam[(int)side].position = Vector3.zero;
            TestHand[(int)side].position = TestHand[(int)side].position - CamPos;

            float YDifference = -Cam.localRotation.eulerAngles.y;
            //float CenterChange = Cam.localRotation.eulerAngles.y;

            
            //invert main to y distance
            if (side == Side.left)
            {
                TestMain[(int)side].localScale = new Vector3(-1, 1, 1);
                Vector3 Rot = TestCam[(int)side].eulerAngles;
                TestCam[(int)side].eulerAngles = new Vector3(Rot.x, -Rot.y, -Rot.z);
            }
            Vector3 UncenteredHandPos = TestHand[(int)side].position;
            Vector3 UncenteredHandRot = TestHand[(int)side].rotation.eulerAngles;
            TestMain[(int)side].rotation = Quaternion.Euler(0, YDifference, 0);
            //TestCam[(int)side].localRotation = Cam.localRotation;
            return new SingleInfo(TestHand[(int)side].position, TestHand[(int)side].rotation.eulerAngles, TestCam[(int)side].position, TestCam[(int)side].rotation.eulerAngles, UncenteredHandPos, UncenteredHandRot);

            void ResetStats()
            {
                TestMain[(int)side].position = Vector3.zero;
                TestMain[(int)side].rotation = Quaternion.identity;
                TestMain[(int)side].localScale = new Vector3(1, 1, 1);
                SetEqual(Cam, TestCam[(int)side]);
                SetEqual(PlayerHands[(int)side].transform, TestHand[(int)side]);
                void SetEqual(Transform Info, Transform Set)
                {
                    Set.localPosition = Info.localPosition;
                    Set.localRotation = Info.localRotation;
                }
            }
        }

        void Start()
        {
            StartCoroutine(ManageLists(1f / 60f));
        }
        IEnumerator ManageLists(float Interval)
        {
            while (true)
            {
                RightInfo.Add(GetControllerInfo(Side.right));
                if (RightInfo.Count > MaxStoreInfo)
                    RightInfo.RemoveAt(0);

                LeftInfo.Add(GetControllerInfo(Side.left));
                if (LeftInfo.Count > MaxStoreInfo)
                    LeftInfo.RemoveAt(0);
                
                if(RightInfo.Count > FramesAgo)
                    RestrictionManager.instance.TriggerFrameEvents(UseSides);

                yield return new WaitForSeconds(Interval);
            }
        }
        public SingleInfo PastFrame(Side side) { return (side == Side.right) ? RightInfo[RightInfo.Count - FramesAgo] : LeftInfo[LeftInfo.Count - FramesAgo]; }
    }
    [System.Serializable]
    public class SingleInfo
    {
        public Vector3 HandPosType(bool IsLocal) { return IsLocal ? UncenteredHandPos : HandPos; }
        public Vector3 HandRotType(bool IsLocal) { return IsLocal ? UncenteredHandRot : HandRot; }
        
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

