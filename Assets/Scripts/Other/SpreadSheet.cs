using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
public enum InfoType
{
    RightWrongBool = 0,
    CapRatio = 1,
}
public class SpreadSheet : MonoBehaviour
{
    public static SpreadSheet instance;
    private void Awake() { instance = this; }
    
    private string RightWrongName;
    private string FalseFramesName;
    private AllMotions Motions;

    public static bool ResetDataIndex = true;

    public void PrintRightWrong(RightWrongStats Stats)
    {
        TextWriter tw = new StreamWriter(RightWrongName, false);
        tw.WriteLine("Right, Wrong, Bool");
        for (int i = 0; i < Stats.RightWrong.Count; i++)
        {
            tw.WriteLine(Stats.RightWrong[i].x + "," + Stats.RightWrong[i].y + "," + Stats.RightWrong[i].z);
        }
        tw.Close();
    }

    public void PrintFalseFrames(LastFailureStats Stats)
    {
        TextWriter tw = new StreamWriter(FalseFramesName, false);
        tw.WriteLine("Index, Frequency, Bool");

        for (int i = 0; i < Stats.LastSetFailures.Count; i++)
        {
            int Index = (int)Stats.LastSetFailures[i].x;
            float Frequency = Stats.LastSetFailures[i].y / Motions.Motions[Index].Infos.Count;
            tw.WriteLine(Index + "," + Frequency + "," + Stats.LastSetFailures[i].z);
        }
        tw.Close();
    }

    void Start()
    {
        Motions = LearnManager.instance.motions;
        if (Motions.Motions.Count == 0)
            return;
        
        if (ResetDataIndex)
        {
            for (int i = 0; i < Motions.Motions.Count; i++)
            {
                Motions.Motions[i].TrueIndex = i;
            }
        }

        RightWrongName = Application.dataPath + "/SpreadSheets/RightWrong" + Most("RightWrong").ToString() + ".csv";
        FalseFramesName = Application.dataPath + "/SpreadSheets/FalseFrames" + Most("FalseFrames").ToString() + ".csv";
        int Most(string Name)
        {
            int Num = 0;
            while (Num < 50)
            {
                if (System.IO.File.Exists(Application.dataPath + "/SpreadSheets/" + Name + Num.ToString() + ".csv"))
                    Num += 1;
                else
                    return Num;
            }
            return 1000;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    /*
    public List<AICapStats> ConvertToCapStats(AIStats stats)
    {
        bool Done = false;
        int Index = 0;
        while (Done == false)
        {
            int Total
            if(stats.RightWrong[Index] > CapMaxInterval)
            {

            }
        }
    }
    */
}

