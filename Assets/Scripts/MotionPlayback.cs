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
            if(LA.MotionIndex < LM.MovementList.Count && LA.Set < LM.MovementList[LA.MotionIndex].Motions.Count)
            {
                Motion motion = LA.CurrentMotion();
                //Debug.Log("|Motion: " + LA.MotionIndex + " |Set: " + LA.Set + " |Frame: " + LA.Frame + "|");
                if (motion.Infos.Count > LA.Frame)
                    moveAll(motion.Infos[LA.Frame]);
            }
        }
        else if (type == PlayType.Repeat)
        {
            NextTime = 1 / PlaybackSpeed;
            Timer += Time.deltaTime;
            if (Timer < NextTime)
                return;
            Timer = 0;
            Frame += 1;
            if (LastMotion != Motion)
                Frame = 0;
            if (Frame >= LearnManager.instance.MovementList[(int)MotionEditor.instance.MotionType].Motions[MotionEditor.instance.MotionNum].Infos.Count)
                Frame = 0;

            SingleInfo info = LearnManager.instance.MovementList[(int)MotionEditor.instance.MotionType].Motions[MotionEditor.instance.MotionNum].Infos[Frame];
            
            moveAll(info);
            LastMotion = Motion;
        }

        /*
        else if (type == PlayType.StandaloneRepeat || type == PlayType.StandaloneSequence)
        {
            
            NextTime = 1 / PlaybackSpeed;
            Timer += Time.deltaTime;
            if (Timer < NextTime)
                return;
            Timer = 0;
            Frame += 1;
            if (Frame == Agent.CurrentMotion().Infos.Count)
            {
                if (type == PlayType.StandaloneSequence)
                {
                    Motion += 1;
                }
                Frame = 0;
            }
            SingleInfo info = Agent.CurrentFrame();
            moveAll(info);
        }
        
        */
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
