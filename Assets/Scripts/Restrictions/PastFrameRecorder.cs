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

            //invert main to y distance
            if (side == Side.left)
            {
                TestMain[(int)side].localScale = new Vector3(-1, 1, 1);
                Vector3 Rot = TestCam[(int)side].eulerAngles;
                TestCam[(int)side].eulerAngles = new Vector3(Rot.x, -Rot.y, -Rot.z);
            }

            TestMain[(int)side].rotation = Quaternion.Euler(0, YDifference, 0);
            //TestCam[(int)side].localRotation = Cam.localRotation;
            return new SingleInfo(TestHand[(int)side].position, TestHand[(int)side].rotation.eulerAngles, TestCam[(int)side].position, TestCam[(int)side].rotation.eulerAngles);

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
            StartCoroutine(ManageLists(1 / 60));
        }

        // Update is called once per frame
        void Update()
        {

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
        public Vector3 HeadPos, HeadRot, HandPos, HandRot;
        public float SpawnTime;
        public SingleInfo(Vector3 HandPosStat, Vector3 HandRotStat, Vector3 HeadPosStat, Vector3 HeadRotStat)
        {
            SpawnTime = Time.timeSinceLevelLoad;
            HeadPos = HeadPosStat;
            HeadRot = HeadRotStat;
            HandPos = HandPosStat;
            HandRot = HandRotStat;
        }
        public SingleInfo(Vector3 HandPosStat, Vector3 HandRotStat, Vector3 HeadPosStat, Vector3 HeadRotStat, float SetTime)
        {
            SpawnTime = SetTime;
            HeadPos = HeadPosStat;
            HeadRot = HeadRotStat;
            HandPos = HandPosStat;
            HandRot = HandRotStat;
        }
    }
}

