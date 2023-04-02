using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Sirenix.OdinInspector;
using RestrictionSystem;
using System.Linq;


public struct LockStorage
{
    public string Name;
    public Vector2 Lock;
}
namespace RestrictionSystem
{
    public class MotionAssign : SerializedMonoBehaviour
    {
        [FoldoutGroup("References")] public LearnManager LM;
        [FoldoutGroup("References")] public BruteForce BF;

        public bool AbleToCallWithButtons;
        public int2 WithinFrames;
        public bool ShouldLockAll;
        
        [FoldoutGroup("Lock")] public SingleRestriction RestrictionToLock;
        [FoldoutGroup("Lock")] public LockStorage CurrentLock;
        [FoldoutGroup("Lock")] public List<LockStorage> LockStorages;

        [FoldoutGroup("LockAngle")] public float2 DegreesFromHead;
        [FoldoutGroup("LockAngle")] public KeyCode LockButton;



        [FoldoutGroup("AllTrueMotions")] public List<List<Vector2>> TrueMotions;
        [FoldoutGroup("AllTrueMotions")] public CurrentLearn TrueMotionEdit;
        [FoldoutGroup("AllTrueMotions"), Button(ButtonSizes.Small)]
        public void GetTrueMotions()
        {
            List<bool> MotionStates = new List<bool>();
            for (int i = 0; i < LM.MovementList[(int)TrueMotionEdit].Motions.Count; i++)
                MotionStates.Add(Enumerable.Range(0, LM.MovementList[(int)TrueMotionEdit].Motions[i].Infos.Count).Any(x => LM.MovementList[(int)TrueMotionEdit].Motions[i].AtFrameState(x)));
            List<Vector2> Range = Motion.ConvertToRange(MotionStates);
            TrueMotions[(int)TrueMotionEdit - 1] = Range;
        }
        
        [FoldoutGroup("Lock"), Button(ButtonSizes.Small)]
        public void PreformLock()
        {
            int CurrentMotionEdit = GetComponent<MotionEditor>().MotionNum;
            int CurrentSpellEdit = (int)GetComponent<MotionEditor>().MotionType;

            List<bool> Frames = new List<bool>();

            List<int> ToPreformOn = ShouldLockAll ? Enumerable.Range(0, LM.MovementList[CurrentSpellEdit].Motions.Count).Where(x => InsideValues(x)).ToList() : new List<int> { CurrentMotionEdit };

            bool IsVelocityRelated = RestrictionToLock.restriction == Restriction.VelocityInDirection || RestrictionToLock.restriction == Restriction.VelocityThreshold;
            for (int m = 0; m < ToPreformOn.Count; m++)
            {
                for (int i = IsVelocityRelated ? 1 : 0; i < LM.MovementList[CurrentSpellEdit].Motions[ToPreformOn[m]].Infos.Count; i++)//frame
                {
                    SingleInfo FirstFrame = IsVelocityRelated ? LM.MovementList[CurrentSpellEdit].GetRestrictionInfoAtIndex(ToPreformOn[m], i - 1) : null;
                    SingleInfo SecondFrame = LM.MovementList[CurrentSpellEdit].GetRestrictionInfoAtIndex(ToPreformOn[m], i);
                    float OutputValue = RestrictionManager.RestrictionDictionary[RestrictionToLock.restriction].Invoke(RestrictionToLock, FirstFrame, SecondFrame);
                    bool Works = OutputValue > CurrentLock.Lock.x && OutputValue < CurrentLock.Lock.y;
                    Frames.Add(Works);
                }

                LM.MovementList[CurrentSpellEdit].Motions[ToPreformOn[m]].SetRanges(Frames);
            }

            bool InsideValues(int Try) { return TrueMotions[GetComponent<MotionEditor>().MotionNum].Any(Val => Try >= Val.x && Try <= Val.y); }
        }

        // Update is called once per frame
        void Update()
        {
            //degree assign
            if (!AbleToCallWithButtons)
                return;
            if (Input.GetKeyDown(LockButton))
                PreformLock();


            PastFrameRecorder PR = PastFrameRecorder.instance;
            if (PR.RightInfo.Count < PR.MaxStoreInfo - 1)
                return;
            //270 on right, 90 on left
            //Value = RestrictionManager.RestrictionDictionary[Restriction.HandToHeadAngle].Invoke(null, PR.PastFrame(Side.right), PastFrameRecorder.instance.GetControllerInfo(Side.right));
        }
    }
}

