using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System.Linq;
using Athena;
[CreateAssetMenu(fileName = "SpellList", menuName = "ScriptableObjects/SpellList", order = 2), System.Serializable]
public class SpellList : SerializedScriptableObject
{
    [FoldoutGroup("Movements")] public Dictionary<Spell, AthenaSpell> Movements;

    [Button] public void Press()
    {
        int Count = 0;
        /*
        Cycler.FrameLoop((spellIndex, motionIndex, frameIndex, frame) =>
        {
            // Use the indexes to access the frame
            Count++;
        });
        */
        Debug.Log(Count);
    }
}
