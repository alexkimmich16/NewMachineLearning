using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Athena;
using Sirenix.OdinInspector;
public class InputDebug : SerializedMonoBehaviour
{
    public static InputDebug instance;
    private void Awake() { instance = this; }

    public Dictionary<Side, SkinnedMeshRenderer> Hands;
    public List<Material> Materials;

    public PastFrameRecorder P => PastFrameRecorder.instance;

    private void Start()
    {
        Runtime.AllSpellStates += RecieveStateChange;
    }

    public void RecieveStateChange(Spell spell, Side side, int Index)
    {
        
        if(spell == Spell.Fireball)
            Hands[side].material = Materials[Index];
    }



}
