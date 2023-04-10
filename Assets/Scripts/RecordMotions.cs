using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RestrictionSystem;
using UnityEngine.UI;
public class RecordMotions : MonoBehaviour
{
    //public int FramesPerSecond;
    //private float FrameInterval;

    public List<SingleInfo> CurrentMotionRecord = new List<SingleInfo>();
    private bool RecordingMotion;

    //private float Timer;

    public List<bool> Values;

    public Toggle CanRecordToggle;


    private void Start()
    {
        //FrameInterval = 1 / FramesPerSecond;
        RecordingMotion = false;
        //CanRecordToggle.onValueChanged.AddListener(delegate {
            //OnToggleChanged(CanRecordToggle);
        //});
    }

    private void Update()
    {
        if (CanRecordToggle.isOn == false)
            return;
        
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

            FinalMotion.Infos = new List<SingleInfo>(CurrentMotionRecord);
            LearnManager.instance.MovementList[(int)MotionEditor.instance.MotionType].Motions.Add(FinalMotion);
            CurrentMotionRecord.Clear();
        }

        if (RecordingMotion == false)
            return;

        SingleInfo info = PastFrameRecorder.instance.GetControllerInfo(Side.right);
        Values.Add(LearnManager.instance.Right.TriggerPressed());
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

            FinalMotion.Infos = new List<SingleInfo>(CurrentMotionRecord);
            LearnManager.instance.MovementList[(int)MotionEditor.instance.MotionType].Motions.Add(FinalMotion);
            CurrentMotionRecord.Clear();
        }

        if (RecordingMotion == false)
            return;

        SingleInfo info = PastFrameRecorder.instance.GetControllerInfo(Side.right);
        Values.Add(LearnManager.instance.Right.TriggerPressed());
        CurrentMotionRecord.Add(info);
    }
    */


}
