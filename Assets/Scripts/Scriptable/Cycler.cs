using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Athena;
public static class Cycler
{
    public static Dictionary<Spell, AthenaSpell> Movements { get { return Resources.Load<SpellList>("SpellList").Movements; } }


    public delegate void ProcessAction(Spell spell, int motionIndex, int frameIndex, AthenaFrame frame);


    public delegate void Onchange();
    public delegate void MotionCallback(AthenaMotion selectedMotion);
    
    public static void FrameLoop(ProcessAction action, Onchange spellChange, Onchange motionChange)
    {
        foreach (Spell spell in Movements.Keys)
        {
            spellChange?.Invoke();
            for (int motionIndex = 0; motionIndex < Movements[spell].Motions.Count; motionIndex++)
            {
                
                AthenaMotion motion = Movements[spell].Motions[motionIndex];
                for (int frameIndex = 0; frameIndex < motion.Infos.Count; frameIndex++)
                {
                    action(spell, motionIndex, frameIndex, AtFrameInfo(spell, motionIndex, frameIndex));
                }
                motionChange?.Invoke();
            }
        }
    }
   

    public static int MotionCount() { return Movements.Count; }
    public static int MovementCount(Spell Spell) { return Movements[Spell].Motions.Count; }

    public static int FrameCount(Spell Spell, int Motion) { return Movements[Spell].Motions[Motion].Infos.Count; }
    public static AthenaFrame AtFrameInfo(Spell Spell, int Motion, int Frame) { return Movements[Spell].Motions[Motion].Infos[Frame]; }
    public static int TrueRangeCount(Spell Spell, int Motion) { return Movements[Spell].Motions[Motion].TrueRanges.Count; }
    public static bool FrameWorks(Spell Spell, int Motion, int Frame) { return Movements[Spell].Motions[Motion].AtFrameState(Frame); }



    public static List<AthenaFrame> GetFrames(Spell Spell, int Motion, int FirstFrame, int LastFrame)
    {
        List<AthenaFrame> Frames = new List<AthenaFrame>();
        for (int i = FirstFrame; i < LastFrame; i++)
        {
            Frames.Add(AtFrameInfo(Spell, Motion, i));
        }
        return Frames;
    }
}
