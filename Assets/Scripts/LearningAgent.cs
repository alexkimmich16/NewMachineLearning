using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using SharpNeat.Phenomes;
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
namespace UnitySharpNEAT
{
    public class LearningAgent : UnitController
    {
        [Header("Set")]
        public DebugType DebugType;

        //[HideInInspector]
        public learningState state;

        [Header("Current")]
        public float Timer;
        public int Frame;
        public int Set;

        [Header("References")]

        public List<Material> FalseTrue;
        public SkinnedMeshRenderer handToChange;

        [Header("Other")]

        [HideInInspector]
        public bool Guess;
        public int GuessNum;

        public delegate void EventHandler(bool State, int Cycle, int Set);
        public event EventHandler MoveToNextEvent;

        public delegate void EventHandlerTwo();
        public event EventHandlerTwo FinalFrame;

        public EditSide side;

        public static List<Vector2> ListNegitives = new List<Vector2>() { new Vector2(1, 1), new Vector2(-1, 1), new Vector2(-1, -1), new Vector2(1, -1) };
        public static List<bool> InvertAngles = new List<bool>() { false, true, false, true };
        //public bool AngleTest;

        public SingleInfo MyInfo;
        
        public bool RequestNum;
        public int Streak;

        public float Fitness;

        public bool SentLearnManagerFinish;

        public bool Active() { return Frame < MaxFrame; }

        public int MaxFrame;
        private void Start()
        {
            LearnManager.OnNewMotion += RecieveNewMotion;
            int sibling = GetSiblingIndex(transform, transform.parent);
            transform.position = new Vector3(0,0, sibling * LearnManager.instance.SpawnGap);

            int GetSiblingIndex(Transform child, Transform parent)
            {
                for (int i = 0; i < parent.childCount; ++i)
                {
                    if (child == parent.GetChild(i))
                        return i;
                }
                Debug.LogWarning("Child doesn't belong to this parent.");
                return 0;
            }
        }
        
        void RecieveNewMotion(int LowStat, int HighStat, int SetStat)
        {
            //Debug.Log("recieve: " + HighStat);
            Frame = LowStat;
            MaxFrame = HighStat;
            SentLearnManagerFinish = false;
            Set = SetStat;
        }

        #region Overrides
        public override float GetFitness()
        {
            float RealFitness = Fitness;
            Fitness = 0;
            return RealFitness;
        }

        protected override void UseBlackBoxOutpts(ISignalArray outputSignalArray)//on output
        {
            if (!Active())
                return;
            CustomDebug("OnActionReceived");
            //outputSignalArray
            GuessNum = (int)outputSignalArray[0];
            bool CurrentGuess = (int)outputSignalArray[0] == 1;
            Guess = CurrentGuess;
            //float CurrentReward = LearnManager.instance.GetReward(Streak);
            ChangeStreak(CurrentGuess == CurrentMotion().AtFrameState(Frame));
            Fitness += LearnManager.instance.GetReward(Streak);
            if (Fitness < 0)
                Fitness = 0;
            handToChange.material = FalseTrue[GuessNum];

            //if (state == learningState.Learning)
        }
        protected override void UpdateBlackBoxInputs(ISignalArray inputSignalArray)//on Input
        {
            if (!Active())
            {
                if (SentLearnManagerFinish == false)
                {
                    LearnManager.instance.AgentWaiting();
                    SentLearnManagerFinish = true;
                }
                return;
            }
            CustomDebug("CollectObservations");
            LearnManager LM = LearnManager.instance;
            int CurrentIndex = 0;

            if (LM.HandPos)
                AddVector3(LM.motions.Motions[Set].Infos[Frame].HandPos);
            if (LM.HandRot)
                AddVector3(LM.motions.Motions[Set].Infos[Frame].HandRot);
            if (LM.HeadPos)
                AddVector3(LM.motions.Motions[Set].Infos[Frame].HeadPos);
            if (LM.HeadRot)
                AddVector3(LM.motions.Motions[Set].Infos[Frame].HeadRot);

            if(Frame < MaxFrame)
                Frame += 1;
            void AddVector3(Vector3 Input)
            {
                inputSignalArray[CurrentIndex] = Input.x;
                inputSignalArray[CurrentIndex + 1] = Input.y;
                inputSignalArray[CurrentIndex + 2] = Input.z;
                CurrentIndex += 3;
            }
        }
        protected override void HandleIsActiveChanged(bool newIsActive)
        {
            foreach (Transform t in transform)
            {
                t.gameObject.SetActive(newIsActive);
            }
        }
        #endregion
        void ChangeStreak(bool Outcome)
        {
            if (Outcome == true && Streak >= 0)
                Streak += 1;
            else if(Outcome == false && Streak <= 0)
                Streak -= 1;

            if (Outcome == true && Streak < 0 || Outcome == false && Streak > 0)
                Streak = 0;
            //Debug.Log("Reward Guess: " + GotRight + "  Works: " + ListWorks + "  Reward: " + LearnManager.instance.motions.GetReward(GotRight, ListWorks));
        }
        
        
        
        /*
        public void MoveToNext()
        {
            Timer = 0;
            //Frame += 1;
            
            if (Frame == CurrentMotion().Infos.Count)
            {
                Frame = 0;
                if (RequestNum)
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
        */
        //public SingleInfo GetCurrentInfo() { return (state == learningState.Learning) ? CurrentFrame() : LearnManager.instance.Info.GetControllerInfo(side); }
        //public SingleInfo CurrentFrame() { return LearnManager.instance.motions.Motions[Set].Infos[Frame]; }
        public Motion CurrentMotion() { return LearnManager.instance.motions.Motions[Set]; }

        public void CustomDebug(string text)
        {
            if (DebugType == DebugType.None)
                return;
            string FrameReference = "";
            if (DebugType == DebugType.WithState)
                FrameReference = " Timer: " + Timer + "|Frame: " + Frame + "|Set: " + Set + "" + "|";
            Debug.Log(text + FrameReference);
        }
        public List<int> GetRandomList()
        {
            List<int> NewList = new List<int>();
            for (int i = 0; i < LearnManager.instance.motions.Motions.Count; i++)
                NewList.Add(i);
            Shuffle.ShuffleSet(NewList);
            return NewList;
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
        HandActions MyHand()
        {
            if (side == EditSide.right)
                return LearnManager.instance.Right;
            else
                return LearnManager.instance.Left;
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
}
