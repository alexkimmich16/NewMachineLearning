using System.Collections.Generic;
using UnityEngine;
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



    public List<Spell> CurrentMotions;
    public bool OldFrameWorks() { return Frame - PastFrameRecorder.instance.FramesAgo() >= 0; }
    public SingleInfo GetFrameInfo(bool Old) { return MovementControl.instance.Movements[(int)MotionEditor.instance.MotionType].GetRestrictionInfoAtIndex(MotionEditor.instance.MotionNum, Old ? MinFramesAgo() : Frame); }
    public int MinFramesAgo() { return Frame - PastFrameRecorder.instance.FramesAgo() >= 0 ? Frame - PastFrameRecorder.instance.FramesAgo() : 0; }
    private void Start()
    {
        MotionEditor.OnChangeMotion += OnSomethingChanged;
    }
    public void OnSomethingChanged()
    {
        Frame = 0;
    }
    void Update()
    {
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
            MotionEditor ME = MotionEditor.instance;
            
            
            if (Frame >= MovementControl.instance.Movements[(int)ME.MotionType].Motions[ME.MotionNum].Infos.Count)
                Frame = 0;
            //if (Frame - BruteForce.instance.PastFrameLookup >= 0)
                //CurrentMotions = RestrictionManager.instance.AllWorkingMotions(MovementControl.instance.Movements[(int)ME.MotionType].GetRestrictionInfoAtIndex(ME.MotionNum, Frame - BruteForce.instance.PastFrameLookup), MovementControl.instance.Movements[(int)ME.MotionType].GetRestrictionInfoAtIndex(ME.MotionNum, Frame));
            bool State = ME.Setting == EditSettings.Editing ? MovementControl.instance.Movements[(int)ME.MotionType].Motions[ME.MotionNum].AtFrameState(Frame) : GetMotionFromInput();
            handToChange.material = DebugRestrictions.instance.Materials[State ? 1 : 0];

            
            if (ME.Setting == EditSettings.DisplayingMotion)
            {
                //ConditionManager.instance.PassValue(State, ME.CurrentTestMotion, Side.right);
            }

            SingleInfo info = MovementControl.instance.Movements[(int)ME.MotionType].Motions[ME.MotionNum].Infos[Frame];
            
            moveAll(info);
            LastMotion = Motion;
            OnNewFrame?.Invoke();

            
            bool GetMotionFromInput() { return Frame - MotionAssign.instance.FramesAgo() >= 0 ? RestrictionManager.instance.MotionWorks(MovementControl.instance.Movements[(int)ME.MotionType].GetRestrictionInfoAtIndex(ME.MotionNum, Frame - MotionAssign.instance.FramesAgo()), MovementControl.instance.Movements[(int)ME.MotionType].GetRestrictionInfoAtIndex(ME.MotionNum, Frame), ME.MotionType) : false; }
            //MotionRestriction GetMotionRestriction() { return ME.Setting == EditSettings.DisplayingBrute ? BruteForce.instance.BruteForceSettings : RestrictionManager.instance.RestrictionSettings.MotionRestrictions[(int)ME.MotionType - 1]; }
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
