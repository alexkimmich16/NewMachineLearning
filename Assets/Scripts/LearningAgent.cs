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
        

        [Header("Current")]
        public int Frame;
        public int Set;
        public int MaxFrame;
        public int MotionIndex;
        [Header("References")]

        public List<Material> FalseTrue;
        public SkinnedMeshRenderer handToChange;

        [Header("Other")]

        [HideInInspector]
        public CurrentLearn CurrentGuess;

        public delegate void EventHandlerTwo();
        public event EventHandlerTwo FinalFrame;

        public EditSide side;

        
        //public bool AngleTest;

        [HideInInspector] public SingleInfo MyInfo;
        
        public int Streak;

        public float Fitness;

        [HideInInspector] public bool SentLearnManagerFinish;

        public List<float> WeightedGuesses;
        
        public bool Active() { return Frame < MaxFrame; }

        
        private void Start()
        {
            LearnManager.OnNewMotion += RecieveNewMotion;
            int sibling = GetSiblingIndex(transform, transform.parent);
            transform.position = new Vector3(0,0, sibling * LearnManager.instance.SpawnGap);
            for (int i = 0; i < LearnManager.instance.MovementList.Count; ++i)
                WeightedGuesses.Add(0);
            int GetSiblingIndex(Transform child, Transform parent)
            {
                for (int i = 0; i < parent.childCount; ++i)
                    if (child == parent.GetChild(i))
                        return i;
                Debug.LogWarning("Child doesn't belong to this parent.");
                return 0;
            }
        }
        void RecieveNewMotion(int Motion, int SetStat)
        {
            SentLearnManagerFinish = false;
            Set = SetStat;
            MotionIndex = Motion;
            LearnManager LM = LearnManager.instance;
            Frame = LM.FeedFrames;
            MaxFrame = LM.MovementList[Motion].Motions[SetStat].Infos.Count - 1;
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

            int Guess = GetHighest();
            CurrentGuess = (CurrentLearn)Guess;
            bool IsCorrect = Guess == MotionIndex;

            ChangeStreak(IsCorrect);
            DataTracker.CallGuess(IsCorrect);
            Fitness += LearnManager.instance.GetReward(Streak);
            if (Fitness < 0)
                Fitness = 0;
            handToChange.material = FalseTrue[Convert.ToInt32(IsCorrect)];
           
            int GetHighest()
            {
                float Highest = 0;
                int index = 0;
                for (int i = 0; i < outputSignalArray.Length; i++)
                {
                    WeightedGuesses[i] = (float)outputSignalArray[i] * 1000;
                    if (outputSignalArray[i] > Highest)
                        index = i;
                }
                return index;
            }
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
            for (int i = 0; i < LearnManager.instance.FeedFrames; i++)
            {
                int CurrentFrame = Frame - i;
                SingleInfo Info = (LM.state == learningState.Learning) ? LM.MovementList[MotionIndex].Motions[Set].Infos[CurrentFrame] : LM.PastFrame(side, CurrentFrame);
                //Debug.Log(MotionIndex + "  " +  Set + "  " + CurrentFrame);
                if (LM.HandPos)
                    AddVector3(Info.HandPos);
                if (LM.HandRot)
                    AddVector3(Info.HandRot);
                if (LM.HeadPos)
                    AddVector3(Info.HeadPos);
                if (LM.HeadRot)
                    AddVector3(Info.HeadRot);
            }

            if(Frame < MaxFrame)
                Frame += 1;


            void AddVector3(Vector3 Input)
            {
                if (LearnManager.instance.ConvertToBytes == false)
                {
                    inputSignalArray[CurrentIndex] = Input.x;
                    inputSignalArray[CurrentIndex + 1] = Input.y;
                    inputSignalArray[CurrentIndex + 2] = Input.z;
                }
                else
                {
                    inputSignalArray[CurrentIndex] = Convert.ToByte(Input.x);
                    inputSignalArray[CurrentIndex + 1] = Convert.ToByte(Input.y);
                    inputSignalArray[CurrentIndex + 2] = Convert.ToByte(Input.z);
                }
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

        public Motion CurrentMotion() { return LearnManager.instance.MovementList[MotionIndex].Motions[Set]; }

        public void CustomDebug(string text)
        {
            if (DebugType == DebugType.None)
                return;
            string FrameReference = "";
            if (DebugType == DebugType.WithState)
                FrameReference = "|Frame: " + Frame + "|Set: " + Set + "" + "|";
            Debug.Log(text + FrameReference);
        }
        /*
        public List<int> GetRandomList()
        {
            List<int> NewList = new List<int>();
            for (int i = 0; i < LearnManager.instance.motions.Motions.Count; i++)
                NewList.Add(i);
            Shuffle.ShuffleSet(NewList);
            return NewList;
        }  
        */
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
