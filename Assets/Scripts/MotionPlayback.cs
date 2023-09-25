using System.Collections.Generic;
using UnityEngine;
using RestrictionSystem;
using System.Linq;
using Athena;
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
    public Transform Right;

    public SkinnedMeshRenderer handToChange;
    public Material[] Materials;



    public delegate void NewFrame();
    public static event NewFrame OnNewFrame;

    public Athena.Athena A => Athena.Athena.instance;
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
            
            if (Frame >= A.Movements[ME.MotionType].Motions[ME.MotionNum].Infos.Count)
                Frame = 0;
            //if (Frame - BruteForce.instance.PastFrameLookup >= 0)
                //CurrentMotions = RestrictionManager.instance.AllWorkingMotions(A.Movements[(int)ME.MotionType].GetRestrictionInfoAtIndex(ME.MotionNum, Frame - BruteForce.instance.PastFrameLookup), A.Movements[(int)ME.MotionType].GetRestrictionInfoAtIndex(ME.MotionNum, Frame));
            bool State = ME.Setting == EditSettings.Editing ? A.FrameWorks(ME.MotionType, ME.MotionNum, Frame) : GetMotionFromInput();
            //Debug.Log(State);
            //handToChange.material = DebugRestrictions.instance.Materials[State ? 1 : 0];
            handToChange.material = Materials[State ? 1 : 0];


            if (ME.Setting == EditSettings.DisplayingMotion)
            {
                //ConditionManager.instance.PassValue(State, ME.CurrentTestMotion, Side.right);
            }

            AthenaFrame info = A.Movements[ME.MotionType].Motions[ME.MotionNum].Infos[Frame];
            
            moveAll(info);
            LastMotion = Motion;
            OnNewFrame?.Invoke();

            
            bool GetMotionFromInput()
            {
                //return Frame - MotionAssign.instance.FramesAgo() >= 0 ? RestrictionManager.instance.MotionWorks(A.Movements[(int)ME.MotionType].GetRestrictionInfoAtIndex(ME.MotionNum, Frame - MotionAssign.instance.FramesAgo()), A.Movements[(int)ME.MotionType].GetRestrictionInfoAtIndex(ME.MotionNum, Frame), ME.MotionType) : false;
                PythonTest Py = PythonTest.instance;

                if (Frame <= R.FramesAgoBuild + 1)
                    return false;



                List<AthenaFrame> Frames = A.GetFrames(ME.MotionType, ME.MotionNum, Frame - R.FramesAgoBuild, Frame);
                List<float> Values = PythonTest.instance.FrameToValues(Frames);
                //Debug.Log(Values.Count);
                return R.PredictState(Values);
            }
            //MotionRestriction GetMotionRestriction() { return ME.Setting == EditSettings.DisplayingBrute ? BruteForce.instance.BruteForceSettings : RestrictionManager.instance.RestrictionSettings.MotionRestrictions[(int)ME.MotionType - 1]; }
        }
        void moveAll(AthenaFrame info)
        {
            MoveDevice(Right, info.Devices[0]);
            MoveDevice(Head, info.Devices[1]);
        }
    }
    public void MoveDevice(Transform trans, DeviceInfo info)
    {
        trans.localPosition = info.Pos;
        trans.localEulerAngles = info.Rot * 360f;
    }
    
}
