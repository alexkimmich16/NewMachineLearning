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
        public List<Vector2> TrueMotions;

        public bool IsTrueMotion(int Index) { return TrueMotions.Any(vector => Index >= vector.x && Index <= vector.y); }

        public void AdjustValues()
        {
            foreach(AthenaMotion mot in Motions)
            {
                foreach (AthenaFrame fra in mot.Infos)
                {
                    //fra.Devices[0].Rot = new Vector3(fra.Devices[0].Rot.x / 360f, fra.Devices[0].Rot.y / 360f, fra.Devices[0].Rot.z / 360f);
                }
            }
        }
    }
}

