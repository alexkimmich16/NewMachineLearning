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
    [ReadOnly]public List<int> InActive;
    [ReadOnly, ListDrawerSettings(ShowIndexLabels = true)] public List<int2> Ranges;
    


    public Dictionary<Spell, SingleRestriction> Restrictions;
    [System.Serializable] public struct SingleRestriction
    {
        public List<RestrictionSettings> Restrictions;
        public int2 FrameLock;
    }
    [System.Serializable]public struct RestrictionSettings
    {
        //[GUIColor("Orange")] 
        public RestrictionListItem Restriction;
        public Vector2 Lock;
    }


    public bool AbleToCallWithButtons;
    public bool ShouldStitch;



    public bool RestrictFrameLength = false;
    [FoldoutGroup("Lock"), ShowIf("RestrictFrameLength")] public int2 WithinFrames;

    public MotionEditor M = MotionEditor.instance;

    public bool InsideFrameLength(int Frame) { return RestrictFrameLength ? Frame >= WithinFrames.x && Frame <= WithinFrames.y : true; }

    //[FoldoutGroup("AllTrueMotions")] public List<List<Vector2>> TrueMotions;

    [FoldoutGroup("Lock"), Button(ButtonSizes.Small)]
    public void LockAll()
    {
        foreach(Spell spell in Restrictions.Keys)
        {
            for (int i = 0; i < Cycler.MovementCount(spell); i++)
            {
                LockMotion(spell, i);
            }
        }

        GetInActiveMotions(M.MotionType);
        GetRanges(M.MotionType);
    }
    public void GetRanges(Spell spell) { Ranges = Enumerable.Range(0, Cycler.MaxTrueMotion(spell) + 1).Select(x => new int2((int)Cycler.TrueRange(spell, x).x, (int)Cycler.TrueRange(spell, x).y)).ToList(); }
    public void GetInActiveMotions(Spell spell) { InActive = Enumerable.Range(0, Cycler.MaxTrueMotion(spell) + 1).Where(x => Cycler.TrueRange(spell, x) == new Vector2(-1, -1)).ToList(); }
    public void LockMotion(Spell spell, int MotionIndex)
    {
        //ALL FRAMES
        bool Works = MotionIndex <= Cycler.MaxTrueMotion(spell);
        if (!Works)
        {
            Cycler.Movements[spell].Motions[MotionIndex].TrueRanges = new List<Vector2>() { new Vector2(-1f, -1f) };

            return;
        }
        List<bool> WorkingFrames = Enumerable.Range(0, Cycler.FrameCount(spell, MotionIndex)).Select(index => index >= Restrictions[spell].FrameLock.x && index <= Restrictions[spell].FrameLock.y).ToList();
        foreach (RestrictionSettings settings in Restrictions[spell].Restrictions)
        {
            for (int i = 0; i < Cycler.FrameCount(spell, MotionIndex); i++)
            {
                
                List<float> Output = settings.Restriction.GetValue(Cycler.AtFrameInfo(spell, MotionIndex, i));

                //Debug.Log("index:" + i + "  " + Output + " Works: " + (Output < settings.Lock.x || Output > settings.Lock.y));
                if (Output[0] < settings.Lock.x || Output[0] > settings.Lock.y || !Works) //|| InsideFrameLength(i) == true
                    WorkingFrames[i] = false;
            }
        }


        List<Vector2> WorkingRanges = AthenaMotion.ConvertToRange(WorkingFrames);
        if (ShouldStitch)
        {
            if (WorkingRanges.Count <= 0)
            {
                Debug.LogWarning("something wrong");
                return;
            }
            Vector2 StitchedVector = new Vector2(WorkingRanges[0].x, WorkingRanges[^1].y);
            WorkingRanges = new List<Vector2>() { StitchedVector };
        }
        Cycler.Movements[spell].Motions[MotionIndex].TrueRanges = WorkingRanges;
        //GetInActiveMotions(spell);
    }
}
