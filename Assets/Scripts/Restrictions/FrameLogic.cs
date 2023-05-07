using System.Collections.Generic;
using System;
using System.Linq;
using Sirenix.OdinInspector;
namespace RestrictionSystem
{
    [System.Serializable]
    public class SideInfo
    {
        [ListDrawerSettings(ShowIndexLabels = true)] public List<MotionInfo> Motions;
        public SideInfo()
        {
            Motions = new List<MotionInfo>();
            for (int i = 0; i < Enum.GetValues(typeof(CurrentLearn)).Length - 1; i++)
                Motions.Add(new MotionInfo());
        }
    }
    [System.Serializable]
    public class MotionInfo
    {
        [FoldoutGroup("Output")] public bool Works;
        [FoldoutGroup("Output")] public float ReturnValue;
        [FoldoutGroup("Input")] public List<bool> Frames;
        [FoldoutGroup("Input")] public List<float> Times;
        public MotionInfo()
        {
            Frames = new List<bool>() { false };
            Times = new List<float>() { 0 };
        }
        public void AddFrame(bool State, float Time)
        {
            Frames.Add(State);
            Times.Add(Time);
        }
        public void RemoveFrame()
        {
            Frames.RemoveAt(0);
            Times.RemoveAt(0);
        }
        public bool GetRecent() { return Frames[^1]; }
    }
    public class FrameLogic : SerializedMonoBehaviour
    {
        public static FrameLogic instance;
        private void Awake() { instance = this; }
        public List<SideInfo> PastMotionFrames; //side, motion, frames

        //[FoldoutGroup("Test")] public List<float> TestValues;
        //[FoldoutGroup("Test")] public List<bool> TestStates;

        public MotionSettings MotionSettings;

        //[FoldoutGroup("Output")] public List<float> Weights;
        public MotionInfo GetMotionInfo(Side side, CurrentLearn motion) { return PastMotionFrames[(int)side].Motions[(int)motion - 1]; }
        void Start()
        {
            //Initialize Frames
            PastMotionFrames = new List<SideInfo>();
            for (int i = 0; i < Enum.GetValues(typeof(Side)).Length; i++)
                PastMotionFrames.Add(new SideInfo());
        }
        public void InputRawMotionState(Side side, CurrentLearn Motion, bool State, float Time)
        {
            MotionInfo info = GetMotionInfo(side, Motion);
            info.AddFrame(State, Time);
            if (info.Times.Sum() > MotionSettings.LogicInfo[(int)Motion - 1].MostTime)
                info.RemoveFrame();
        }

        public bool Calculate(Side side, CurrentLearn Motion)
        {
            //*sequence matters*

            FrameLogicInfo logic = MotionSettings.LogicInfo[(int)Motion - 1];
            if (logic.FrameLogicEnabled)
                return GetMotionInfo(side, Motion).GetRecent();

            MotionInfo info = GetMotionInfo(side, Motion);

            List<float> Adjusted = info.Times.Select(x => (info.Times.Sum() / x) + logic.Spread).ToList();
            List<float> RealTime = Adjusted.Select(x => x / Adjusted.Sum()).ToList();
            //Weights = RealWeight;

            List<float> RealStates = info.Frames.Select(x => x ? 1f : 0f).ToList();
            List<float> FinalWeights = RealTime.Zip(RealStates, (x, y) => x * y).ToList();

            info.ReturnValue = FinalWeights.Sum();
            info.Works = FinalWeights.Sum() > logic.CutoffValue;
            //Debug.Log(FrameTimeTotal / TestValues[0]);

            return info.Works;
        }
    }
}

