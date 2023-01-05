using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
namespace RestrictionSystem
{
    public class DebugRestrictions : SerializedMonoBehaviour
    {
        public static DebugRestrictions instance;
        private void Awake() { instance = this; }
        [FoldoutGroup("References")] public SkinnedMeshRenderer handToChange;

        [FoldoutGroup("Values"), ReadOnly] public float Velocity;
        //[FoldoutGroup("Values"), ReadOnly] public Vector3 VelocityDirection;
        //[FoldoutGroup("Values"), ReadOnly] public float AngleDistance;
        //[FoldoutGroup("Values"), ReadOnly] public Vector3 HandPosition;
        //[FoldoutGroup("Values"), ReadOnly] public float HandToHeadDir;

        [FoldoutGroup("Testing")] public MotionRestriction Restrictions;
        [FoldoutGroup("Testing")] public bool DebugHand;
        [FoldoutGroup("Testing")] public int FramesAgo = 2;

        public bool Lock;

        //public bool ABS;

        public int Add;

        public bool DebugVelocity;
        public float LineLength;

        void Update()
        {
            if (LearnManager.instance.RightInfo.Count < LearnManager.instance.MaxStoreInfo - 1)
                return;
            
            SingleInfo frame1 = LearnManager.instance.PastFrame(EditSide.right, FramesAgo);
            SingleInfo frame2 = LearnManager.instance.Info.GetControllerInfo(EditSide.right);
            //
            if (DebugVelocity)
                Debug.DrawLine(frame2.HandPos, frame2.HandPos + ((frame2.HandPos - frame1.HandPos).normalized * LineLength), Color.blue);

            Velocity = Vector3.Distance(frame1.HandPos, frame2.HandPos) / (1f / 60f);
            //VelocityDirection = (frame2.HandPos - frame1.HandPos).normalized;
            //AngleDistance = Vector3.Angle((frame2.HandPos - frame1.HandPos).normalized, frame2.HandRot.normalized);
            //HandPosition = frame2.HandPos;



            if (DebugHand)
                handToChange.material = LearnManager.instance.FalseTrue[RestrictionManager.instance.MotionWorks(frame1, frame2, Restrictions) ? 1 : 0]; //set hand

        }
        /*
        public float GetVelocity(SingleInfo frame1, SingleInfo frame2) { return Vector3.Distance(frame1.HandPos, frame2.HandPos) / (1f / 60f); }
        public Vector3 GetVelocityDirection(SingleInfo frame1, SingleInfo frame2) { return (frame2.HandPos - frame1.HandPos).normalized; }
        public float GetAngleDistance(SingleInfo frame1, SingleInfo frame2) { return Vector3.Angle((frame2.HandPos - frame1.HandPos).normalized, frame2.HandRot.normalized); }
        public Vector3 GetPosition(SingleInfo frame1, SingleInfo frame2) { return frame2.HandPos; }
        */
    }
}

