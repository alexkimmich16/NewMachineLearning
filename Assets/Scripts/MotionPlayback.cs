using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
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

    [Header("Info")]
    public int Frame;
    public float NextTime;
    public float Timer;

    [Header("Trans")]
    public Transform Head;
    public Transform Left;
    public Transform Right;

    public SkinnedMeshRenderer handToChange;

    public UnitySharpNEAT.LearningAgent LA;

    public delegate void NewFrame();
    public static event NewFrame OnNewFrame;

    public bool OneInterprolate;
    public List<SingleInfo> interpolating;

    private void Start()
    {
        if(type == PlayType.WatchAI)
            LA = transform.parent.GetComponent<UnitySharpNEAT.LearningAgent>();
    }
    void Update()
    {
        LearnManager LM = LearnManager.instance;
        if (type == PlayType.WatchAI)
        {
            //Debug.Log("|Motion: " + LA.MotionIndex + " |Set: " + LA.Set + " |Frame: " + LA.Frame + "|");
            if (LA.Active() && LA.IsInterpolating() == false)
                moveAll(LA.CurrentMotion().Infos[LA.Frame]);
            else if (LA.IsInterpolating())
                moveAll(LA.InterpolateFrames[0]);
        }
        else if (type == PlayType.Repeat)
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
            {
                Frame = 0;
            }
            bool State = ME.Setting == EditSettings.Editing ? LM.MovementList[(int)ME.MotionType].Motions[ME.MotionNum].AtFrameState(Frame) : GetMotionFromInput();
            handToChange.material = LM.FalseTrue[State ? 1 : 0];
            SingleInfo info = LM.MovementList[(int)ME.MotionType].Motions[ME.MotionNum].Infos[Frame];
            
            moveAll(info);
            LastMotion = Motion;
            if(OnNewFrame != null)
                OnNewFrame();

            bool GetMotionFromInput() { return Frame - BruteForce.instance.PastFrameLookup >= 0 ? RestrictionSystem.RestrictionManager.instance.MotionWorks(LM.MovementList[(int)ME.MotionType].GetRestrictionInfoAtIndex(ME.MotionNum, Frame - BruteForce.instance.PastFrameLookup), LM.MovementList[(int)ME.MotionType].GetRestrictionInfoAtIndex(ME.MotionNum, Frame), BruteForce.instance.BruteForceSettings) : false; }
        }
        void moveAll(SingleInfo info)
        {
            MoveController(Right, info);
            MoveHead(info);
        }
    }
    public void MoveController(Transform trans, SingleInfo info)
    {
        trans.localPosition = info.HandPos;
        trans.localEulerAngles = info.HandRot;
    }
    public void MoveHead(SingleInfo info)
    {
        Head.localPosition = info.HeadPos;
        Head.localEulerAngles = info.HeadRot;
    }
    
}
