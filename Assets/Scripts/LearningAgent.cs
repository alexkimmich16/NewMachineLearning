using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using SharpNeat.Phenomes;
using Sirenix.OdinInspector;
using RestrictionSystem;
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

        public delegate void NewFrame();
        public event NewFrame OnNewFrame;
        public static event NewFrame OnLog;

        [Header("Other")]
        public EditSide side;

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
            Fitness = 5000;
        }
        
        [Header("Output")]
        public CurrentLearn CurrentGuess;
        public CurrentLearn RealMotion;

        public int GuessStreak;
        public CurrentLearn lastGuess;

        private float OutputMultiplier = 1000f;
        public List<SingleInfo> InterpolateFrames;
        public bool CanGiveAnswer, CanRecieveInfo;
        public static int StartedUpCount;
        public static int TotalMaxAgents = 100;

        
        public float LastWorldTime;
        public float LargestTime;
        public float LastInputWaitTime;
        [Header("Debug")]
        public bool IsInterpolatingDsplay;
        public bool IsLoggerDisplay;

        private bool OnStart = true;

        

        public void StartupCountAdd()
        {
            if (StartedUpCount % 100 == 0)
                StartedUpCount = 0;
            StartedUpCount += 1;
            //Debug.Log("StartedUpCount" + StartedUpCount);
            if (StartedUpCount == TotalMaxAgents)
            {
                //Debug.Log("done");
                LearnManager.instance.StartAlgorithmSequence();
            }

        }
        
        public void OnIntervalReached()
        {
            CanGiveAnswer = true;
            CanRecieveInfo = true;
        }

        public bool Active() { return Frame != MaxFrame || IsInterpolating(); }
        public bool IsInterpolating() { return InterpolateFrames.Count > 0 && Frame == MaxFrame; }
        public bool IsLogger() { return transform == transform.parent.GetChild(0); }
        private void Start()
        {
            LearnManager.OnNewMotion += RecieveNewMotion;
            LearnManager.OnIntervalReached += OnIntervalReached;

            int sibling = 0;
            for (int i = 0; i < transform.parent.childCount; ++i)
                if (transform == transform.parent.GetChild(i))
                    sibling = i;
            transform.position = new Vector3(0,0, sibling * LearnManager.instance.SpawnGap);
            gameObject.name = "AI: " + sibling;
            int Count = LearnManager.instance.UseAgentSingleOutput ? 1 : 4;
            for (int i = 0; i < Count; ++i)
                WeightedGuesses.Add(0);
            LearnManager.instance.OnNewGen += OnNewGeneration;
            StartupCountAdd();
            //AddToStartup();
        }
        void RecieveNewMotion(int Motion, int SetStat, List<SingleInfo> InterpolateFramesStat)
        {
            LearnManager LM = LearnManager.instance;

            SentLearnManagerFinish = false;
            Set = SetStat;
            MotionIndex = Motion;
            
            //Frame = LM.FramesToFeedAI;
            Frame = 0;
            MaxFrame = LM.MovementList[Motion].Motions[SetStat].Infos.Count - 1;

            InterpolateFrames = new List<SingleInfo>(InterpolateFramesStat);
        }
        #region Overrides
        public override float GetFitness()
        {
            if (Fitness < 0)
                Fitness = 0;
            float RealFitness = Fitness;
            //Fitness = 0;
            return RealFitness;
        }
        protected override void UseBlackBoxOutpts(ISignalArray outputSignalArray)//on output
        {
            CurrentLearn Truth = TrueMotion();
            CurrentLearn Guess = (CurrentLearn)GetGuess(out Conflict);

            SetVariablesPublic();
            if (!Active())
                return;

            CustomDebug("OnActionReceived");
            if (CanGiveAnswer == false)
                return;
            CanGiveAnswer = false;
            if (IsInterpolating())
                return;
            if (IsLogger())
                OnLog();

            Reward = FitnessIncrease();
            Fitness += FitnessIncrease();

            ChangeSameGuessStreak(Guess);

            CurrentLearn TrueMotion()
            {
                if (IsInterpolating())
                    return CurrentLearn.Nothing;
                if (LearnManager.instance.AtFrameStateAlwaysTrue)
                    return (CurrentLearn)MotionIndex;

                bool IndexWorks = LearnManager.instance.MovementList[MotionIndex].Motions[Set].AtFrameState(Frame);
                CurrentLearn IndexCheckMotion = (CurrentLearn)((IndexWorks) ? MotionIndex : 0);
                return IndexCheckMotion;
            }
            float FitnessIncrease()
            {
                LearnManager LM = LearnManager.instance;
                float Increase = (IsCorrect()) ? 100 : -100;
                float Subtract = (LM.ShouldPunishStreak(GuessStreak)) ? -LM.StreakPunishAmount : 0;
                if (LM.ShouldPunishStreak(GuessStreak))
                    GuessStreak = 0;
                float MotionMultiplier = LM.UseWeightedRewardMultiplier ? LM.WeightedRewardMultiplier[(int)Truth] : 1;
                float ShouldRewardOnFalse = (Truth == CurrentLearn.Nothing && LM.RewardNothingGuess == false) ? 0 : 1;
                float HighestMultiplier = (LearnManager.instance.MultiplyByHighestGuess) ? (WeightedGuesses[GetGuess(out bool Conflict)] / OutputMultiplier) : 1;
                float RewardOnInterpolateMultiplier = (IsInterpolating() && LearnManager.instance.RewardOnInterpolation || IsInterpolating() == false) ? 1f : 0f;
                return (Increase + Subtract) * ShouldRewardOnFalse * MotionMultiplier * HighestMultiplier * RewardOnInterpolateMultiplier;
            }
            bool IsCorrect()
            {
                bool NoConflict = true;
                bool CorrectGuess = Guess == Truth;
                bool IsInterpolationSafe = IsInterpolating() && LearnManager.instance.RewardOnInterpolation || IsInterpolating() == false;
                return NoConflict && CorrectGuess && IsInterpolationSafe ;
            }
            void SetVariablesPublic()
            {
                IsInterpolatingDsplay = IsInterpolating();
                IsLoggerDisplay = IsLogger();
                CurrentGuess = Guess;
                RealMotion = Truth;
                
                handToChange.material = FalseTrue[Convert.ToInt32(IsCorrect())];

                //GetHighest(out bool Conflict);
            }
            int GetGuess(out bool ConflictingGuesses)
            {
                int index = 0;
                if (LearnManager.instance.UseAgentSingleOutput)
                {
                    ConflictingGuesses = false;
                    WeightedGuesses[0] = (float)outputSignalArray[0] * 3;
                    WeightedGuesses[0] = Mathf.RoundToInt(WeightedGuesses[0]);
                }
                else
                {
                    float Highest = 0;
                    for (int i = 0; i < outputSignalArray.Length; i++)
                    {
                        WeightedGuesses[i] = (float)outputSignalArray[i] * OutputMultiplier;
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
                }
                return index;
            }
        }
        protected override void UpdateBlackBoxInputs(ISignalArray inputSignalArray)//on Input
        {
            InitializeArrayOnStart();

            if (Time.timeSinceLevelLoad - LastInputWaitTime > LearnManager.instance.InputWaitTime)
            {
                DoInputs();
                LastInputWaitTime = Time.timeSinceLevelLoad;
            }

            if (CanRecieveInfo == true)
            {
                DoInputs();
                CanRecieveInfo = false;
            }

            //Debug.Log("Frame: " + Frame + "  MaxFrame: " + MaxFrame + "  IsInterpolating: " + IsInterpolating());
            ProgressFrames();

            if (OnNewFrame != null)
                OnNewFrame();

            if (!Active())
            {
                LearnManager.instance.AgentWaiting();
                /*
                if (IsLogger())
                {
                    if (Time.timeSinceLevelLoad - LastWorldTime > LargestTime)
                        LargestTime = (Time.timeSinceLevelLoad - LastWorldTime);
                    LastWorldTime = Time.timeSinceLevelLoad;
                }
                */
            }
            //Debug.Log("LastTime: " + (Time.timeSinceLevelLoad - LastWorldTime).ToString("F2") + "  Motion: " + (CurrentLearn)MotionIndex + "  Set: " + Set + "  Frames: " + (MaxFrame + 1));
            

            CustomDebug("CollectObservations");
            void ProgressFrames()
            {
                if (Frame < MaxFrame)
                    Frame += 1;
                else if (IsInterpolating())
                    InterpolateFrames.RemoveAt(0);
            }
            void InitializeArrayOnStart()
            {
                if (OnStart)
                {
                    OnStart = false;
                    for (int i = 0; i < LearnManager.instance.NeatSupervisor().NetworkOutputCount; i++)
                        inputSignalArray[i] = 0;
                }
            }
            void DoInputs()
            {
                if (LearnManager.instance.InputAllGrid)
                {
                    List<float> Feed = MatrixManager.instance.GridToFloats();
                    //inputSignalArray = Feed;
                    for (int i = 0; i < Feed.Count; i++)
                        inputSignalArray[i] = Feed[i];
                }
                else
                {
                    List<MatrixStat> Changes = MatrixManager.instance.MatrixChanges(true);

                    for (int i = 0; i < Changes.Count; i++)
                    {
                        int Index1 = (Changes[i].X * MatrixManager.instance.Height + Changes[i].Y) * LearnManager.instance.InfoCountInMatrixSingle();
                        //if (IsLogger())
                            //Debug.Log("Index: " + Index1 + "  Length: " + inputSignalArray.Length);
                        inputSignalArray[Index1] = Changes[i].H; 
                        inputSignalArray[Index1 + 1] = Changes[i].S;
                        inputSignalArray[Index1 + 2] = Changes[i].RotX;
                        inputSignalArray[Index1 + 3] = Changes[i].RotY;
                        inputSignalArray[Index1 + 4] = Changes[i].RotZ;
                        //MatrixManager.instance.GridStats[Changes[i].X, Changes[i].Y].y
                    }

                }
            }
            
        }
        protected override void HandleIsActiveChanged(bool newIsActive)
        {
            /*
            foreach (Transform t in transform)
            {
                t.gameObject.SetActive(newIsActive);
            }
            */
        }
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
}
