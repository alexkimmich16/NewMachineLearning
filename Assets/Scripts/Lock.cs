using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Sirenix.OdinInspector;
using System.Linq;
using Athena;
public class Lock : SerializedMonoBehaviour
{

    public static Lock instance;
    private void Awake() { instance = this; }

    public enum Restriction
    {
        Magnitude = 0,
        TestValue = 1,
    }
    public struct RestrictionUI
    {
        public Restriction restriction;
        [ShowIf("restriction", Restriction.TestValue)] public Axis axis;
        //[ShowIf("restriction", Restriction.TestValue)] 
        public Value value;

        public enum Axis { X, Y, Z }
        public enum Value { pos, rot, vel, velRot }

        public float Returnvalue(AthenaFrame frame)
        {
            DeviceInfo info = frame.Devices[0];
            Dictionary<Value, Vector3> Values = new Dictionary<Value, Vector3>() { { Value.pos, info.Pos }, { Value.rot, info.Rot }, { Value.vel, info.velocity }, { Value.velRot, info.angularVelocity } };
            if (restriction == Restriction.Magnitude)
            {
                return Values[value].magnitude;
            }
            else if (restriction == Restriction.TestValue)
            {
                List<float> Vector3Axis = new List<float>() { Values[value].x, Values[value].y, Values[value].z};
                return Vector3Axis[(int)axis];
            }
            Debug.LogError("outside of possible options");
            return 0f;
        }


    }



    public Dictionary<Spell, SingleRestriction> Restrictions;
    [System.Serializable] public struct SingleRestriction
    {
        public List<RestrictionSettings> Restrictions;
        
    }
    [System.Serializable]public struct RestrictionSettings
    {
        //[GUIColor("Orange")] 
        public RestrictionUI Restriction;
        public Vector2 Lock;
    }


    public bool AbleToCallWithButtons;
    public bool ShouldStitch;



    public bool RestrictFrameLength = false;
    [FoldoutGroup("Lock"), ShowIf("RestrictFrameLength")] public int2 WithinFrames;

    [FoldoutGroup("Lock")] public KeyCode LockButton;

    public Athena.Athena A = Athena.Athena.instance;
    public MotionEditor M = MotionEditor.instance;

    public bool InsideFrameLength(int Frame) { return RestrictFrameLength ? Frame >= WithinFrames.x && Frame <= WithinFrames.y : true; }

    //[FoldoutGroup("AllTrueMotions")] public List<List<Vector2>> TrueMotions;

    [FoldoutGroup("Lock"), Button(ButtonSizes.Small)]
    public void LockAll()
    {
        foreach(Spell spell in Restrictions.Keys)
        {
            
            for (int i = 0; i < A.MovementCount(spell); i++)
            {
                //Debug.Log(spell.ToString() + "  " + i);
                //int Start = 
                LockMotion(spell, i);
            }
        }
    }
    [FoldoutGroup("Lock"), Button(ButtonSizes.Small)] public void LockCurrent() { LockMotion(M.MotionType, M.MotionNum); }
    public void LockMotion(Spell spell, int MotionIndex)
    {
        //ALL FRAMES

        bool Works = A.Movements[spell].IsTrueMotion(MotionIndex);
        if (!Works)
        {
            A.Movements[spell].Motions[MotionIndex].TrueRanges = new List<Vector2>() { new Vector2(-1f, -1f) };

            return;
        }


        List<bool> WorkingFrames = Enumerable.Repeat(true, A.FrameCount(spell, MotionIndex)).ToList();//ALL RESTRICTIONS
        foreach (RestrictionSettings settings in Restrictions[spell].Restrictions)
        {
            for (int i = 0; i < A.FrameCount(spell, MotionIndex); i++)
            {
                float Output = settings.Restriction.Returnvalue(A.AtFrameInfo(spell, MotionIndex, i));
                //Debug.Log("index:" + i + "  " + Output + " Works: " + (Output < settings.Lock.x || Output > settings.Lock.y));
                if (Output < settings.Lock.x || Output > settings.Lock.y || !Works) //|| InsideFrameLength(i) == true
                    WorkingFrames[i] = false;
            }
        }


        List<Vector2> WorkingRanges = AthenaMotion.ConvertToRange(WorkingFrames);
        if (ShouldStitch)
        {
            Vector2 StitchedVector = new Vector2(WorkingRanges[0].x, WorkingRanges[WorkingRanges.Count - 1].y);
            WorkingRanges = new List<Vector2>() { StitchedVector };
        }
        A.Movements[spell].Motions[MotionIndex].TrueRanges = WorkingRanges;

    }
    /*
    public bool InsideTrueMotions(int Try, int MotionIndex)
    {
        if (MotionIndex < 0 || MotionIndex > TrueMotions.Count)
            return false;
        return TrueMotions[MotionIndex].Any(Vector => Try >= Vector.x && Try <= Vector.y);
    }
    */
    // Update is called once per frame
    void Update()
    {
        //degree assign
        if (!AbleToCallWithButtons)
            return;
        if (Input.GetKeyDown(LockButton))
            LockCurrent();

        //270 on right, 90 on left
        //Value = RestrictionManager.RestrictionDictionary[Restriction.HandToHeadAngle].Invoke(null, PR.PastFrame(Side.right), PastFrameRecorder.instance.GetControllerInfo(Side.right));
    }
}
