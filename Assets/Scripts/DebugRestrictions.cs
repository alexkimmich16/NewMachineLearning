using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
namespace RestrictionSystem
{
    public enum DebugType
    {
        None = 0,
        ThisDebugTest = 1,
        MotionSettings = 2,
        OneMotionSetting = 3,
    }
    public class DebugRestrictions : SerializedMonoBehaviour
    {
        public static DebugRestrictions instance;
        private void Awake() { instance = this; }

        public bool Active;
        [FoldoutGroup("References")] public SkinnedMeshRenderer handToChange;

        [FoldoutGroup("Values"), ReadOnly] public float Velocity;

        [FoldoutGroup("References")] public List<Material> Materials;
        //[FoldoutGroup("Values"), ReadOnly] public Vector3 VelocityDirection;
        //[FoldoutGroup("Values"), ReadOnly] public float AngleDistance;
        //[FoldoutGroup("Values"), ReadOnly] public Vector3 HandPosition;
        //[FoldoutGroup("Values"), ReadOnly] public float HandToHeadDir;

        [FoldoutGroup("Testing")] public MotionRestriction Restrictions;
        //[FoldoutGroup("Testing")] public bool DebugHand;
        [FoldoutGroup("Testing")] public DebugType debugType;
        [FoldoutGroup("Testing"), ShowIf("debugType", DebugType.OneMotionSetting)] public CurrentLearn MotionTry;

        //public bool ABS;

        public bool DebugVelocity;
        public float LineLength;
        private void Start()
        {
            ConditionManager.instance.MotionConditions[0].OnNewState += MotionDone;
            ConditionManager.instance.MotionConditions[1].OnNewState += MotionDone;
            ConditionManager.instance.MotionConditions[2].OnNewState += MotionDone;
        }
        public void MotionDone(Side side, bool NewState, int Index)
        {
            //if(NewState == true)
                Debug.Log("NewState: " + NewState + "  Index: " + Index);
        }

        void Update()
        {
            if (!Active)
                return;

            PastFrameRecorder PR = PastFrameRecorder.instance;
            if (PR.RightInfo.Count < PR.MaxStoreInfo - 1)
                return;
            
            SingleInfo frame1 = PR.PastFrame(Side.right);
            SingleInfo frame2 = PastFrameRecorder.instance.GetControllerInfo(Side.right);
            
            if (DebugVelocity)
                Debug.DrawLine(frame2.HandPos, frame2.HandPos + ((frame2.HandPos - frame1.HandPos).normalized * LineLength), Color.blue);

            Velocity = Vector3.Distance(frame1.HandPos, frame2.HandPos) / (1f / 60f);
            //VelocityDirection = (frame2.HandPos - frame1.HandPos).normalized;
            //AngleDistance = Vector3.Angle((frame2.HandPos - frame1.HandPos).normalized, frame2.HandRot.normalized);
            //HandPosition = frame2.HandPos;


            if (debugType == DebugType.ThisDebugTest)
                handToChange.material = Materials[RestrictionManager.MotionWorks(frame1, frame2, Restrictions) ? 1 : 0]; //set hand
            else if(debugType == DebugType.MotionSettings)
            {
                ///CurrentLearn Motion = RestrictionManager.instance.GetCurrentMotion(frame1, frame2);
                ///handToChange.material = Materials[(int)Motion];
                //Debug.Log("Motion: " + Motion.ToString());
            }
            else if(debugType == DebugType.OneMotionSetting)
            {
                //
                handToChange.material = Materials[RestrictionManager.MotionWorks(frame1, frame2, RestrictionManager.instance.RestrictionSettings.MotionRestrictions[(int)MotionTry - 1]) ? 1 : 0]; //set hand
                //Debug.Log("Motion: " + Motion.ToString());
            }
        }
        /*
        public float GetVelocity(SingleInfo frame1, SingleInfo frame2) { return Vector3.Distance(frame1.HandPos, frame2.HandPos) / (1f / 60f); }
        public Vector3 GetVelocityDirection(SingleInfo frame1, SingleInfo frame2) { return (frame2.HandPos - frame1.HandPos).normalized; }
        public float GetAngleDistance(SingleInfo frame1, SingleInfo frame2) { return Vector3.Angle((frame2.HandPos - frame1.HandPos).normalized, frame2.HandRot.normalized); }
        public Vector3 GetPosition(SingleInfo frame1, SingleInfo frame2) { return frame2.HandPos; }
        */
    }
}

