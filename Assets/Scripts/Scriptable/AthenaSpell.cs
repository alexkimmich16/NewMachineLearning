using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System.Linq;
namespace Athena
{
    [CreateAssetMenu(fileName = "Spell", menuName = "ScriptableObjects/AthenaSpell", order = 2), System.Serializable]
    public class AthenaSpell : ScriptableObject
    {
        [ListDrawerSettings(Expanded = false, ShowIndexLabels = true)] public List<AthenaMotion> Motions;
        public Vector2 TrueMotions;

        public bool IsTrueMotion(int Index) { return Index >= TrueMotions.x && Index <= TrueMotions.y; }
    }
}

