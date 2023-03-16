using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Sirenix.OdinInspector;
using RestrictionSystem;
namespace RestrictionSystem
{
    public class MotionAssign : SerializedMonoBehaviour
    {
        [FoldoutGroup("References")] public LearnManager LM;
        [FoldoutGroup("References")] public BruteForce BF;

        public bool AbleToCallWithButtons;
        public int2 WithinFrames;
        [FoldoutGroup("LockAngle")] public float2 DegreesFromHead;
        [FoldoutGroup("LockAngle")] public KeyCode LockAngleButton;

        [FoldoutGroup("LockVelocity")] public float2 MinMaxVelocity;
        [FoldoutGroup("LockVelocity")] public KeyCode LockVelocityButton;
        [FoldoutGroup("LockVelocity")] public SingleRestriction VelocitySettings;

        //public RestrictionManager RM;

        [FoldoutGroup("LockAngle"), Button(ButtonSizes.Small)]
        public void AngleLock()
        {
            int CurrentMotionEdit = GetComponent<MotionEditor>().MotionNum;
            int CurrentSpellEdit = (int)GetComponent<MotionEditor>().MotionType;
            List<bool> Frames = new List<bool>();
            for (int i = 0; i < LM.MovementList[CurrentSpellEdit].Motions[CurrentMotionEdit].Infos.Count; i++)//frame
            {
                float Angle = RestrictionManager.RestrictionDictionary[Restriction.HandToHeadAngle].Invoke(null, null, LM.MovementList[CurrentSpellEdit].GetRestrictionInfoAtIndex(CurrentMotionEdit, i));
                bool Works = Angle > DegreesFromHead.x && Angle < DegreesFromHead.y;
                Frames.Add(Works);
            }

            //List = Frames;
            //Ranges = Motion.ConvertToRange(Frames);
            LM.MovementList[CurrentSpellEdit].Motions[CurrentMotionEdit].IntoRange(Frames);
            ///convert to ints
        }


        [FoldoutGroup("LockVelocity"), Button(ButtonSizes.Small)]
        public void VelocityLock()
        {
            int CurrentMotionEdit = GetComponent<MotionEditor>().MotionNum;
            int CurrentSpellEdit = (int)GetComponent<MotionEditor>().MotionType;
            List<bool> Frames = new List<bool>();
            for (int i = 1; i < LM.MovementList[CurrentSpellEdit].Motions[CurrentMotionEdit].Infos.Count; i++)//frame
            {
                float Velocity = RestrictionManager.RestrictionDictionary[Restriction.VelocityThreshold].Invoke(VelocitySettings, LM.MovementList[CurrentSpellEdit].GetRestrictionInfoAtIndex(CurrentMotionEdit, i - 1), LM.MovementList[CurrentSpellEdit].GetRestrictionInfoAtIndex(CurrentMotionEdit, i));
                bool Works = Velocity > MinMaxVelocity.x && Velocity < MinMaxVelocity.y && i >= WithinFrames.x && i <= WithinFrames.y;
                Frames.Add(Works);
            }

            //List = Frames;
            //Ranges = Motion.ConvertToRange(Frames);
            LM.MovementList[CurrentSpellEdit].Motions[CurrentMotionEdit].IntoRange(Frames);
            ///convert to ints
        }
        // Update is called once per frame
        void Update()
        {
            //degree assign
            if (!AbleToCallWithButtons)
                return;
            if (Input.GetKeyDown(LockAngleButton))
                AngleLock();

            if (Input.GetKeyDown(LockVelocityButton))
                VelocityLock();

            PastFrameRecorder PR = PastFrameRecorder.instance;
            if (PR.RightInfo.Count < PR.MaxStoreInfo - 1)
                return;
            //270 on right, 90 on left
            //Value = RestrictionManager.RestrictionDictionary[Restriction.HandToHeadAngle].Invoke(null, PR.PastFrame(Side.right), PastFrameRecorder.instance.GetControllerInfo(Side.right));
        }
    }
}

