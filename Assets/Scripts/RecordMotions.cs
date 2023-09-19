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
    private void Start()
    {
        //FrameInterval = 1 / FramesPerSecond;
        RecordingMotion = false;
        //CanRecordToggle.onValueChanged.AddListener(delegate {
            //OnToggleChanged(CanRecordToggle);
        //});
    }
    public bool HitRecordButton()
    {
        InputDevices.GetDeviceAtXRNode(XRNode.RightHand).TryGetFeatureValue(CommonUsages.trigger, out float Trigger);
        return Trigger > 0.5 || Input.GetKey(RecordKey);
    }
    private void Update()
    {
        if (CanRecordToggle.isOn == false)
            return;
        
        if (RecordingMotion == false && HitRecordButton() == true)
        {
            RecordingMotion = true;
        }
        else if (RecordingMotion == true && HitRecordButton() == false)
        {
            RecordingMotion = false;

            AthenaMotion FinalMotion = new AthenaMotion();
            FinalMotion.TrueRanges = AthenaMotion.ConvertToRange(Values);
            Values.Clear();

            FinalMotion.Infos = new List<AthenaFrame>(CurrentMotionRecord);
            if (TrueMotion.isOn)
            {
                FinalMotion.TrueRanges = new List<Vector2>() { Vector2.zero};
                Athena.Athena.instance.Movements[(int)MotionEditor.instance.MotionType].Motions.Insert(0, FinalMotion);
                //Vector2 Before = MotionAssign.instance.TrueMotions[(int)MotionEditor.instance.MotionType][0];
                //MotionAssign.instance.TrueMotions[(int)MotionEditor.instance.MotionType][0] = new Vector2(Before.x, Before.y + 1);
            }
            else
            {
                FinalMotion.TrueRanges.Clear();
                Athena.Athena.instance.Movements[(int)MotionEditor.instance.MotionType].Motions.Add(FinalMotion);
            }
                
            CurrentMotionRecord.Clear();
        }

        if (RecordingMotion == false)
            return;

        AthenaFrame info = PastFrameRecorder.instance.GetControllerInfo(Side.right);
        Values.Add(false);
        CurrentMotionRecord.Add(info);
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
            MovementControl.instance.Movements[(int)MotionEditor.instance.MotionType].Motions.Add(FinalMotion);
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
