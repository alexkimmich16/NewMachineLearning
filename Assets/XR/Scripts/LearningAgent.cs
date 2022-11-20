using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using System;
//https://www.phyley.com/decompose-force-into-xy-components
public enum learningState
{
    Learning = 0,
    Testing = 1,
}
public enum DebugType
{
    None = 0,
    Basic = 1,
    WithState = 2,
}

public class LearningAgent : Agent
{
    public static LearningAgent instance;
    private void Awake() { instance = this; }
    [Header("Set")]
    
    public int DesiredCycles;
    public DebugType DebugType;

    //[HideInInspector]
    public learningState state;

    [Header("Current")]
    public float Timer;
    public int Frame;
    public int Set;
    public int CycleNum;

    [Header("References")]

    public List<Material> FalseTrue;
    public SkinnedMeshRenderer handToChange;

    [Header("Other")]

    [HideInInspector]
    public bool Guess;
    public int GuessNum;

    //[HideInInspector]
    //public List<Motion> Motions;

    public delegate void EventHandler(bool State, int Cycle, int Set);
    public event EventHandler MoveToNextEvent;

    public delegate void EventHandlerTwo();
    public event EventHandlerTwo FinalFrame;

    public EditSide side;

    public GameObject RightTest;
    //public SingleInfo info;

    public float SetDegrees;

    public float HandCamAngle, CamAngle, EndAngle;

    public static List<Vector2> ListNegitives = new List<Vector2>() { new Vector2(1, 1), new Vector2(-1, 1), new Vector2(-1, -1), new Vector2(1, -1) };
    public static List<bool> InvertAngles = new List<bool>() { false, true, false, true };
    public bool AngleTest;
    //private int Offset = 270;

    //public List<int> Index;

    public SingleInfo MyInfo;

    public bool RequestNum;

    public List<int> GetRandomList()
    {
        List<int> NewList = new List<int>();
        for (int i = 0; i < LearnManager.instance.motions.Motions.Count; i++)
            NewList.Add(i);
        Shuffle.ShuffleSet(NewList);
        return NewList;
    }
    //public 
    public void LearnStep()
    {
        //Debug.Log("Learnst: " + state.ToString());
        if (state == learningState.Learning)
        {
            if (Frame == 0)
                MoveToNext();
            else
            {
                RequestDecision();
                CustomDebug("RequestDecision");
            }
        }
        else if (state == learningState.Testing)
        {
            RequestDecision();
            //CustomDebug("RequestDecision");
            Timer = 0;
        }
    }
    public override void CollectObservations(VectorSensor sensor)
    {
        //CustomDebug("CollectObservations");
        //Debug.Log("Observe  Frame: " + Frame + " Set: " + Set);
        LearnManager LM = LearnManager.instance;
        SingleInfo info = GetCurrentInfo();
        MyInfo = info;
        if (LM.HeadPos)
            sensor.AddObservation(info.HeadPos);
        if (LM.HeadRot)
            sensor.AddObservation(info.HeadRot.normalized);
        if (LM.HandPos)
            sensor.AddObservation(info.HandPos);
        if (LM.HandRot)
            sensor.AddObservation(info.HandRot.normalized);
        if (LM.HandVel)
            sensor.AddObservation(info.HandVel);
        if (LM.AdjustedHandPos)
            sensor.AddObservation(info.AdjustedHandPos);

        RequestAction();
    }
    public override void OnActionReceived(ActionBuffers actions)
    {
        //CustomDebug("OnActionReceived");
        
        bool CurrentGuess = actions.DiscreteActions[0] == 1;

        //Debug.Log(CurrentGuess);
        Guess = CurrentGuess;
        GuessNum = actions.DiscreteActions[0];
        //handToChange.material = LearnManager.instance.FalseTrue[Convert.ToInt32(GotRight)];
        if (state == learningState.Learning)
            Reward();
        else if (state == learningState.Testing)
            handToChange.material = FalseTrue[GuessNum];
    }
    void Reward()
    {
        
        bool ListWorks = CurrentMotion().AtFrameState(Frame);
        bool GotRight = Guess == ListWorks;

        
        if (GotRight)
        {
            //Debug.Log("Reward");
            //set red
        }
        else
        {
            //Debug.Log("Punish");
            //set blue
        }

        Debug.Log("Reward Guess: " + GotRight + "  Works: " + ListWorks + "  Reward: " + LearnManager.instance.motions.GetReward(GotRight, ListWorks));

        SetReward(LearnManager.instance.motions.GetReward(GotRight, ListWorks));
        MoveToNextEvent(GotRight, CycleNum, CurrentMotion().TrueIndex);
        MoveToNext();
    }
    public void MoveToNext()
    {
        Timer = 0;
        Frame += 1;
        if (Frame == CurrentMotion().Infos.Count)
        {
            Frame = 0;
            if(RequestNum)
                Set = LearnManager.instance.RequestMotion();
            else
            {
                Set += 1;
                if (Set == LearnManager.instance.motions.Motions.Count - 1)
                {
                    if (CycleNum < DesiredCycles - 1)
                    {
                        //restart
                        Set = 0;
                        CycleNum += 1;
                        //Index = GetRandomList();
                    }
                    else
                    {
                        FinalFrame();
                        LearnManager.instance.LearnReached -= LearnStep;
                        Frame = 0;
                        Set = 0;
                        CustomDebug("Done");
                    }
                }
            }
            
        }
    }
    public void CustomDebug(string text)
    {
        if (DebugType == DebugType.None)
            return;
        string FrameReference = "";
        if (DebugType == DebugType.WithState)
            FrameReference = " Timer: " + Timer + "|Frame: " + Frame + "|Set: " + Set + "" + "|CycleNum: " + CycleNum + "|";
        Debug.Log(text + FrameReference);
    }
    private void Start()
    {
        //Index = GetRandomList();
        if(RequestNum)
            Set = LearnManager.instance.RequestMotion();
    }
    public bool LastState()
    {
        if (Frame - 1 >= 0)
            return CurrentMotion().AtFrameState(Frame - 1);
        else
        {
            if (Set == 0)
                return true;
            else
            {
                //goo to last set
                int LastSize = LearnManager.instance.motions.Motions[Set - 1].Infos.Count - 1;
                return LearnManager.instance.motions.Motions[Set - 1].AtFrameState(LastSize);
            }
        }
    }
    public SingleInfo GetCurrentInfo()
    {
        if (state == learningState.Learning)
            return CurrentFrame();
        else
            return LearnManager.instance.Info.GetControllerInfo(side);
    }
    /*
    public SingleInfo CurrentControllerInfo()
    {
        LearnManager LM = LearnManager.instance;
        SingleInfo newInfo = new SingleInfo();
        bool ConstantRot = true;
        HandActions controller = MyHand();
        newInfo.HeadPos = LM.Cam.localPosition;
        newInfo.HeadRot = LM.Cam.rotation.eulerAngles;
        if (Right)
        {
            if(ConstantRot == false)
            {
                newInfo.HandPos = controller.transform.localPosition;
                newInfo.HandRot = controller.transform.localRotation.eulerAngles;
                newInfo.HandVel = controller.Velocity;

                newInfo.AdjustedHandPos = LM.Cam.position - controller.transform.position;

                //newInfo.Works = controller.TriggerPressed();

                return newInfo;
            }
            else
            {
                float CamRot = LM.Cam.rotation.eulerAngles.y;
                Vector3 handPos = controller.transform.localPosition;
                newInfo.HeadPos = new Vector3(LM.Cam.localPosition.x, 0, LM.Cam.localPosition.z);
                Vector3 LevelCamPos = new Vector3(newInfo.HeadPos.x, 0, newInfo.HeadPos.z);
                Vector3 LevelHandPos = new Vector3(handPos.x, 0, handPos.z);
                Vector3 targetDir = LevelCamPos - LevelHandPos;

                Vector3 StartEulerAngles = LM.Cam.eulerAngles;
                LM.Cam.eulerAngles = new Vector3(0, CamRot, 0);

                Vector3 forwardDir = LM.Cam.rotation * Vector3.forward;
                LM.Cam.eulerAngles = StartEulerAngles;

                float NewHandCamAngle = Vector3.SignedAngle(targetDir, forwardDir, Vector3.up) + 180;

                //reset hand
                //
                return newInfo;
            }
        }
        else
        {
            //CamRot
            float CamRot = LM.Cam.rotation.eulerAngles.y;
            CamAngle = CamRot;
            Vector3 handPos = controller.transform.localPosition;
            float Distance = Vector3.Distance(new Vector3(handPos.x, 0, handPos.z), new Vector3(newInfo.HeadPos.x, 0, newInfo.HeadPos.z));
            //pos
            Vector3 LevelCamPos = new Vector3(newInfo.HeadPos.x, 0, newInfo.HeadPos.z);
            Vector3 LevelHandPos = new Vector3(handPos.x, 0, handPos.z);
            Vector3 targetDir = LevelCamPos - LevelHandPos;

            Vector3 StartEulerAngles = LM.Cam.eulerAngles;
            LM.Cam.eulerAngles = new Vector3(0, CamRot, 0);

            Vector3 forwardDir = LM.Cam.rotation * Vector3.forward;
            LM.Cam.eulerAngles = StartEulerAngles;

            HandCamAngle = Vector3.SignedAngle(targetDir, forwardDir, Vector3.up) + 180;

            float Angle = GetAngle(CamRot);
            EndAngle = Angle;

            newInfo.AdjustedHandPos = IntoComponents(Angle);
            Vector2 XYForce = IntoComponents(Angle);
            Vector3 AdjustedCamPos = new Vector3(XYForce.x, 0, XYForce.y);

            Vector3 Point = (AdjustedCamPos * Distance) + new Vector3(newInfo.HeadPos.x, 0, newInfo.HeadPos.z);
            Point = new Vector3(Point.x, handPos.y, Point.z);
            newInfo.HandPos = Point;

            ///additional rotation
            Vector3 Rotation = controller.transform.rotation.eulerAngles;
            newInfo.HandRot = new Vector3(Rotation.x, Rotation.y, -Rotation.z);

            ///velocity
            Vector2 InputVelocity = new Vector2(controller.Velocity.x, controller.Velocity.z);
            Vector2 FoundVelocity = mirrorImage(IntoComponents(CamAngle), InputVelocity);
            newInfo.HandVel = new Vector3(FoundVelocity.x, controller.Velocity.y, FoundVelocity.y);

            //newInfo.Works = controller.TriggerPressed();
            return newInfo;
            Vector2 IntoComponents(float Angle)
            {
                int Quad = GetQuad(Angle, out float RemainingAngle);
                if (InvertAngles[Quad])
                    RemainingAngle = 90 - RemainingAngle;

                //Negitives
                float Radians = RemainingAngle * Mathf.Deg2Rad;
                //Debug.Log("remainAngle: " + Radians);

                float XForce = Mathf.Cos(Radians) * ListNegitives[Quad].x;
                float YForce = Mathf.Sin(Radians) * ListNegitives[Quad].y;

                return new Vector2(XForce, YForce);

                int GetQuad(float Angle, out float RemainAngle)
                {
                    //0-360
                    int TotalQuads = (int)(Angle / 90);
                    RemainAngle = Angle - TotalQuads * 90;
                    //int Quad = StartQuad;
                    if (TotalQuads < 0)
                    {
                        TotalQuads += 4;
                    }

                    int RemoveExcessQuads = (int)(TotalQuads / 4);
                    //RemainAngle = Angle;
                    return TotalQuads - RemoveExcessQuads * 4;
                }
            }
            static Vector2 mirrorImage(Vector2 Line, Vector2 Pos)
            {
                float temp = (-2 * (Line.x * Pos.x + Line.y * Pos.y)) / (Line.x * Line.x + Line.y * Line.y);
                float x = (temp * Line.x) + Pos.x;
                float y = (temp * Line.y) + Pos.y;
                return new Vector2(x, y);
            }
            float GetAngle(float CamRot)
            {
                float Angle = 360 - (CamRot + HandCamAngle + Offset);
                //Offset
                if (Angle > 360)
                    Angle -= 360;
                else if (Angle < -360)
                    Angle += 360;
                return Angle;
            }
        }
    }
    */
    public SingleInfo CurrentFrame()
    {
        return LearnManager.instance.motions.Motions[Set].Infos[Frame];
    }
    public Motion CurrentMotion()
    {
        return LearnManager.instance.motions.Motions[Set];
    }

    HandActions MyHand()
    {
        if (side == EditSide.right)
            return LearnManager.instance.Right;
        else
            return LearnManager.instance.Left;
    }
    
}
[System.Serializable]
public class SingleInfo
{
    public Vector3 HeadPos, HeadRot, HandPos, HandRot, HandVel, AdjustedHandPos;
    //public bool Works;
}
[System.Serializable]
public class Motion
{
    //[HideInInspector]
    public List<SingleInfo> Infos;
    public List<Vector2> TrueRanges;
    public int TrueIndex;

    public void IntoRange(List<bool> Values)
    {
        List<Vector2> ranges = new List<Vector2>();
        bool Last = false;
        int Start = 0;
        for (int i = 0; i < Values.Count; i++)
        {
            if (Values[i] != Last)
            {
                //onchange
                if(Last == false)
                {
                    Start = i;
                }
                else if(Last == true)
                {
                    //range is start to i
                    ranges.Add(new Vector2(Start, i - 1));
                }
                Last = Values[i];
            }
        }
        TrueRanges = ranges;
    }
    public bool AtFrameState(int Frame)
    {
        for (int i = 0; i < TrueRanges.Count; i++)
            if (Frame >= TrueRanges[i].x && Frame <= TrueRanges[i].y)
                return true;
        return false;
    }
}

///should contain: velocity, hand rot, hand pos, head rot, head pos, 
///
///possible ways of input/recording:
///1: as lists containing info, generated from player motions
///2: randomly generated motions
///3: doing it in engine
///
///possible ways of learning given info:
///1: operator gives start and end time if at all(would require display and repeat motion)
///2: 
///

///should be able to tell if motion is true between 2 given frames

///OR give it lists, with active times determined ahead of time when given