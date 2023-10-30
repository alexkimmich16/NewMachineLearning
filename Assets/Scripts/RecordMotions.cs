using System.Collections.Generic;
using UnityEngine;
using RestrictionSystem;
using UnityEngine.UI;
using UnityEngine.XR;
using Athena;
public class RecordMotions : MonoBehaviour
{
    //public int FramesPerSecond;
    //private float FrameInterval;

    public List<AthenaFrame> CurrentMotionRecord = new List<AthenaFrame>();
    private bool RecordingMotion;

    //private float Timer;

    public List<bool> Values;

    public Toggle CanRecordToggle;
    public Toggle TrueMotion;

    public KeyCode RecordKey;

    public Side RecordSide;
    private void Start()
    {
        RecordingMotion = false;
    }
    public bool HitRecordButton(out Side side)
    {
        side = Side.right;
        InputDevices.GetDeviceAtXRNode(XRNode.RightHand).TryGetFeatureValue(CommonUsages.gripButton, out bool RightPress);
        InputDevices.GetDeviceAtXRNode(XRNode.LeftHand).TryGetFeatureValue(CommonUsages.gripButton, out bool LeftPress);
        if (RightPress || LeftPress)
            side = RightPress ? Side.right : Side.left;


        return RightPress || LeftPress || Input.GetKey(RecordKey);
    }

    public void NewRecordingState(bool State)
    {
        RecordingMotion = State;
        if (State == true)
            PastFrameRecorder.NewFrame += NewFrame;
        else
            PastFrameRecorder.NewFrame -= NewFrame;
    }

    private void Update()
    {
        if (CanRecordToggle.isOn == false)
            return;

        bool RecordButtonHit = HitRecordButton(out Side side);

        if (RecordingMotion == false && RecordButtonHit == true)
        {
            RecordSide = side; // THIS FOR ALLOW BOTH SIDES
            NewRecordingState(true);
        }
        else if (RecordingMotion == true && RecordButtonHit == false)
        {
            NewRecordingState(false);

            AthenaMotion FinalMotion = new AthenaMotion();
            FinalMotion.TrueRanges = AthenaMotion.ConvertToRange(Values);
            Values.Clear();
            FinalMotion.Infos = new List<AthenaFrame>(CurrentMotionRecord);
            FinalMotion.TrueRanges.Clear();
            if (TrueMotion.isOn)
            {
                Cycler.Movements[MotionEditor.instance.MotionType].Motions.Insert(0, FinalMotion);
                Cycler.Movements[MotionEditor.instance.MotionType].TrueMotions.y += 1;
            }
            else
            {
                Cycler.Movements[MotionEditor.instance.MotionType].Motions.Add(FinalMotion);
            }
            
            

            CurrentMotionRecord.Clear();
        }        
    }
    public void NewFrame(Side side)
    {
        if(side == RecordSide)
        {
            //AthenaFrame info = PastFrameRecorder.instance.GetControllerInfo(Side.right);
            AthenaFrame info = PastFrameRecorder.instance.GetFramesList(side, 1)[0];
            Values.Add(false);
            CurrentMotionRecord.Add(info);

        }
    }
    /*
    private void FixedUpdate()
    {
        if (ShouldRecord == false)
        {
            //Timer = 0;
            return;
        }
        
        Timer += Time.deltaTime;
        if (Timer < FrameInterval)
            return;
        Timer = 0;
        
        if (RecordingMotion == false && LearnManager.instance.Right.GripPressed() == true)
        {
            RecordingMotion = true;
        }
        else if (RecordingMotion == true && LearnManager.instance.Right.GripPressed() == false)
        {
            RecordingMotion = false;

            Motion FinalMotion = new Motion();
            FinalMotion.SetRanges(Values);
            Values.Clear();

            FinalMotion.Infos = new List<AthenaFrame>(CurrentMotionRecord);
            A.Movements[(int)MotionEditor.instance.MotionType].Motions.Add(FinalMotion);
            CurrentMotionRecord.Clear();
        }

        if (RecordingMotion == false)
            return;

        AthenaFrame info = PastFrameRecorder.instance.GetControllerInfo(Side.right);
        Values.Add(LearnManager.instance.Right.TriggerPressed());
        CurrentMotionRecord.Add(info);
    }
    */


}
