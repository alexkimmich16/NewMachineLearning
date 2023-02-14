using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System.Linq;
using Unity.Mathematics;
namespace RestrictionSystem
{
    public delegate void CompletionUpdate(float Percent, int ETAInSeconds);

    public delegate bool RestrictionCheck(List<SingleRestriction> CheckRestrictions);
    public class AutoPickExtension : SerializedMonoBehaviour
    {
        public static CompletionUpdate OnCompletionUpdate;

        [FoldoutGroup("Stats")] public AllChanges AllRestrictionLocks; // each represents a value
        
        public static List<int> GetValuesWithMax(int Value, List<int> Maxes)
        {
            long LeftCount = Value;
            List<int> Output = new List<int>();
            for (int i = 0; i < Maxes.Count; i++)
            {
                Output.Add(Mathf.FloorToInt(LeftCount / Maxes[i]));
                LeftCount -= Mathf.FloorToInt(LeftCount / Maxes[i]) * Maxes[i];
            }
            return Output;
        }
        public static List<int> Seperate(List<int> ToSeperate)
        {
            List<int> Output = new List<int>();
            for (int i = 0; i < ToSeperate.Count; i++)
                for (int j = 0; j < ToSeperate[i]; j++)
                    Output.Add(i);
            return Output;
        }
        public static List<int> ReCombine(List<int> ToUnSeperate)
        {
            List<int> Output = new List<int>();
            for (int i = 0; i < System.Enum.GetValues(typeof(Restriction)).Length; i++)
                Output.Add(0);

            for (int i = 0; i < ToUnSeperate.Count; i++)
                Output[ToUnSeperate[i]] += 1;

            return Output;
        }
        public static List<int> ByType = new List<int>() { 3, 6, 2, 2, 6, 3};
        public static Dictionary<ValueLockType, VariableType> RestrictionValueTypes = new Dictionary<ValueLockType, VariableType>(){
            {ValueLockType.checkType, VariableType.CheckType},
            {ValueLockType.OtherDirection, VariableType.Vector3},
            {ValueLockType.UseLocalHandPos, VariableType.Bool},
            {ValueLockType.UseLocalHandRot, VariableType.Bool},
            {ValueLockType.Direction, VariableType.Vector3},
            {ValueLockType.UseAxisList, VariableType.AxisList},
        };

        [FoldoutGroup("Stats"), ListDrawerSettings(Expanded = false, ShowIndexLabels = true)] public List<RestrictionVariableLock> RestrictionVariableLocks;
        [FoldoutGroup("Stats"), ListDrawerSettings(Expanded = false, ShowIndexLabels = true)] public List<RestrictionVariableLock> QualityRestrictionVariableLocks;


        [FoldoutGroup("Stats")] public int MaxOfIndividualRestrictions = 2;
        [FoldoutGroup("Stats")] public int MaxTotalRestrictions = 5;
        [FoldoutGroup("Stats")] public int MinTotalRestrictions = 2;
        [FoldoutGroup("Stats")] public bool AddToEachTypeTotalIfZero = true;

        [FoldoutGroup("Time")] public List<float> RestrictionTimeTake = new List<float>() { 1f,2f,4f,8f,16f};
        [FoldoutGroup("Time")] private float StartTime;
        [FoldoutGroup("Time"), ReadOnly] public float TotalSeconds;
        [FoldoutGroup("Time"), ReadOnly] public float SecondsLeft;
        [FoldoutGroup("Time"), ReadOnly] public float TimeDoneSoFar;


        [FoldoutGroup("RuntimeValues")] public long TotalRequiredRuns;
        [FoldoutGroup("RuntimeValues")] public int RestrictionTypeIndex;
        [FoldoutGroup("RuntimeValues"), ReadOnly] public int CurrentDone;
        [FoldoutGroup("RuntimeValues"), ReadOnly] public int IndexFromValues;

        [FoldoutGroup("IndexToRestrictionTest")] public List<int> EachRestrictionFrames;
        [FoldoutGroup("IndexToRestrictionTest")] public List<int> EachTotal;
        [FoldoutGroup("IndexToRestrictionTest")] public List<float> EachTime;
        [FoldoutGroup("IndexToRestrictionTest")] public List<int> RestrictionTypes;
        [FoldoutGroup("IndexToRestrictionTest")] public List<int> RestrictionValues;
        
        [FoldoutGroup("IndexToRestrictionTest")] public List<List<int>> ExampleNums;
        //[FoldoutGroup("IndexToRestrictionTest")] public List<SingleRestriction> IndexRestricions;


        [FoldoutGroup("Highest"), ReadOnly] public float Highest;
        [FoldoutGroup("Highest"), ReadOnly] public List<int> HighestValueCombination;
        [FoldoutGroup("Highest"), ReadOnly] public List<int> HighestTypeCombination;


        ///run large scan, than accurate scan for returned index


        public int ETASeconds(float PercentDone) { return Mathf.RoundToInt(((100f / PercentDone) * (Time.timeSinceLevelLoad - StartTime)) - (Time.timeSinceLevelLoad - StartTime)); }
        [FoldoutGroup("Buttons"), Button(ButtonSizes.Small)] public void RunTest() { StartCoroutine(AutoPickRunning()); }
        [FoldoutGroup("Buttons"), Button(ButtonSizes.Small)] public void GetAllStats()
        {
            RestrictionTypeIndex = 1;
            RestrictionTypes = Seperate(GetValuesWithMax(RestrictionTypeIndex, ValuesOfMax()));

            RestrictionValues.Clear();                //reset values
            for (int i = 0; i < RestrictionTypes.Count; i++)
                RestrictionValues.Add(0);
            
            EachRestrictionFrames = GetEachRestrictionFrames();
            TotalRequiredRuns = GetTotalRequiredRuns();

            
            TotalSeconds = EachTime.Aggregate((a, b) => a + b);
        }

        public int GetIndexFromValues()
        {
            if (EachRestrictionFrames.Count <= 1)
                return 0;
            int Multiplier = 1;
            int Total = EachRestrictionFrames[RestrictionTypes[0]];
            if(RestrictionValues.Count >= 2)
            {
                for (int i = 1; i < RestrictionValues.Count; i++)
                {
                    Multiplier = Multiplier * EachRestrictionFrames[RestrictionTypes[i - 1]];
                    Total += Multiplier * RestrictionValues[i];
                }
            }
                
            return Total;
        }
        public float PercentDone()
        {
            float UndoneRestrictionSeconds = TimeDoneSoFar;
            float CurrentRestrictionSeconds = ((float)GetIndexFromValues() / (float)EachTotal[CurrentDone]) * EachTime[CurrentDone];
            return (UndoneRestrictionSeconds + CurrentRestrictionSeconds) / TotalSeconds;
            //float 
            //((float)(CurrentDone + GetIndexFromValues()) / (float)TotalRequiredRuns) * 100f
        }
        private void Update()
        {
            if(EachTotal.Count > 0)
                OnCompletionUpdate?.Invoke(PercentDone(), ETASeconds(PercentDone()));
        }
        public AllChanges GetCurrentChanges(List<SingleRestriction> Restrictions) { return new AllChanges("Test1", Restrictions.Select(R => AllRestrictionLocks.Restrictions[(int)BruteForce.instance.motionGet]).ToList()); }
        public List<SingleRestriction> GetSingleRestrictions(List<int> RestrictionTypes, List<int> RestrictionValues) { return Enumerable.Range(0, RestrictionTypes.Count).Select(t => SingleRestrictionAtIndex((Restriction)RestrictionTypes[t], RestrictionValues[t])).ToList(); }

        private List<int> GetEachRestrictionFrames() //ABSOLUTE WORKING
        {
            List<int> Output = new List<int>();
            for (int i = 0; i < System.Enum.GetValues(typeof(Restriction)).Length; i++) // 5
            {
                List<int> Working = new List<int>();
                for (int j = 0; j < ByType.Count; j++)
                {
                    if (DependantVariableList((Restriction)i)[j] == true && !RestrictionVariableLocks[i].VariableLocks.Any(V => V.changeType == ChangeType.LockVariable && (int)V.ValueToLock == j))
                    {
                        Working.Add(ByType[j]);
                    }
                }
                Output.Add(Working.Count > 0 ? Working.Aggregate((a, b) => a * b) : 1);
            }
            return Output;
        }
        public int GetTotalRequiredRuns() //PROblem
        {
            EachTotal = new List<int>();
            ExampleNums = new List<List<int>>();
            for (int i = 0; i < Mathf.Pow(MaxOfIndividualRestrictions + 1, System.Enum.GetValues(typeof(Restriction)).Length); i++)
            {
                List<int> SeperatedRestrictionTypes = Seperate(GetValuesWithMax(i, ValuesOfMax()));
                bool Works = RestrictionTypeWorks(SeperatedRestrictionTypes);
                if (Works)
                {
                    ExampleNums.Add(SeperatedRestrictionTypes);
                }
                int ToAdd = Works ? GetPossibleCombinationCount(SeperatedRestrictionTypes) : 0;

                if((AddToEachTypeTotalIfZero && ToAdd == 0) || ToAdd != 0)
                {
                    EachTotal.Add(ToAdd);
                    float WillRunTime = ToAdd != 0 ? (1 / RestrictionTimeTake[SeperatedRestrictionTypes.Count]) * ToAdd : 0;
                    EachTime.Add(WillRunTime);
                }
            }

            return EachTotal.Aggregate((a, b) => a + b);


            //SeperatedRestrictionTypes.Aggregate((a, b) => a * EachRestrictionFrames[b])
        }

        public int GetPossibleCombinationCount(List<int> SeperatedRestrictionTypes)
        {
            int MaxPossibleCombinations = 1;
            for (int i = 0; i < SeperatedRestrictionTypes.Count; i++)
                MaxPossibleCombinations = MaxPossibleCombinations * EachRestrictionFrames[SeperatedRestrictionTypes[i]];

            int FoundPossibleCombinations = 0;
            List<int> Values = new List<int>();
            for (int i = 0; i < SeperatedRestrictionTypes.Count; i++)
                Values.Add(0);

            for (int i = 0; i < MaxPossibleCombinations; i++)
            {
                for (int j = 0; j < SeperatedRestrictionTypes.Count; j++)
                {
                    if (Works(Values))
                        FoundPossibleCombinations += 1;


                    if (Values[j] == EachRestrictionFrames[SeperatedRestrictionTypes[j]])
                        Values[j] = 0;
                    else
                        Values[j] += 1;
                }
                
            }
            bool Works(List<int> Values)
            {
                for (int i = 0; i < SeperatedRestrictionTypes.Count; i++)
                {
                    int RestrictionToCheck = SeperatedRestrictionTypes[i];
                    if (RestrictionVariableLocks[RestrictionToCheck].VariableLocks.Count == 0)
                        continue;

                    for (int j = 0; j < RestrictionVariableLocks[RestrictionToCheck].VariableLocks.Count; j++)
                    {
                        VariableLock Lock = RestrictionVariableLocks[RestrictionToCheck].VariableLocks[j];
                        if (Lock.changeType == ChangeType.LockVariable && Values[RestrictionToCheck] != Lock.RestrictValue) //should lock and not equal
                        {
                            return false;
                        }
                    }
                }
                return true;
                    //Lock.changeType != ChangeType.LockVariable || IndexRestricions.Where(t => t.restriction == (Restriction)i).All(r => NumberConverter.GetIndexOfValue(NumberConverter.GetRestrictionVar(r, Lock.ValueToLock)) == Lock.RestrictValue);
            }
            //Debug.Log(FoundPossibleCombinations);
            return FoundPossibleCombinations;
        }
        public bool RestrictionTypeWorks(List<int> SeperatedRestrictionTypes)
        {
            List<int> UnSeperatedRestrictions = ReCombine(SeperatedRestrictionTypes);

            bool LowerThanMaxTotal = SeperatedRestrictionTypes.Count <= MaxTotalRestrictions;
            bool LowerThanMaxOverlap = !UnSeperatedRestrictions.Any(x => x > MaxOfIndividualRestrictions);
            bool LargerThanMin = SeperatedRestrictionTypes.Count >= MinTotalRestrictions;
            //Debug.Log(LowerThanMaxTotal + " " + LowerThanMaxOverlap + " " + LargerThanMin);
            if ((LowerThanMaxTotal && LowerThanMaxOverlap && LargerThanMin) == false)
                return false;
            //Debug.Log("passed1");
            for (int i = 0; i < UnSeperatedRestrictions.Count; i++) //all the restriction locks
                if (RestrictionVariableLocks[i].VariableLocks.Count > 0)
                    for (int j = 0; j < RestrictionVariableLocks[i].VariableLocks.Count; j++)//all the restrictions inside each restriction lock
                    {
                        VariableLock Lock = RestrictionVariableLocks[i].VariableLocks[j];

                        bool OutsideCount = (UnSeperatedRestrictions[i] < Lock.UseAmountMinMax.x || UnSeperatedRestrictions[i] > Lock.UseAmountMinMax.y) && Lock.changeType == ChangeType.LockUseAmount;
                        //Debug.Log("j: " + j + "1: " + WithinCount + "  2: " + VariableLocked);

                        if (OutsideCount)
                            return false;
                    }

            //Debug.Log("passed2");
            return true;
        }
        
        
        SingleRestriction SingleRestrictionAtIndex(Restriction restriction, int index)
        {
            List<int> FinalStats = GetOutputList(index, GetMiddleStats(DependantVariableList(restriction)));

            SingleRestriction ReturnRestriciton = new SingleRestriction();
            ReturnRestriciton.checkType = NumberConverter.GetValueOfIndex(FinalStats[0], VariableType.CheckType).CheckTypeStore;
            ReturnRestriciton.OtherDirection = NumberConverter.GetValueOfIndex(FinalStats[1], VariableType.Vector3).Vector3Store;
            ReturnRestriciton.UseLocalHandPos = NumberConverter.GetValueOfIndex(FinalStats[2], VariableType.Bool).BoolStore;
            ReturnRestriciton.UseLocalHandRot = NumberConverter.GetValueOfIndex(FinalStats[3], VariableType.Bool).BoolStore;
            ReturnRestriciton.Direction = NumberConverter.GetValueOfIndex(FinalStats[4], VariableType.Vector3).Vector3Store;
            ReturnRestriciton.UseAxisList = NumberConverter.GetValueOfIndex(FinalStats[5], VariableType.AxisList).AxisStore;
            ReturnRestriciton.restriction = restriction;

            return ReturnRestriciton;
        }
        private List<long> GetMiddleStats(List<bool> WorkingList)
        {
            List<long> Output = new List<long>();
            for (int i = 0; i < WorkingList.Count; i++)
                Output.Add(1);

            for (int i = 0; i < WorkingList.Count; i++)
                for (int j = 0; j < i; j++)
                    if(WorkingList[i])
                        Output[j] = (long)(Output[j] * (ByType[i]));
            return Output;
        }
        private List<int> GetOutputList(long Total, List<long> MiddleStepCounts)
        {
            List<int> Output = new List<int>();
            long LeftCount = Total;
            for (int i = 0; i < MiddleStepCounts.Count; i++)
            {
                if(i == 1 && Output[0] != 2)
                {
                    Output.Add(0);
                }
                else
                {
                    Output.Add(Mathf.FloorToInt(LeftCount / MiddleStepCounts[i]));
                    LeftCount -= Mathf.FloorToInt(LeftCount / MiddleStepCounts[i]) * MiddleStepCounts[i];
                }
                
            }
            return Output;
        }
        public List<bool> DependantVariableList(Restriction restriction)
        {
            List<bool> Variables = new List<bool>();

            Variables.Add(SingleRestriction.CheckTypeRestrictions.Contains(restriction));
            Variables.Add(SingleRestriction.CheckTypeRestrictions.Contains(restriction));
            Variables.Add(SingleRestriction.LocalHandPosRestrictions.Contains(restriction));
            Variables.Add(SingleRestriction.LocalHandRotRestrictions.Contains(restriction));
            Variables.Add(SingleRestriction.RequiresOffsetRestrictions.Contains(restriction));
            Variables.Add(SingleRestriction.AxisListRestrictions.Contains(restriction));

            return Variables;
        }
        IEnumerator AutoPickRunning()
        {
            bool Running = true;
            StartTime = Time.timeSinceLevelLoad;
            GetAllStats();

            void Conclude()
            {
                Debug.Log("DONE!!");
                Running = false;
                CurrentDone = (int)TotalRequiredRuns;
                RestrictionValues.Clear();
                RestrictionTypes.Clear();
                EachRestrictionFrames.Clear();
                EachTotal.Clear();
                //StopCoroutine(AutoPickRunning());
            }
            while (Running)
            {
                while (!RestrictionTypeWorks(RestrictionTypes))
                {
                    NextValue();
                    if (RestrictionTypeIndex >= EachTotal.Count)
                    {
                        Conclude();
                        break;
                    }
                }

                BruteForce.instance.DoBruteForceTest(new MotionRestriction("Testing1", GetSingleRestrictions(RestrictionTypes, RestrictionValues)), GetCurrentChanges(GetSingleRestrictions(RestrictionTypes, RestrictionValues)), out float Value);

                if(Value > Highest)
                {
                    HighestValueCombination = new List<int>(RestrictionValues);
                    HighestTypeCombination = new List<int>(RestrictionTypes);
                    Highest = Value;
                }

                yield return new WaitForEndOfFrame();
                //CurrentDone = (Enumerable.Range(0, RestrictionTypeIndex).Aggregate((a, b) => a + EachTotal[b]));

                ///plus current step values
                

                NextValue();
                void NextValue()
                {
                    IndexFromValues = GetIndexFromValues();
                    for (int i = 0; i < RestrictionValues.Count; i++) //hit max
                    {
                        if (RestrictionValues[i] == EachRestrictionFrames[RestrictionTypes[i]])
                            RestrictionValues[i] = 0;
                        else
                        {
                            RestrictionValues[i] += 1;
                            return;
                        }
                    }
                    NextRestriction();
                }
                void NextRestriction()
                {
                    bool Called = false;
                    while (EachTotal[RestrictionTypeIndex] == 0 || Called == false)
                    {
                        Called = true;
                        TimeDoneSoFar += EachTime[RestrictionTypeIndex];
                        CurrentDone += EachTotal[RestrictionTypeIndex];
                        RestrictionTypeIndex += 1;
                        
                        if (RestrictionTypeIndex >= EachTotal.Count)
                        {
                            Conclude();
                            return;
                        }
                        /// restirction AND value done**


                        RestrictionTypes = Seperate(GetValuesWithMax(RestrictionTypeIndex, ValuesOfMax()));
                    }

                    RestrictionValues.Clear();                //reset values
                    for (int i = 0; i < RestrictionTypes.Count; i++)
                        RestrictionValues.Add(0);
                }
            }
            

            ///run accurate check
        }
        List<int> ValuesOfMax()
        {
            List<int> NewList = new List<int>();
            int Last = 1;
            NewList.Add(Last);

            for (int i = 1; i < System.Enum.GetValues(typeof(Restriction)).Length; i++)
            {
                Last = Last * (MaxOfIndividualRestrictions + 1);
                NewList.Add(Last);
            }
            NewList.Reverse();
            return NewList;
        }

    }

    [System.Serializable]
    public struct VariableLock
    {
        ///can include: requiring at least one, disallowing motion type, locking variables
        public ChangeType changeType;

        [ShowIf("changeType", ChangeType.LockUseAmount)] public int2 UseAmountMinMax;
        [ShowIf("changeType", ChangeType.LockVariable)] public ValueLockType ValueToLock;
        [ShowIf("changeType", ChangeType.LockVariable)] public int RestrictValue;
    }

    [System.Serializable]
    public struct RestrictionVariableLock
    {
        public Restriction restriction;
        public List<VariableLock> VariableLocks;
    }

    public enum ChangeType
    {
        LockUseAmount = 0,
        LockVariable = 2,
    }
}
