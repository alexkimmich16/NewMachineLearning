using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
namespace RestrictionSystem
{
    public delegate bool CompletionUpdate(float Percent);

    public class AutoPickExtension : SerializedMonoBehaviour
    {
        public static List<int> ByType = new List<int>() { 3, 27, 2, 2, 27, 8};

        public Restriction TestingRestriction;
        public int TestValue;
        [ReadOnly] public int MaxValue;
        public List<>
        public SingleRestriction OutputRestriction;
        

        //public CompletionUpdate OnCompletionUpdate;
        private void Update()
        {
            OutputRestriction = GetSingleRestrictionAtIndex(TestingRestriction, TestValue);
            MaxValue = TotalFramesToCheck();
        }
        public int TotalFramesToCheck()
        {
            List<bool> TestingValues = DependantVariableList(TestingRestriction);
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
        public List<SingleRestriction> GenerateRestrictions()
        {
            List<SingleRestriction> Restrictions = new List<SingleRestriction>();
            return Restrictions;
        }
        /*
         * public void StartAutoBruteForce()
        {
            StartCoroutine(AutoPickRunning());
        }
        public SingleRestriction SingleRestrictionByIndex(int Index)
        {

        }
        IEnumerator AutoPickRunning()
        {
            while (true)
            {

            }
        }
        */
    }
    //public static Dictionary<VariableType, int> VariablePossibilities = new Dictionary<VariableType, int>() { { VariableType.Vector3, 27 }, { VariableType.Bool, 2 } };

    [System.Serializable]
    public struct DefinedAutoRestriction
    {
        public AllChanges.OneRestrictionChange Stats;
        public SingleRestriction Restriction;
    }

    [System.Serializable]
    public struct VariableStore
    {
        public VariableType variableType;
        public VariableStore(VariableType type)
        {
            variableType = type;
            IntStore = 0;
        }
        public VariableStore(int Num)
        {
            variableType = VariableType.IntRange;
            IntStore = Num;
        }
        
        public int IntStore;
    }
    
    public enum VariableType
    {
        Vector3 = 0,
        Bool = 1,
        IntRange = 2,
        None = 3,
    }
}
/*
        public VariableStore(Vector3 Vector3Store)
        {
            variableType = VariableType.Vector3;
            this.Vector3Store = Vector3Store;
            BoolStore = false;
            IntStore = 0;
        }
        public VariableStore(bool BoolStore)
        {
            variableType = VariableType.Bool;
            this.Vector3Store = Vector3.zero;
            this.BoolStore = BoolStore;
            IntStore = 0;
        }
        public VariableStore(int IntStore)
        {
            variableType = VariableType.IntRange;
            Vector3Store = Vector3.zero;
            BoolStore = false;
            this.IntStore = IntStore;
        }

        public Vector3 Vector3Store;
        public bool BoolStore;
        */
