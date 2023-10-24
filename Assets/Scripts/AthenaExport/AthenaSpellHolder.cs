using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Barracuda;
using Sirenix.OdinInspector;
using System.Linq;
using System.IO;
using System;
using Unity.Mathematics;

namespace Athena
{
    [CreateAssetMenu(fileName = "SpellHolder", menuName = "ScriptableObjects/AthenaSpellHolder", order = 3), Serializable]
    public class AthenaSpellHolder : SerializedScriptableObject
    {
        [ListDrawerSettings(ShowIndexLabels = true)]public List<RestrictionListItem> restrictions = new List<RestrictionListItem>();
        
        
        
        public Dictionary<Spell, SpellReferences> Spells = new Dictionary<Spell, SpellReferences>();


        public Model GetModel(Spell spell)
        {

            if (Spells.ContainsKey(spell))
                return ModelLoader.Load(Spells[spell].Model);
            Debug.LogError("Couldn't Find: " + spell.ToString());
            return null;
        }

        public class SpellReferences
        {
            public NNModel Model;
            public event AnonymousSpellStateEvent SpellEvent;

            //public Dictionary<Side, int> SideStates = new Dictionary<Side, int>() { { Side.right, 0 }, { Side.left, 0 } };
            public void StateChangeEvent(Side side, int state) { SpellEvent?.Invoke(side, state); }
        }
    }
}


