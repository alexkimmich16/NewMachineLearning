using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.UI;
using TMPro;
using RestrictionSystem;
namespace Training
{
    public class TrainManager : SerializedMonoBehaviour
    {
        /// <summary>
        /// to be automated and updated with regression
        /// should incude:
        /// type of restriction
        /// 
        /// </summary>
        public List<SpellTips> SpellTips;
        public TextMeshProUGUI text;
        private PastFrameRecorder PFR => PastFrameRecorder.instance;
        private MotionSettings settings => RestrictionManager.instance.RestrictionSettings;

        public List<float> Differences;

        // Update is called once per frame
        void Update()
        {
            if(PastFrameRecorder.IsReady())
                text.text = GetRequest();
        }

        public string GetRequest()
        {
            Spell spell = MotionEditor.instance.MotionType;

            Differences = new List<float>();

            ///get most accurate restriction
            ///

            for (int i = 0; i < RestrictionManager.instance.RestrictionSettings.MotionRestrictions[((int)spell) - 1].Restrictions.Count; i++)
            {
                SingleRestriction Restriction = RestrictionManager.instance.RestrictionSettings.MotionRestrictions[((int)spell) - 1].Restrictions[i];
                float RestictionValue = RestrictionManager.RestrictionDictionary[Restriction.restriction].Invoke(Restriction, PFR.PastFrame(Side.right), PFR.GetControllerInfo(Side.right));

                float Current = 0f;

                for (int pow = 0; pow < settings.Coefficents[((int)spell) - 1].Coefficents[i].Degrees.Count; pow++)//power
                {
                    Current += Mathf.Pow(RestictionValue, pow + 1) * settings.Coefficents[((int)spell) - 1].Coefficents[i].Degrees[pow];
                }

                Vector2 MaxMin = RestrictionManager.instance.RestrictionSettings.ExpectedMaxMin[((int)spell) - 1][i];

                Differences.Add(MaxMin.y - Current);
                //if()
            }
            float Highest = 0f;
            int Index = 0;
            for (int i = 0; i < Differences.Count; i++)
            {
                if(Differences[i] > Highest && SpellTips[((int)spell) - 1].restrictionTips[i].Active)
                {
                    Index = i;
                    Highest = Differences[i];
                }
            }
            if (SpellTips[((int)spell) - 1].restrictionTips[Index].TipName == "")
                return "Training: " + RestrictionManager.instance.RestrictionSettings.MotionRestrictions[((int)spell) - 1].Restrictions[Index].Label;
            else
                return SpellTips[((int)spell) - 1].restrictionTips[Index].TipName;
        }
        
    }
    public struct SpellTips
    {
        public string Spell;
        public List<RestrictionTip> restrictionTips;
    }
    public struct RestrictionTip
    {
        public string TipName;
        public bool Active;
    }
}

