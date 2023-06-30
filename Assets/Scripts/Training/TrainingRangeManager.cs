using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RestrictionSystem;
using System.Linq;
public class TrainingRangeManager : MonoBehaviour
{
    private MotionSettings settings => RestrictionManager.instance.RestrictionSettings;
    //private RestrictionManager RM => RestrictionManager.instance;
    private RestrictionStatManager RSM => RestrictionStatManager.instance;
    //private Spell spell => MotionEditor.instance.MotionType;
    void Start()
    {
        UpdateMinMax();
    }
    public void UpdateMinMax()
    {
        MovementControl MC = MovementControl.instance;

        List<SingleInfo> Frames = new List<SingleInfo>();
        settings.ExpectedMaxMin = new Vector2[MC.MotionCount() - 1][];
        for (int i = 0; i < MC.MotionCount() - 1; i++)
        {
            List<SingleFrameRestrictionValues> TrueValues = RSM.GetRestrictionsForMotions((Spell)i + 1, settings.MotionRestrictions[i]).Where(x => x.AtMotionState).ToList();
            settings.ExpectedMaxMin[i] = new Vector2[TrueValues[0].OutputRestrictions.Count];
            for (int j = 0; j < TrueValues[0].OutputRestrictions.Count; j++)//for each restriction
            {
                //TrueValues.Select(x => x.OutputRestrictions[j]).ToArray();
                float[] TrueRestrictions = TrueValues.Select(x => x.OutputRestrictions[j]).ToArray();
                float[] Values = new float[TrueRestrictions.Length];
                for (int frame = 0; frame < TrueRestrictions.Length; frame++)//each frame
                {
                    float Total = 0f;

                    for (int pow = 0; pow < settings.Coefficents[i].Coefficents[j].Degrees.Count; pow++)//power
                    {
                        Total += Mathf.Pow(TrueRestrictions[frame], pow + 1) * settings.Coefficents[i].Coefficents[j].Degrees[pow];
                    }
                    Values[frame] = Total;
                }
                settings.ExpectedMaxMin[i][j] = new Vector2(Values.Min(), Values.Max());
            }

        }
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
