using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using RestrictionSystem;
using System.Linq;
public class ConditionTester : SerializedMonoBehaviour
{
    //INVOLVES WHEN TO CAST
    //SHOULD INCLUDE BOTH INSTANT AND MEANWHILE DEMOS

    [FoldoutGroup("Object")] public GameObject SpawnPrefab;
    [FoldoutGroup("Object"), ReadOnly] public GameObject SpawnedObject;
    [FoldoutGroup("Object")] public float KillTime;

    public List<SingleInfo> TestInfo;
    public void Spawn(Vector3 Position)
    {
        if (SpawnedObject != null)
            Destroy(SpawnedObject);
        SpawnedObject = Instantiate(SpawnPrefab, Vector3.zero, Quaternion.identity);
        Destroy(SpawnedObject, KillTime);
    }

    public CurrentLearn Motion() { return MotionEditor.instance.MotionType; } 

    ///involves 2 things:
    ///getting correct coefficents
    /// testing
    public void CalculatetCoefficents()
    {
        MotionConditionInfo Condition = ConditionManager.instance.conditions.MotionConditions[(int)Motion() - 1];
        List<SingleInfo> singleInfos = GetStartEnds(Motion());
        for (int i = 0; i < singleInfos.Count; i += 2)//for each start and end(for loop +=2 each time)
        {
            SingleInfo Start = singleInfos[i];
            SingleInfo End = singleInfos[i + 1];

            float TimeDifference = End.SpawnTime - Start.SpawnTime;
            float DistanceDifference = Vector3.Distance(End.HandPos, Start.HandPos);
        }

        //find best conditions and values that fits ALL GIVEN MOTIONS
    }
    public void TestAll()
    {
        //get all starts and ends
        CurrentLearn CurrentMotion = Motion();
        List<SingleInfo> singleInfos = GetStartEnds(Motion());

        
    }
    void Start()
    {
        TestInfo = GetStartEnds(Motion());
    }
    List<SingleInfo> GetStartEnds(CurrentLearn Motion) // works
    {
        List<SingleInfo> ReturnValue = new List<SingleInfo>();
        
        for (int i = 0; i < LearnManager.instance.MovementList[(int)Motion].Motions.Count; i++)
        {
            if (MotionAssign.instance.TrueMotions[(int)Motion - 1].Any(Vector => (i >= Vector.x && i <= Vector.y)))
            {
                Motion motion = LearnManager.instance.MovementList[(int)Motion].Motions[i];
                ReturnValue.Add(motion.Infos[(int)motion.TrueRanges[0].x]); 
                ReturnValue.Add(motion.Infos[(int)motion.TrueRanges[motion.TrueRanges.Count - 1].y]); 
            }
        }
        return ReturnValue;
        //based on motionassinger
    }

    void Update()
    {
        ///include:
        ///time
        ///distance
        ///longevity(as more correct in a row, require more false in a row to stop)
        ///


        if (MotionEditor.instance.TestAllMotions.isOn == false)
        {

        }
    }
}
