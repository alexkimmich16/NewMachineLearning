using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Sirenix.OdinInspector;
using RestrictionSystem;
using System.Linq;

namespace RestrictionSystem
{
    public class MotionAssign : SerializedMonoBehaviour
    {
        public static MotionAssign instance;
        private void Awake() { instance = this; }
        [System.Serializable]
        public struct MotionContainer
        {
            [ListDrawerSettings(Expanded = false, ShowIndexLabels = true)] public List<RestrictionStorage> Restrictions;
            [System.Serializable]
            public struct RestrictionStorage
            {
                public string Motion;
                public List<LockedRestriction> Restrictions;
                [System.Serializable]
                public struct LockedRestriction
                {
                    //[GUIColor("Orange")] 
                    public SingleRestriction Restriction;
                    public Vector2 Lock;
                }
            }
        }

        [FoldoutGroup("References")] public LearnManager LM;
        [FoldoutGroup("References")] public BruteForce BF;

        public bool AbleToCallWithButtons;
        public bool ShouldLockAll;
        public bool ShouldStitch;



        [FoldoutGroup("Lock")] public bool RestrictFrameLength = false;
        [FoldoutGroup("Lock"), ShowIf("RestrictFrameLength")] public int2 WithinFrames;
        [FoldoutGroup("Lock")] public MotionContainer Restrictions;

        [FoldoutGroup("Lock")] public KeyCode LockButton;

        public bool InsideFrameLength(int Frame) { return RestrictFrameLength ? Frame >= WithinFrames.x && Frame <= WithinFrames.y : true; }

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
            List<int> ToPreformOn = ShouldLockAll ? Enumerable.Range(0, LM.MovementList[CurrentSpellEdit].Motions.Count).Where(x => InsideTrueMotions(x, CurrentSpellEdit - 1)).ToList() : new List<int> { CurrentMotionEdit };
            
            Debug.Log(ToPreformOn.Count);
            for (int m = 0; m < ToPreformOn.Count; m++)//ALL TO PREFORM ON
            {
                List<bool> WorkingFrames = Enumerable.Repeat(true, LM.MovementList[CurrentSpellEdit].Motions[ToPreformOn[m]].Infos.Count).ToList();//ALL RESTRICTIONS
                for (int n = 0; n < Restrictions.Restrictions[CurrentSpellEdit - 1].Restrictions.Count; n++)
                {
                    bool IsVelocityRelated = Restrictions.Restrictions[CurrentSpellEdit - 1].Restrictions[n].Restriction.restriction == Restriction.VelocityInDirection || Restrictions.Restrictions[CurrentSpellEdit - 1].Restrictions[n].Restriction.restriction == Restriction.VelocityThreshold;
                    for (int i = IsVelocityRelated ? 1 : 0; i < LM.MovementList[CurrentSpellEdit].Motions[ToPreformOn[m]].Infos.Count; i++)//ALL FRAMES
                    {
                        AllMotions allMotions = LM.MovementList[CurrentSpellEdit];
                        float OutputValue = RestrictionManager.RestrictionDictionary[Restrictions.Restrictions[CurrentSpellEdit - 1].Restrictions[n].Restriction.restriction].Invoke(Restrictions.Restrictions[CurrentSpellEdit - 1].Restrictions[n].Restriction, IsVelocityRelated ? allMotions.GetRestrictionInfoAtIndex(ToPreformOn[m], i - 1) : null, allMotions.GetRestrictionInfoAtIndex(ToPreformOn[m], i));
                        if((OutputValue >= Restrictions.Restrictions[CurrentSpellEdit - 1].Restrictions[n].Lock.x && OutputValue <= Restrictions.Restrictions[CurrentSpellEdit - 1].Restrictions[n].Lock.y && InsideFrameLength(i)) == false)
                            WorkingFrames[i] = false;
                    }

                }

                List<Vector2> WorkingRanges = Motion.ConvertToRange(WorkingFrames);
                if (ShouldStitch)
                {
                    Vector2 StitchedVector = new Vector2(WorkingRanges[0].x, WorkingRanges[WorkingRanges.Count - 1].y);
                    WorkingRanges = new List<Vector2>() { StitchedVector };
                }
                    
                //Debug.Log("Index: " + ToPreformOn[m] + "  Frames: " + ToPreformOn);
                LM.MovementList[CurrentSpellEdit].Motions[ToPreformOn[m]].SetRanges(WorkingRanges);
            }
        }
        public bool InsideTrueMotions(int Try, int MotionIndex)
        {
            if (MotionIndex < 0 || MotionIndex > TrueMotions.Count)
                return false;
            return TrueMotions[MotionIndex].Any(Vector => Try >= Vector.x && Try <= Vector.y);
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

