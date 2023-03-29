using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using RestrictionSystem;
public enum PlayType
{
    WatchAI = 0,
    StandaloneRepeat = 1,
    StandaloneSequence = 2,
    Repeat = 3,
}
public class MotionPlayback : MonoBehaviour
{
    [Header("Set")]
    public int Motion;
    private int LastMotion;
    public float PlaybackSpeed = 1f;
    public PlayType type;
    public bool LocalHandPos;
    public bool LocalHandRot;

    [Header("Info")]
    public int Frame;
    public float NextTime;
    public float Timer;

    [Header("Trans")]
    public Transform Head;
    public Transform Left;
    public Transform Right;

    public SkinnedMeshRenderer handToChange;

    public delegate void NewFrame();
    public static event NewFrame OnNewFrame;

    public bool OneInterprolate;
    public List<SingleInfo> interpolating;



    public List<RestrictionSystem.CurrentLearn> CurrentMotions;
    public bool OldFrameWorks() { return Frame - PastFrameRecorder.instance.FramesAgo >= 0; }
    public RestrictionSystem.SingleInfo GetFrameInfo(bool Old) { return LearnManager.instance.MovementList[(int)MotionEditor.instance.MotionType].GetRestrictionInfoAtIndex(MotionEditor.instance.MotionNum, Old ? MinFramesAgo() : Frame); }
    public int MinFramesAgo() { return Frame - PastFrameRecorder.instance.FramesAgo >= 0 ? Frame - PastFrameRecorder.instance.FramesAgo : 0; }

    void Update()
    {
        LearnManager LM = LearnManager.instance;
        if (type == PlayType.Repeat)
        {
            NextTime = 1 / PlaybackSpeed;
            Timer += Time.deltaTime;
            if (Timer < NextTime)
                return;
            Timer = 0;
            if (interpolating.Count > 0)
            {
                moveAll(interpolating[0]);
                interpolating.RemoveAt(0);
                return;
            }
            Frame += 1;
            if (LastMotion != Motion)
                Frame = 0;
            MotionEditor ME = MotionEditor.instance;
            
            
            if (Frame >= LM.MovementList[(int)ME.MotionType].Motions[ME.MotionNum].Infos.Count)
                Frame = 0;
            //if (Frame - BruteForce.instance.PastFrameLookup >= 0)
                //CurrentMotions = RestrictionManager.instance.AllWorkingMotions(LM.MovementList[(int)ME.MotionType].GetRestrictionInfoAtIndex(ME.MotionNum, Frame - BruteForce.instance.PastFrameLookup), LM.MovementList[(int)ME.MotionType].GetRestrictionInfoAtIndex(ME.MotionNum, Frame));
            bool State = ME.Setting == EditSettings.Editing ? LM.MovementList[(int)ME.MotionType].Motions[ME.MotionNum].AtFrameState(Frame) : GetMotionFromInput();
            handToChange.material = LM.FalseTrue[State ? 1 : 0];

            
            if (ME.Setting == EditSettings.DisplayingBrute || ME.Setting == EditSettings.DisplayingMotion)
            {
                //ConditionManager.instance.PassValue(State, ME.CurrentTestMotion, Side.right);
            }

            SingleInfo info = LM.MovementList[(int)ME.MotionType].Motions[ME.MotionNum].Infos[Frame];
            
            moveAll(info);
            LastMotion = Motion;
            OnNewFrame?.Invoke();

            
            bool GetMotionFromInput() { return Frame - BruteForce.instance.PastFrameLookup >= 0 ? RestrictionManager.instance.MotionWorks(LM.MovementList[(int)ME.MotionType].GetRestrictionInfoAtIndex(ME.MotionNum, Frame - BruteForce.instance.PastFrameLookup), LM.MovementList[(int)ME.MotionType].GetRestrictionInfoAtIndex(ME.MotionNum, Frame), (RestrictionSystem.CurrentLearn)((int)ME.CurrentTestMotion)) : false; }
            MotionRestriction GetMotionRestriction() { return ME.Setting == EditSettings.DisplayingBrute ? BruteForce.instance.BruteForceSettings : RestrictionManager.instance.RestrictionSettings.MotionRestrictions[(int)ME.CurrentTestMotion - 1]; }
        }
        void moveAll(SingleInfo info)
        {
            MoveController(Right, info);
            MoveHead(info);
        }
    }
    public void MoveController(Transform trans, SingleInfo info)
    {
        trans.localPosition = info.HandPosType(LocalHandPos);
        trans.localEulerAngles = info.HandRotType(LocalHandRot);
    }
    public void MoveHead(SingleInfo info)
    {
        Head.localPosition = info.HeadPos;
        Head.localEulerAngles = info.HeadRot;
    }
    
}
