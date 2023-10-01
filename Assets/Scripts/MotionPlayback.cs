using System.Collections.Generic;
using UnityEngine;
using RestrictionSystem;
using System.Linq;
using Athena;
public enum PlayType
{
    Repeat = 0,
    WatchPlayer = 1
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
    public Transform Right;

    public List<LineRenderer> Velocities;
    public List<LineRenderer> Accelerations;

    public SkinnedMeshRenderer handToChange;
    public Material[] Materials;

    



    public delegate void NewFrame();
    public static event NewFrame OnNewFrame;

    //public AthenCycler.Athena A => AthenCycler.AthenCycler.instance;
    public Runtime R => Runtime.instance;
    MotionEditor ME => MotionEditor.instance;

   
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
            Frame += 1;
            
            if (Frame >= Cycler.FrameCount(ME.MotionType, ME.MotionNum))
                Frame = 0;
            //if (Frame - BruteForce.instance.PastFrameLookup >= 0)
                //CurrentMotions = RestrictionManager.instance.AllWorkingMotions(Cycler.Movements[(int)ME.MotionType].GetRestrictionInfoAtIndex(ME.MotionNum, Frame - BruteForce.instance.PastFrameLookup), Cycler.Movements[(int)ME.MotionType].GetRestrictionInfoAtIndex(ME.MotionNum, Frame));
            bool State = ME.Setting == EditSettings.Editing ? Cycler.FrameWorks(ME.MotionType, ME.MotionNum, Frame) : GetMotionFromInput();
            //Debug.Log(State);
            //handToChange.material = DebugRestrictions.instance.Materials[State ? 1 : 0];
            handToChange.material = Materials[State ? 1 : 0];


            if (ME.Setting == EditSettings.DisplayingMotion)
            {
                //ConditionManager.instance.PassValue(State, ME.CurrentTestMotion, Side.right);
            }

            AthenaFrame info = Cycler.AtFrameInfo(ME.MotionType, ME.MotionNum, Frame);
            
            moveAll(info);
            LastMotion = Motion;
            OnNewFrame?.Invoke();
            
            
            bool GetMotionFromInput()
            {
                //return Frame - MotionAssign.instance.FramesAgo() >= 0 ? RestrictionManager.instance.MotionWorks(Cycler.Movements[(int)ME.MotionType].GetRestrictionInfoAtIndex(ME.MotionNum, Frame - MotionAssign.instance.FramesAgo()), Cycler.Movements[(int)ME.MotionType].GetRestrictionInfoAtIndex(ME.MotionNum, Frame), ME.MotionType) : false;

                if (Frame <= Runtime.FramesAgoBuild + 1)
                    return false;



                List<AthenaFrame> Frames = Cycler.GetFrames(ME.MotionType, ME.MotionNum, Frame - Runtime.FramesAgoBuild, Frame);
                List<float> Values = Runtime.FrameToValues(Frames);
                //Debug.Log(Values.Count);
                return R.PredictState(Values, ME.MotionType) == 1;
            }

            //MotionRestriction GetMotionRestriction() { return ME.Setting == EditSettings.DisplayingBrute ? BruteForce.instance.BruteForceSettings : RestrictionManager.instance.RestrictionSettings.MotionRestrictions[(int)ME.MotionType - 1]; }
        }
        else if(type == PlayType.WatchPlayer)
        {
            if (!PastFrameRecorder.IsReady)
                return;
            AthenaFrame info = PastFrameRecorder.instance.GetFramesList(Side.right, 1)[0];
            moveAll(info);
        }
        void moveAll(AthenaFrame info)
        {
            MoveDevice(Right, info.Devices[0]);
            MoveDevice(Head, info.Devices[1]);

            SetLine(Velocities[0], info.Devices[0], true, Right);
            SetLine(Velocities[1], info.Devices[1], true, Head);

            SetLine(Accelerations[0], info.Devices[0], false, Right);
            SetLine(Accelerations[1], info.Devices[1], false, Head);
        }
    }
    public void MoveDevice(Transform trans, DeviceInfo info)
    {
        trans.localPosition = info.Pos;
        trans.localEulerAngles = info.Rot * 360f;
    }
    public void SetLine(LineRenderer renderer, DeviceInfo info, bool Vel, Transform devicePos)
    {
        renderer.positionCount = 2;
        renderer.SetPosition(0, devicePos.position);
        renderer.SetPosition(1, devicePos.position + (Vel ? info.velocity : info.acceleration));
        renderer.startColor = Vel ? Color.blue : Color.red;
    }
}
