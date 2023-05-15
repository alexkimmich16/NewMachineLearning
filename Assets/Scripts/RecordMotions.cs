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
    public bool HitRecordButton() { return LearnManager.instance.Right.GripPressed() || Input.GetKey(RecordKey); }
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

            Motion FinalMotion = new Motion();
            FinalMotion.SetRanges(Values);
            Values.Clear();

            FinalMotion.Infos = new List<SingleInfo>(CurrentMotionRecord);
            if (TrueMotion.isOn)
            {
                FinalMotion.TrueRanges = new List<Vector2>() { Vector2.zero};
                LearnManager.instance.MovementList[(int)MotionEditor.instance.MotionType].Motions.Insert(0, FinalMotion);
                //Vector2 Before = MotionAssign.instance.TrueMotions[(int)MotionEditor.instance.MotionType][0];
                //MotionAssign.instance.TrueMotions[(int)MotionEditor.instance.MotionType][0] = new Vector2(Before.x, Before.y + 1);
            }
            else
            {
                FinalMotion.TrueRanges.Clear();
                LearnManager.instance.MovementList[(int)MotionEditor.instance.MotionType].Motions.Add(FinalMotion);
            }
                
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
