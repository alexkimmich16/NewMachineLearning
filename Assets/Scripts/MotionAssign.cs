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
        [System.Serializable]
        public class RestrictionStorage
        {
            public SingleRestriction Restriction;
            public Vector2 Lock;
        }
        public struct LockStorage
        {
            public string Name;
            public Vector2 Lock;
        }

        [FoldoutGroup("References")] public LearnManager LM;
        [FoldoutGroup("References")] public BruteForce BF;

        public bool AbleToCallWithButtons;
        
        public bool ShouldLockAll;
        [FoldoutGroup("Lock")] public bool RestrictFrameLength = false;
        [FoldoutGroup("Lock"), ShowIf("RestrictFrameLength")] public int2 WithinFrames;
        [FoldoutGroup("Lock")] public List<RestrictionStorage> Restrictions;
        [FoldoutGroup("Storage")] public List<LockStorage> LockStorages;

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
            List<int> ToPreformOn = ShouldLockAll ? Enumerable.Range(0, LM.MovementList[CurrentSpellEdit].Motions.Count).Where(x => InsideValues(x)).ToList() : new List<int> { CurrentMotionEdit };
            
            Debug.Log(ToPreformOn.Count);
            for (int m = 0; m < ToPreformOn.Count; m++)//ALL TO PREFORM ON
            {
                List<bool> WorkingFrames = Enumerable.Repeat(true, LM.MovementList[CurrentSpellEdit].Motions[ToPreformOn[m]].Infos.Count).ToList();//ALL RESTRICTIONS
                for (int n = 0; n < Restrictions.Count; n++)
                {
                    bool IsVelocityRelated = Restrictions[n].Restriction.restriction == Restriction.VelocityInDirection || Restrictions[n].Restriction.restriction == Restriction.VelocityThreshold;
                    for (int i = IsVelocityRelated ? 1 : 0; i < LM.MovementList[CurrentSpellEdit].Motions[ToPreformOn[m]].Infos.Count; i++)//ALL FRAMES
                    {
                        AllMotions allMotions = LM.MovementList[CurrentSpellEdit];
                        float OutputValue = RestrictionManager.RestrictionDictionary[Restrictions[n].Restriction.restriction].Invoke(Restrictions[n].Restriction, IsVelocityRelated ? allMotions.GetRestrictionInfoAtIndex(ToPreformOn[m], i - 1) : null, allMotions.GetRestrictionInfoAtIndex(ToPreformOn[m], i));
                        if((OutputValue >= Restrictions[n].Lock.x && OutputValue <= Restrictions[n].Lock.y && InsideFrameLength(i)) == false)
                            WorkingFrames[i] = false;
                    }

                }


                    
                //Debug.Log("Index: " + ToPreformOn[m] + "  Frames: " + ToPreformOn);
                LM.MovementList[CurrentSpellEdit].Motions[ToPreformOn[m]].SetRanges(WorkingFrames);
            }

            bool InsideValues(int Try) { return TrueMotions[(int)GetComponent<MotionEditor>().MotionType - 1].Any(Val => Try >= Val.x && Try <= Val.y); }
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

