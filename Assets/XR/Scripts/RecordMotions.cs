using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class RecordMotions : MonoBehaviour
{
    public int FramesPerSecond;
    private float FrameInterval;
    private AllMotions Motions;

    public Motion currentMotion;
    private bool RecordingMotion;
    public bool ShouldRecord;

    private float Timer;

    public List<bool> Values;
    private void Start()
    {
        Motions = LearnManager.instance.motions;
        FrameInterval = 1 / FramesPerSecond;
    }
    private void FixedUpdate()
    {
        if (ShouldRecord == false)
        {
            Timer = 0;
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
            FinalMotion.IntoRange(Values);
            Values.Clear();

            FinalMotion.Infos = new List<SingleInfo>(currentMotion.Infos);
            Motions.Motions.Add(FinalMotion);
            currentMotion.Infos.Clear();
        }

        if (RecordingMotion == false)
            return;

        SingleInfo info = LearnManager.instance.Info.GetControllerInfo(EditSide.right);
        Values.Add(LearnManager.instance.Right.TriggerPressed());
        currentMotion.Infos.Add(info);
    }
}
