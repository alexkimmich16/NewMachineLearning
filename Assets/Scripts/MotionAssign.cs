using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Sirenix.OdinInspector;
using RestrictionSystem;
public class MotionAssign : SerializedMonoBehaviour
{
    public float2 DegreesFromHead;

    public LearnManager LM;
    public BruteForce BF;


    public List<bool> List;
    public float Value;
    public List<Vector2> Ranges;

    public KeyCode SetAllButton;
    //public RestrictionManager RM;
    [Button(ButtonSizes.Small)]
    public void SetAll()
    {
        int CurrentMotionEdit = GetComponent<MotionEditor>().MotionNum;
        int CurrentSpellEdit = (int)GetComponent<MotionEditor>().MotionType;
        List<bool> Frames = new List<bool>();
        for (int i = 0; i < LM.MovementList[CurrentSpellEdit].Motions[CurrentMotionEdit].Infos.Count; i++)//frame
        {
            float Angle = RestrictionManager.RestrictionDictionary[Restriction.HandToHeadAngle].Invoke(null, null, LM.MovementList[CurrentSpellEdit].GetRestrictionInfoAtIndex(CurrentMotionEdit, i));
            bool Works = Angle > DegreesFromHead.x && Angle < DegreesFromHead.y;
            Frames.Add(Works);
        }

        List = Frames;
        Ranges = Motion.ConvertToRange(Frames);
        LM.MovementList[CurrentSpellEdit].Motions[CurrentMotionEdit].IntoRange(Frames);
        ///convert to ints
    }

    // Update is called once per frame
    void Update()
    {
        //degree assign

        if (Input.GetKeyDown(SetAllButton))
            SetAll();


        PastFrameRecorder PR = PastFrameRecorder.instance;
        if (PR.RightInfo.Count < PR.MaxStoreInfo - 1)
            return;
        //270 on right, 90 on left
        Value = RestrictionManager.RestrictionDictionary[Restriction.HandToHeadAngle].Invoke(null, PR.PastFrame(Side.right), PastFrameRecorder.instance.GetControllerInfo(Side.right));
    }
}
