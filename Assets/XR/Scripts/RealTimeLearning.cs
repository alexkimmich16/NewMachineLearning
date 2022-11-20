using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class RealTimeLearning : Agent
{
    public static RealTimeLearning instance;
    private void Awake() { instance = this; }

    public int FramesPerSecond;
    private float FrameInterval;
    public List<Motion> Motions;

    public HandActions hand;

    public Motion currentMotion;
    private bool RecordingMotion;
    public bool ShouldRecord;

    private float Timer;

    public bool DoingWorkingMotion;
    /*
    public bool RequestMotion()
    {
        int Num = Random.Range(0, 2);
        return Num == 1;
    }

    public override void OnEpisodeBegin()
    {
        //Frame = 0;
    }
    public override void CollectObservations(VectorSensor sensor)
    {
        SingleInfo NowInfo = CurrentInfo();

        sensor.AddObservation(NowInfo.AdjustedHandPos);
        sensor.AddObservation(NowInfo.HeadRot.normalized);
        sensor.AddObservation(NowInfo.HandRot.normalized);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        void RewardAndEnd()
        {
            int Reward;
            //Guess
            if (Guess == MotionAI.instance.Motions[Set].IsWorkingMotion)
            {
                Reward = 1;
                Current.x += 1;
                Debug.Log("Got " + MotionAI.instance.Motions[Set].IsWorkingMotion + " Right");
            }
            else
            {
                Debug.Log("Got " + MotionAI.instance.Motions[Set].IsWorkingMotion + " Wrong");
                Current.y += 1;
                Reward = -1;
            }

            SetReward(Reward);
            //Debug.Log("Reward: " + Reward);
            //Debug.Log("Set: " + Set + "  Frame: " + Frame + "  Reward: " + Reward);
            EndEpisode();
        }
    }
    public SingleInfo CurrentInfo()
    {
        return MotionAI.instance.CurrentControllerInfo();
    }
    public bool ButtonsPressed()
    {
        return hand.TriggerPressed() && hand.GripPressed();
    }

    void Update()
    {
        if (ShouldRecord == false)
            return;
        if (RecordingMotion == false && ButtonsPressed() == true)
        {
            RecordingMotion = true;
        }
        else if (RecordingMotion == true && ButtonsPressed() == false)
        {
            RecordingMotion = false;
            Motion FinalMotion = new Motion();
            FinalMotion.Infos = new List<SingleInfo>(currentMotion.Infos);
            FinalMotion.IsWorkingMotion = DoingWorkingMotion;
            Motions.Add(FinalMotion);
            currentMotion.Infos.Clear();
        }

        if (RecordingMotion == false)
            return;
        Timer += Time.deltaTime;

        if (Timer < FrameInterval)
            return;
        Timer = 0;
        
    }
    */
}
