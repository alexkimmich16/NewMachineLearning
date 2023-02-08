using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System.Linq;
namespace RestrictionSystem
{
    public delegate bool CompletionUpdate(float Percent);

    //public delegate float RestrictionTest(SingleRestriction restriction, SingleInfo frame1, SingleInfo frame2);
    public class AutoPickExtension : SerializedMonoBehaviour
    {
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
        public static List<int> ByType = new List<int>() { 3, 27, 2, 2, 27, 8};
        public static Dictionary<RestrictionChangeValue, VariableType> RestrictionValueTypes = new Dictionary<RestrictionChangeValue, VariableType>(){
            {RestrictionChangeValue.checkType, VariableType.CheckTypeList},
            {RestrictionChangeValue.OtherDirection, VariableType.Vector3},
            {RestrictionChangeValue.UseLocalHandPos, VariableType.Bool},
            {RestrictionChangeValue.UseLocalHandRot, VariableType.Bool},
            {RestrictionChangeValue.Direction, VariableType.Vector3},
            {RestrictionChangeValue.UseAxisList, VariableType.AxisList},
        };

        //public List<RestrictionVariableLock> RestrictionVariableLocks;

        public Restriction TestingRestriction;
        public int MaxRestrictions = 2;
        [FoldoutGroup("Test")] public int PerEnumAdd;
        [FoldoutGroup("Test"), ReadOnly] public int MaxValue;

        [FoldoutGroup("GeometricTest")] public int TestValue;
        [FoldoutGroup("GeometricTest"), ReadOnly] public List<int> TestValues;


        [FoldoutGroup("IndexToRestrictionTest")] public int RestrictionTypeIndexTest;
        //[FoldoutGroup("IndexToRestrictionTest")] public int RestrictionSettingsIndexTest;
        [FoldoutGroup("IndexToRestrictionTest")] public List<int> RestrictionTypes;
        [FoldoutGroup("IndexToRestrictionTest")] public List<int> RestrictionValues;
        [FoldoutGroup("IndexToRestrictionTest"), ReadOnly] public List<int> ExampleValues;
        [FoldoutGroup("IndexToRestrictionTest")] public List<SingleRestriction> IndexRestricions;
        [FoldoutGroup("IndexToRestrictionTest"), ReadOnly] public int TotalRequiredRuns;

        [FoldoutGroup("AutoPickTesting"), Button(ButtonSizes.Small)]
        public void RunTest() { StartCoroutine(AutoPickRunning()); }


        public int GetTotalRequiredRuns()
        {
            List<int> NumberOfEachRequirement = new List<int>();
            for (int i = 0; i < System.Enum.GetValues(typeof(Restriction)).Length; i++)
                NumberOfEachRequirement.Add(0);

            //3 ^ 5;
            //all possible combinations
            for (int i = 0; i < Mathf.Pow(MaxRestrictions + 1, System.Enum.GetValues(typeof(Restriction)).Length) ;i++)
            {
                List<int> Values = GetValuesWithMax(RestrictionTypeIndexTest, ValuesOfMax());
                for (int j = 0; j < Values.Count; j++)
                    NumberOfEachRequirement[j] += Values[j];
            }
            return 1;
        }
        public void AddToValues(out bool BaseMax, out bool AbsoluteMax)
        {
            AbsoluteMax = false;
            BaseMax = false;
            for (int i = 0; i < RestrictionTypes.Count; i++) //hit max
            {
                if (RestrictionValues[i] == TotalFramesToCheck((Restriction)RestrictionTypes[i]))
                    RestrictionValues[i] = 0;
                else
                {
                    RestrictionValues[i] += 1;
                    return;
                }
            }
            BaseMax = true;
            RestrictionTypeIndexTest += 1;
            if(RestrictionTypeIndexTest == 50)
                AbsoluteMax = true;
            //move to next

        }
        public CompletionUpdate OnCompletionUpdate;

        private void Update()
        {
            MaxValue = TotalFramesToCheck(TestingRestriction);
            IndexRestricions = GenerateRestrictions(RestrictionTypes, RestrictionValues);
        }
        public int TotalFramesToCheck(Restriction restriction)
        {
            List<bool> TestingValues = DependantVariableList(restriction);
            int Total = 0;
            for (int i = 1; i < TestingValues.Count; i++)
                if(TestingValues[i])
                    Total = Total != 0 ? Total * ByType[i] : ByType[i];
            return Total;
        }
        SingleRestriction GetSingleRestrictionAtIndex(Restriction restriction, int index)
        {
            List<long> FinalStats = GetOutputList(index, GetMiddleStats(DependantVariableList(restriction)));

            SingleRestriction ReturnRestriciton = new SingleRestriction();
            ReturnRestriciton.checkType = (CheckType)FinalStats[0];
            ReturnRestriciton.OtherDirection = GetVector3Index(FinalStats[1]);
            ReturnRestriciton.UseLocalHandPos = FinalStats[2] == 1 ? true : false;
            ReturnRestriciton.UseLocalHandRot = FinalStats[3] == 1 ? true : false;
            ReturnRestriciton.Direction = GetVector3Index(FinalStats[4]);
            ReturnRestriciton.UseAxisList = GetAxisListIndex(FinalStats[5]);
            ReturnRestriciton.restriction = restriction;

            return ReturnRestriciton;

            Vector3 GetVector3Index(long index)
            {
                List<long> Possibilities = new List<long>() { 9,3,1};
                List<long> Output = new List<long>();
                long LeftCount = index;
                for (int i = 0; i < Possibilities.Count; i++)
                {
                    Output.Add((long)Mathf.Floor(LeftCount / Possibilities[i]));
                    LeftCount -= (long)Mathf.Floor(LeftCount / Possibilities[i]) * Possibilities[i];
                }
                return new Vector3(Output[0] - 1, Output[1] - 1, Output[2] - 1);
            }
            List<Axis> GetAxisListIndex(long index)
            {
                List<long> Possibilities = new List<long>() { 4, 2, 1 };
                List<long> Output = new List<long>();
                long LeftCount = index;
                for (int i = 0; i < Possibilities.Count; i++)
                {
                    Output.Add((long)Mathf.Floor(LeftCount / Possibilities[i]));
                    LeftCount -= (long)Mathf.Floor(LeftCount / Possibilities[i]) * Possibilities[i];
                }
                List<Axis> AxisList = new List<Axis>();
                for (int i = 0; i < 3; i++)
                    if (Output[i] == 1)
                        AxisList.Add((Axis)i);
                return AxisList;
            }
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
        private List<long> GetOutputList(long Total, List<long> MiddleStepCounts)
        {
            List<long> Output = new List<long>();
            long LeftCount = Total;
            for (int i = 0; i < MiddleStepCounts.Count; i++)
            {
                if(i == 1 && Output[0] != 2)
                {
                    Output.Add(0);
                }
                else
                {
                    Output.Add((long)Mathf.Floor(LeftCount / MiddleStepCounts[i]));
                    LeftCount -= (long)Mathf.Floor(LeftCount / MiddleStepCounts[i]) * MiddleStepCounts[i];
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
        
        public List<SingleRestriction> GenerateRestrictions(List<int> RestrictionTypes, List<int> RestrictionValues)
        {
            List<SingleRestriction> Restrictions = new List<SingleRestriction>();
            for (int i = 0; i < RestrictionTypes.Count; i++)
                Restrictions.Add(GetSingleRestrictionAtIndex((Restriction)RestrictionTypes[i], RestrictionValues[i]));
            return Restrictions;
        }
        IEnumerator AutoPickRunning()
        {
            RestrictionTypeIndexTest = 1;
            TotalRequiredRuns = GetTotalRequiredRuns();
            UpdateValues();

            while (true)
            {
                yield return new WaitForEndOfFrame();
                for (int i = 0; i < PerEnumAdd; i++)
                {
                    AddToValues(out bool BaseMax, out bool Max);
                    if (BaseMax)
                        UpdateValues();

                    if (Max)
                        break;
                }
                //run brute force
                //check if higher than last
                //OnCompletionUpdate(1f);
            }
            void UpdateValues()
            {
                RestrictionTypes = Seperate(GetValuesWithMax(RestrictionTypeIndexTest, ValuesOfMax()));
                RestrictionValues.Clear();
                for (int i = 0; i < RestrictionTypes.Count; i++)
                    RestrictionValues.Add(0);
            }
        }
        List<int> ValuesOfMax()
        {
            List<int> NewList = new List<int>();
            int Last = 1;
            NewList.Add(Last);

            for (int i = 1; i < System.Enum.GetValues(typeof(Restriction)).Length; i++)
            {
                Last = Last * (MaxRestrictions + 1);
                NewList.Add(Last);
            }
            NewList.Reverse();
            return NewList;
        }

    }
    //public static Dictionary<VariableType, int> VariablePossibilities = new Dictionary<VariableType, int>() { { VariableType.Vector3, 27 }, { VariableType.Bool, 2 } };

    [System.Serializable]
    public struct VariableLock
    {
        public RestrictionChangeValue ValueToChange;

        //value
    }
    [System.Serializable]
    public struct RestrictionVariableLock
    {
        public List<VariableLock> VariableLocks;
    }
    public enum VariableType
    {
        Vector3 = 0,
        Bool = 1,
        CheckTypeList = 2,
        AxisList = 2,
    }
    public enum RestrictionChangeValue
    {
        checkType = 0,
        OtherDirection = 1,
        UseLocalHandPos = 2,
        UseLocalHandRot = 3,
        Direction = 4,
        UseAxisList = 5,
    }
}
