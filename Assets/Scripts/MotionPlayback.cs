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
    void Update()
    {
        //handToChange.material = LearnManager.instance.FalseTrue[Convert.ToInt32(Agent.CurrentMotion().AtFrameState(Frame))];
        //handToChange.material = LearnManager.instance.FalseTrue[Convert.ToInt32(LearnManager.instance.motions.Motions[Motion].AtFrameState(Frame))];
        if (type == PlayType.WatchAI)
        {
            //Debug.Log("move");
            LearnManager LM = LearnManager.instance;
            SingleInfo info = LM.motions.Motions[LM.Set].Infos[LM.CurrentFrame + LM.FramesBeforeRecalculation];
            moveAll(info);
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
        else if (type == PlayType.Repeat)
        {
            NextTime = 1 / PlaybackSpeed;
            Timer += Time.deltaTime;
            if (Timer < NextTime)
                return;
            Timer = 0;
            Frame += 1;
            if(LastMotion != Motion)
                Frame = 0;
            if (Frame == LearnManager.instance.motions.Motions[Motion].Infos.Count)
                Frame = 0;
            SingleInfo info = LearnManager.instance.motions.Motions[Motion].Infos[Frame];
            
            moveAll(info);
            LastMotion = Motion;
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
