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

    public MeshRenderer handToChange;
    public Material[] Materials;

    public bool DoLines = true;

    public Side side;
    public EditSettings Setting;




    public delegate void NewFrame();
    public static event NewFrame OnNewFrame;

    //public AthenCycler.Athena A => AthenCycler.AthenCycler.instance;
    public Runtime R => Runtime.instance;
    MotionEditor ME => MotionEditor.instance;

   
    private void Start()
    {
        MotionEditor.OnChangeMotion += OnSomethingChanged;
        if(type == PlayType.WatchPlayer)
        {
            Runtime.AllSpellChangeState += SpellStateChange;
            PastFrameRecorder.NewFrame += WatchPlayerUpdate;
        }
            
    }

    public void WatchPlayerUpdate(Side FrameSide)
    {
        if (side != FrameSide)
            return;
        if (!PastFrameRecorder.IsReady)
            return;

        AthenaFrame info = PastFrameRecorder.instance.GetFramesList(side, 1)[0];
        moveAll(info);
    }
    public void OnSomethingChanged()
    {
        Frame = 0;
    }
    public void SpellStateChange(Spell spell, Side side, int state)
    {
        if (spell == MotionEditor.instance.MotionType && side == Side.right && handToChange != null)
        {
            handToChange.material = Materials[state];
        }
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
            ME.FrameDisplay.text = "Frame: " + Frame;
            if (Frame >= Cycler.FrameCount(ME.MotionType, ME.MotionNum))
                Frame = 0;

            AthenaFrame info = new AthenaFrame(Cycler.AtFrameInfo(ME.MotionType, ME.MotionNum, Frame));
            if (side == Side.left)
                info.Devices[0] = info.Devices[0].Invert();
            if (Setting == EditSettings.Editing)
            {
                handToChange.material = Materials[Cycler.FrameWorks(ME.MotionType, ME.MotionNum, Frame) ? 1 : 0];
            }
            else if(Setting == EditSettings.DisplayingMotion)
            {
                /*
                List<AthenaFrame> Frames = Cycler.GetFrames(ME.MotionType, ME.MotionNum, ).instance.GetFramesList(side, Runtime.FramesAgoBuild);


                List<float> FrameValues = FrameToValues(Frames);
                int ActiveInputCount = FrameValues.Count / FramesAgoBuild;
                //Debug.Log(ActiveInputCount);
                Tensor input = new Tensor(1, 1, FramesAgoBuild, ActiveInputCount, FrameValues.ToArray());

                IWorker worker = R.Workers[spell][side];

                worker.Execute(input);
                Tensor output = worker.PeekOutput();

                int State = output.ArgMax()[0];

                if (SideStates[spell][side] != State)
                {
                    AllSpellChangeState?.Invoke(spell, side, State);
                    SpellHolder.Spells[spell].StateChangeEvent(side, State);
                    SideStates[spell][side] = State;
                }

                input.Dispose();
                output.Dispose();
                */
            }


            
            moveAll(info);


            LastMotion = Motion;
            OnNewFrame?.Invoke();
            //MotionRestriction GetMotionRestriction() { return ME.Setting == EditSettings.DisplayingBrute ? BruteForce.instance.BruteForceSettings : RestrictionManager.instance.RestrictionSettings.MotionRestrictions[(int)ME.MotionType - 1]; }
        }
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
    public void MoveDevice(Transform trans, DeviceInfo info)
    {
        trans.localPosition = info.Pos;
        trans.localEulerAngles = info.Rot * 360f;
    }
    public void SetLine(LineRenderer renderer, DeviceInfo info, bool Vel, Transform devicePos)
    {
        if (!DoLines)
            return;
        
        renderer.positionCount = 2;
        renderer.SetPosition(0, devicePos.position);
        renderer.SetPosition(1, devicePos.position + (Vel ? info.velocity : info.acceleration));
        //renderer.startColor = Vel ? Color.blue : Color.red;
    }
}
