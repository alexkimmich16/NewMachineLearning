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
        OneMotion = 3,
        All = 4,
    }
    public class DebugRestrictions : SerializedMonoBehaviour
    {
        public static DebugRestrictions instance;
        private void Awake() { instance = this; }

        public bool Active;
        [FoldoutGroup("References")] public List<SkinnedMeshRenderer> handToChange;

        [FoldoutGroup("Values"), ReadOnly] public float Velocity;

        [FoldoutGroup("References")] public List<Material> Materials;
        //[FoldoutGroup("Values"), ReadOnly] public Vector3 VelocityDirection;
        //[FoldoutGroup("Values"), ReadOnly] public float AngleDistance;
        //[FoldoutGroup("Values"), ReadOnly] public Vector3 HandPosition;
        //[FoldoutGroup("Values"), ReadOnly] public float HandToHeadDir;

        [FoldoutGroup("Testing")] public MotionRestriction Restrictions;
        //[FoldoutGroup("Testing")] public bool DebugHand;
        [FoldoutGroup("Testing")] public bool TestAll;

        [FoldoutGroup("Guesses")]public float MaxFalseGuesses;
        [FoldoutGroup("Guesses")] public float TotalCorrectGuesses;
        [FoldoutGroup("Guesses")] public float TotalGuesses;
        [FoldoutGroup("Guesses"), Range(0, 1)]public float Threshold;


        [FoldoutGroup("Input")] public float InputCounts;
        [FoldoutGroup("Input")] public float Index;
        [FoldoutGroup("Input")] public float Output;
        [FoldoutGroup("Input")] public float Output2;

        [FoldoutGroup("Input"), Range(0,1)] public float MakeCloserToMiddleMulitplier;



        [FoldoutGroup("Input"), Range(0.5f,1f)] public float Threshold1;
        [FoldoutGroup("Input"), Range(0.5f, 1f)] public float Threshold2;
        [FoldoutGroup("Input"), Range(0.5f, 1f)] public float Threshold3;
        [FoldoutGroup("Input"), Range(0.5f, 1f)] public float Threshold4;
        [FoldoutGroup("Input"), Range(0.5f, 1f)] public float Threshold5;

        //public bool ABS;

        public bool DebugVelocity;
        public float LineLength;
        private void Start()
        {
            //ConditionManager.instance.MotionConditions[0].OnNewState += MotionDone;
            ConditionManager.instance.conditions.MotionConditions[1].OnNewState += MotionDone;
            //ConditionManager.instance.MotionConditions[2].OnNewState += MotionDone;
        }
        public void MotionDone(Side side, bool NewState, int Index, int Level)
        {
            //Debug.Log("NewState: " + NewState + "  Index: " + Index);
        }

        void Update()
        {
            if (!Active)
                return;

            PastFrameRecorder PR = PastFrameRecorder.instance;
            if (!PastFrameRecorder.IsReady())
                return;
            
            SingleInfo frame1 = PR.PastFrame(Side.right);
            SingleInfo frame2 = PastFrameRecorder.instance.GetControllerInfo(Side.right);
            
            if (DebugVelocity)
                Debug.DrawLine(frame2.HandPos, frame2.HandPos + ((frame2.HandPos - frame1.HandPos).normalized * LineLength), Color.blue);

            Velocity = Vector3.Distance(frame1.HandPos, frame2.HandPos) / (1f / 60f);
            //VelocityDirection = (frame2.HandPos - frame1.HandPos).normalized;
            //AngleDistance = Vector3.Angle((frame2.HandPos - frame1.HandPos).normalized, frame2.HandRot.normalized);
            //HandPosition = frame2.HandPos;

            
            if(!MotionEditor.instance.TestAllMotions.isOn)
            {
                handToChange[0].material = Materials[RestrictionManager.instance.MotionWorks(PR.PastFrame(Side.right), PR.GetControllerInfo(Side.right), MotionEditor.instance.CurrentTestMotion) ? 1 : 0]; //set hand
                handToChange[1].material = Materials[RestrictionManager.instance.MotionWorks(PR.PastFrame(Side.left), PR.GetControllerInfo(Side.left), MotionEditor.instance.CurrentTestMotion) ? 1 : 0]; //set hand
            }
            else
            {
                handToChange[0].material = Materials[GetMatTestNum(Side.right)]; //set hand
                handToChange[1].material = Materials[GetMatTestNum(Side.left)]; //set hand
            }
            

            int GetMatTestNum(Side side)
            {
                List<int> Working = new List<int>();
                for (int j = 1; j < RestrictionManager.instance.RestrictionSettings.Coefficents.Count + 1; j++)
                {
                    bool Works = RestrictionManager.instance.MotionWorks(PR.PastFrame(side), PastFrameRecorder.instance.GetControllerInfo(side), (CurrentLearn)j);
                    if (Works)
                        Working.Add(j);
                }
                if (Working.Count == 0)
                    return 0;
                else if (Working.Count == 1)
                    return Working[0];
                else
                    return RestrictionManager.instance.RestrictionSettings.Coefficents.Count + 1;

            }
            /*
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
                //handToChange.material = Materials[RestrictionManager.MotionWorks(frame1, frame2, RestrictionManager.instance.RestrictionSettings.MotionRestrictions[(int)MotionTry - 1]) ? 1 : 0]; //set hand
                
                
                RegressionSystem.instance.ControllerGuess(out float Guess);
                
                if(Guess < 0.5f)
                    handToChange.material = Materials[0];
                if(Guess > Threshold1)
                    handToChange.material = Materials[1];
                if (Guess > Threshold2)
                    handToChange.material = Materials[2];
                if (Guess > Threshold3)
                    handToChange.material = Materials[3];
                if (Guess > Threshold4)
                    handToChange.material = Materials[4];
                if (Guess > Threshold5)
                    handToChange.material = Materials[5];
                
            //Debug.Log("Motion: " + Motion.ToString());
        }
            */
        }
        /*
        public float GetVelocity(SingleInfo frame1, SingleInfo frame2) { return Vector3.Distance(frame1.HandPos, frame2.HandPos) / (1f / 60f); }
        public Vector3 GetVelocityDirection(SingleInfo frame1, SingleInfo frame2) { return (frame2.HandPos - frame1.HandPos).normalized; }
        public float GetAngleDistance(SingleInfo frame1, SingleInfo frame2) { return Vector3.Angle((frame2.HandPos - frame1.HandPos).normalized, frame2.HandRot.normalized); }
        public Vector3 GetPosition(SingleInfo frame1, SingleInfo frame2) { return frame2.HandPos; }
        */
    }


    ///test lkeft
}

