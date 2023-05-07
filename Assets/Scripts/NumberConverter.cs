using System.Collections.Generic;
using UnityEngine;
using System.Linq;
namespace RestrictionSystem
{

    public enum VariableType
    {
        Vector3 = 0,
        Bool = 1,
        Int = 2,
        AxisList = 3,
        CheckType = 4,
    }
    public enum ValueLockType
    {
        checkType = 0,
        OtherDirection = 1,
        UseLocalHandPos = 2,
        UseLocalHandRot = 3,
        Direction = 4,
        UseAxisList = 5,
    }
    [System.Serializable]
    public struct VariableStore
    {
        public VariableType variableType;
        public Vector3 Vector3Store;
        public bool BoolStore;
        public int IntStore;
        public List<Axis> AxisStore;
        public CheckType CheckTypeStore;
        public VariableStore(VariableType variableType, Vector3 Vector3Store, bool BoolStore, int IntStore, List<Axis> AxisStore, CheckType CheckTypeStore)
        {
            this.variableType = variableType;
            this.Vector3Store = Vector3Store;
            this.BoolStore = BoolStore;
            this.IntStore = IntStore;
            this.AxisStore = AxisStore;
            this.CheckTypeStore = CheckTypeStore;
        }
        public VariableStore(Vector3 Vector3Store) : this(VariableType.Vector3, Vector3Store, false, 0, null, (CheckType)0) { }
        public VariableStore(bool BoolStore) : this(VariableType.Bool, Vector3.zero, BoolStore, 0, null, (CheckType)0) { }
        public VariableStore(int IntStore) : this(VariableType.Int, Vector3.zero, false, IntStore, null, (CheckType)0) { }
        public VariableStore(List<Axis> AxisStore) : this(VariableType.AxisList, Vector3.zero, false, 0, AxisStore, (CheckType)0) { }
        public VariableStore(CheckType CheckTypeStore) : this(VariableType.CheckType, Vector3.zero, false, 0, null, CheckTypeStore) { }
    }
    public static class NumberConverter
    {
        public static List<Vector3> Vector3List = new List<Vector3>() { Vector3.down, Vector3.up, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };
        public static List<List<Axis>> AxisList = new List<List<Axis>>() { { new List<Axis>() { Axis.Y } }, new List<Axis>() { Axis.X, Axis.Y, Axis.Z }, new List<Axis>() { Axis.X, Axis.Z } };

        public static int GetIndexOfValue(VariableStore variableStore)
        {
            if(variableStore.variableType == VariableType.Vector3)
                return Vector3List.Select((v, i) => new { v, i }).First(vi => vi.v == variableStore.Vector3Store).i;
            else if(variableStore.variableType == VariableType.Bool)
                return variableStore.BoolStore ? 1 : 0;
            else if (variableStore.variableType == VariableType.Int)
                return variableStore.IntStore;
            else if (variableStore.variableType == VariableType.AxisList)
                return AxisList.Select((v, i) => new { v, i }).First(A => A.v == variableStore.AxisStore).i;
            else if (variableStore.variableType == VariableType.CheckType)
                return (int)variableStore.CheckTypeStore;
            return 0;
        }
        public static VariableStore GetValueOfIndex(int Index, VariableType variableType)
        {
            if (variableType == VariableType.Vector3)
                return new VariableStore(Vector3List[Index]);
            else if (variableType == VariableType.Bool)
                return new VariableStore(Index == 1 ? true : false);
            else if (variableType == VariableType.Int)
                return new VariableStore(Index);
            else if (variableType == VariableType.AxisList)
                return new VariableStore(AxisList[Index]);
            else if (variableType == VariableType.CheckType)
                return new VariableStore((CheckType)Index);
            return new VariableStore(0);
        }

        public static VariableStore GetRestrictionVar(SingleRestriction restriction, ValueLockType RestrictionType)
        {
            if (RestrictionType == ValueLockType.checkType)
                return new VariableStore(restriction.checkType);
            else if (RestrictionType == ValueLockType.OtherDirection)
                return new VariableStore(restriction.OtherDirection);
            else if (RestrictionType == ValueLockType.UseLocalHandPos)
                return new VariableStore(restriction.UseLocalHandPos);
            else if (RestrictionType == ValueLockType.UseLocalHandRot)
                return new VariableStore(restriction.UseLocalHandRot);
            else if (RestrictionType == ValueLockType.Direction)
                return new VariableStore(restriction.Direction);
            else if (RestrictionType == ValueLockType.UseAxisList)
                return new VariableStore(restriction.UseAxisList);
            return new VariableStore(0);
        }
        //return Enumerable.Range(0, System.Enum.GetValues(typeof(Restriction)).Length).Select(c => Enumerable.Range(0, ByType.Count).Where(d => DependantVariableList((Restriction)c)[d] && RestrictionVariableLocks[c].VariableLocks.Any(V => V.changeType == ChangeType.LockVariable && (int)V.ValueToLock == d))).ToList();
        //Working = Enumerable.Range(0, System.Enum.GetValues(typeof(Restriction)).Length).Select(c => Enumerable.Range(0, ByType.Count).Where(d => DependantVariableList((Restriction)c)[d] == true && RestrictionVariableLocks[c].VariableLocks.Any(V => V.changeType == ChangeType.LockVariable && (int)V.ValueToLock == d)).ToList()).ToList();
        //return Enumerable.Range(0, System.Enum.GetValues(typeof(Restriction)).Length).Select(c => Enumerable.Range(0, Working[c].Count).Aggregate((a, b) => a * b)).ToList();
        //Enumerable.Range(0, Working.Count).Aggregate((a, b) => a * b)).ToList()
    }
}

