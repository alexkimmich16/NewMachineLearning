using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
namespace Athena
{
    [CreateAssetMenu(fileName = "Spell", menuName = "ScriptableObjects/AthenaSpell", order = 2), System.Serializable]
    public class AthenaSpell : ScriptableObject
    {
        [ListDrawerSettings(Expanded = false, ShowIndexLabels = true)] public List<AthenaMotion> Motions;
    }
}

