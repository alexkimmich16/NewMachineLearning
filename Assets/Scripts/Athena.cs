using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RestrictionSystem;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using Sirenix.OdinInspector;
using System.Linq;

namespace Athena
{
    public class Athena : SerializedMonoBehaviour
    {
        public static Athena instance;
        private void Awake() { instance = this; }
    }

    [System.Serializable]public class AthenaMotion
    {
        public bool AtFrameState(int Frame) { return TrueRanges.Any(range => Frame >= range.x && Frame <= range.y); }

        public List<AthenaFrame> Infos;
        public List<Vector2> TrueRanges;
        [HideInInspector] public int TrueIndex;
        [HideInInspector] public int PlayCount;

        public static List<Vector2> ConvertToRange(List<bool> Values)
        {
            List<Vector2> ranges = new List<Vector2>();
            bool Last = false;
            int Start = 0;
            if (Values.All(state => state == false))
            {
                return new List<Vector2> { new Vector2(-1f, -1f) };
            }

            for (int i = 0; i < Values.Count; i++)
            {
                if (Values[i] != Last)//onchange
                {
                    if (Last == false)
                    {
                        Start = i;
                    }
                    else if (Last == true)
                    {
                        //range is start to i
                        ranges.Add(new Vector2(Start, i - 1));
                    }

                    Last = Values[i];
                }
                else if (Last == true && i == Values.Count - 1)
                {
                    ranges.Add(new Vector2(Start, i - 1));
                }
            }
            return ranges;
        }
    }
}