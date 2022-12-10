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

        
        public delegate void EventHandlerTwo();
        public event EventHandlerTwo FinalFrame;
        [Header("Other")]
        public EditSide side;

        
        //public bool AngleTest;

        [HideInInspector] public SingleInfo MyInfo;
        
        public int Streak;

        public float Fitness;

        [HideInInspector] public bool SentLearnManagerFinish;

        public List<float> WeightedGuesses;
        //public List<float> Sort;
        public bool Conflict;
        public float Reward;
        public void OnNewGeneration()
        {
            Fitness = 0;
        }
        public bool Active() { return Frame < MaxFrame; }
        [Header("Output")]
        public CurrentLearn CurrentGuess;
        public CurrentLearn RealMotion;
        public bool Iscorrect;

        public int GuessStreak;
        public CurrentLearn lastGuess;

        public List<CurrentLearn> Guesses;

        public bool IsLogger;

        private float WeightOutputMultiplier = 1000f;
        
        private void Start()
        {
            LearnManager.OnNewMotion += RecieveNewMotion;
            int sibling = GetSiblingIndex(transform, transform.parent);
            IsLogger = sibling == 0;
            transform.position = new Vector3(0,0, sibling * LearnManager.instance.SpawnGap);
            for (int i = 0; i < LearnManager.instance.MovementList.Count; ++i)
                WeightedGuesses.Add(0);
            LearnManager.instance.OnNewGen += OnNewGeneration;
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
            Frame = LM.FramesToFeedAI;
            //LM.MovementList[Motion].Motions[SetStat].Infos.Count - 1
            MaxFrame = LM.MovementList[Motion].Motions[SetStat].Infos.Count - 1;
            Guesses.Clear();
        }
        #region Overrides
        public override float GetFitness()
        {
            float RealFitness = Fitness;
            return RealFitness;
        }
        protected override void UseBlackBoxOutpts(ISignalArray outputSignalArray)//on output
        {
            if (!Active())
                return;
            CustomDebug("OnActionReceived");

            bool IndexWorks = LearnManager.instance.MovementList[MotionIndex].Motions[Set].AtFrameState(Frame);

            CurrentLearn TrueMotion = (CurrentLearn)((IndexWorks) ? MotionIndex : 0);
            CurrentLearn Guess = (CurrentLearn)GetHighest(out Conflict);
            if(IsLogger)
                DataTracker.instance.LogGuess(MotionIndex, Set);
            
            Fitness += FitnessIncrease();

            if (Fitness < 0)
                Fitness = 0;

            ChangeSameGuessStreak(Guess);
            SetVariablesPublic();
            float FitnessIncrease()
            {
                LearnManager LM = LearnManager.instance;
                float Increase = (IsCorrect()) ? 100 : -100;
                float Subtract = (LM.ShouldPunish(GuessStreak)) ? -1000 : 0;
                if (LM.ShouldPunish(GuessStreak))
                    GuessStreak = 0;
                Guesses.Add(Guess);
                float MotionMultiplier = LM.ShouldRewardMultiplier ? LM.MotionRewardMultiplier[(int)TrueMotion] : 1;
                float ShouldRewardOnFalse = (TrueMotion == CurrentLearn.Nothing && LM.RewardOnFalse == false) ? 0 : 1;
                float HighestMultiplier = (LearnManager.instance.MultiplyByHighestGuess) ? (WeightedGuesses[GetHighest(out bool Conflict)] / WeightOutputMultiplier) : 1;
                return (Increase + Subtract) * ShouldRewardOnFalse * MotionMultiplier * HighestMultiplier;
            }
            bool IsCorrect()
            {
                bool NoConflict = true;
                bool CorrectGuess = Guess == TrueMotion;
                return NoConflict && CorrectGuess;
            }
            void SetVariablesPublic()
            {
                CurrentGuess = Guess;
                RealMotion = TrueMotion;
                Reward = FitnessIncrease();
                handToChange.material = FalseTrue[Convert.ToInt32(IsCorrect())];
                Iscorrect = IsCorrect();
            }
            int GetHighest(out bool ConflictingGuesses)
            {
                float Highest = 0;
                int index = 0;
                for (int i = 0; i < outputSignalArray.Length; i++)
                {
                    WeightedGuesses[i] = (float)outputSignalArray[i] * WeightOutputMultiplier;
                    if (WeightedGuesses[i] > Highest)
                    {
                        Highest = WeightedGuesses[i];
                        index = i;
                    }
                }
                List<float> SortList = new List<float>(WeightedGuesses);
                SortList.Sort();
                SortList.Reverse();
                ConflictingGuesses = (SortList[0] == SortList[1]) ? true : false;

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
            for (int i = 0; i < LearnManager.instance.FramesToFeedAI; i++)
            {
                int CurrentFrame = Frame - i;
                SingleInfo Info = (LM.state == learningState.Learning) ? CurrentMotion().Infos[CurrentFrame] : LM.PastFrame(side, CurrentFrame);
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

            //TotalFrameTest +=

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
        //override v
        #endregion
        void ChangeSameGuessStreak(CurrentLearn Guess)
        {
            if (Guess == lastGuess)
            {
                GuessStreak += 1;
                //GuessStreak1
            }
            else
            {
                GuessStreak = 0;
            }


            lastGuess = Guess;
            /*
            if (Outcome == true && Streak >= 0)
                Streak += 1;
            else if(Outcome == false && Streak <= 0)
                Streak -= 1;

            if (Outcome == true && Streak < 0)
                Streak = 1;
            else if(Outcome == false && Streak > 0)
                Streak = -1;
            //Debug.Log("Reward Guess: " + GotRight + "  Works: " + ListWorks + "  Reward: " + LearnManager.instance.motions.GetReward(GotRight, ListWorks));
            */
        }

        public Motion CurrentMotion() { return LearnManager.instance.MovementList[MotionIndex].Motions[Set];}

        public void CustomDebug(string text)
        {
            if (DebugType == DebugType.None)
                return;
            string FrameReference = "";
            if (DebugType == DebugType.WithState)
                FrameReference = "|Frame: " + Frame + "|Set: " + Set + "" + "|";
            Debug.Log(text + FrameReference);
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
