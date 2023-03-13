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
            //ConditionManager.instance.MotionConditions[1].OnNewState += MotionDone;
            //ConditionManager.instance.MotionConditions[2].OnNewState += MotionDone;
        }
        public void MotionDone(Side side, bool NewState, int Index)
        {
            //if(NewState == true)
                Debug.Log("NewState: " + NewState + "  Index: " + Index);
        }

        void Update()
        {
            //Output = 0.5f * (1f - Mathf.Abs(Index - (InputCounts - 1f) / InputCounts));
            if (InputCounts % 2f == 0)
            {
                int EachSideTotal = (int)(InputCounts / 2);
                float Spacing = 0.5f * 1 / (EachSideTotal + 1);
                bool UpperSide = Index > EachSideTotal - 1;
                Output = (UpperSide ? (Index + 2) : (Index + 1)) * Spacing;
                
            }
            else
            {
                int EachSideTotal = (int)(InputCounts - 1) / 2;
                float Spacing = 0.5f * 1/(EachSideTotal + 1);
                Output = (Index +1 ) * Spacing;
            }

            float OrigionalLerpValue = Output; //.25
            float LerpValueToMiddleRange = 0.5f - OrigionalLerpValue;//.25
            float AdjustedLerpValue = OrigionalLerpValue + (MakeCloserToMiddleMulitplier * LerpValueToMiddleRange);
            Output2 = AdjustedLerpValue;
            //Output = 0.5f * (1f + 2f * (Index - 0.5f) * (InputCounts - 1f));


            MaxFalseGuesses = (int)(TotalGuesses - Mathf.Ceil(TotalGuesses * Threshold));
            //MaxFalseGuesses = TotalGuesses - (TotalCorrectGuesses + Mathf.Ceil((TotalGuesses * Threshold - TotalCorrectGuesses) / (1f - Threshold)));
            //DisplayFrames = Mathf.Log((TotalGuesses * Threshold) / (1f - Threshold), 2);

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
                //handToChange.material = Materials[RestrictionManager.MotionWorks(frame1, frame2, RestrictionManager.instance.RestrictionSettings.MotionRestrictions[(int)MotionTry - 1]) ? 1 : 0]; //set hand
                handToChange.material = Materials[RegressionSystem.instance.ControllerGuess(out float Guess) ? 1 : 0]; //set hand
                /*
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
                */
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

