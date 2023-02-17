using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Sirenix.OdinInspector;
public class SpreadSheet : SerializedMonoBehaviour
{
    public static SpreadSheet instance;
    private void Awake() { instance = this; }
    
    //public string OutputName;

    public string Location() { return Application.dataPath + "/SpreadSheets/AIStatHolder.csv"; }
    public string RestrictionLocation() { return Application.dataPath + "/SpreadSheets/RestrictionStats.csv"; }
    public string MotionsLocation() { return Application.dataPath + "/SpreadSheets/MotionStats.csv"; }
    private bool HasWritten;

    public float WriteToSpreadsheetInterval = 60f;

    public void PrintMotionStats(List<SingleFrameRestrictionValues> Motions)
    {
        TextWriter tw = new StreamWriter(MotionsLocation(), false);
        string HeaderWrite = "";
        for (int i = 0; i < Motions[0].OutputRestrictions.Count; i++)
            HeaderWrite = HeaderWrite + "I" + i.ToString() + ",";
        HeaderWrite = HeaderWrite + "States";
        tw.WriteLine(HeaderWrite);

        for (int i = 0; i < Motions.Count ; i++)
        {
            string LineWrite = "";
            for (int j = 0; j < Motions[i].OutputRestrictions.Count; j++)
                LineWrite = LineWrite + Motions[i].OutputRestrictions[j] + ",";
            LineWrite = LineWrite + Motions[i].AtMotionState;
            tw.WriteLine(LineWrite);
        }
        tw.Close();
    }



    private Dictionary<string, bool> IsActiveDictionary;

    public void PrintStats()
    {
        TextWriter tw = new StreamWriter(Location(), true);
        if (HasWritten == false)
        {
            string LineWrite = "Motion, Set";
            if (IsActiveDictionary["MotionFinishedCount"] == true) { LineWrite = LineWrite + ", MotionFinishedCount"; }
            if (IsActiveDictionary["Guess"] == true) { LineWrite = LineWrite + ", Guess"; }
            if (IsActiveDictionary["Truth"] == true) { LineWrite = LineWrite + ", Truth"; }
            if (IsActiveDictionary["Timer"] == true) { LineWrite = LineWrite + ", Timer"; }
            LineWrite = LineWrite + ", " + ProceduralTesting.instance.ComplexityCurrent + ", " + ProceduralTesting.instance.TrialDurationCurrent;
            tw.WriteLine(LineWrite);
            HasWritten = true;
        }
        
        DataTracker DT = DataTracker.instance;
        for (int i = 0; i < DT.Stats.Count; i++)
        {
            string CombinedString = DT.Stats[i].Motion + "," + DT.Stats[i].Set;
            if (IsActiveDictionary["MotionFinishedCount"] == true) { CombinedString = CombinedString + ", " + DT.Stats[i].MotionPlayNum; }
            if (IsActiveDictionary["Guess"] == true) { CombinedString = CombinedString + ", " + DT.Stats[i].Guess; }
            if (IsActiveDictionary["Truth"] == true) { CombinedString = CombinedString + ", " + DT.Stats[i].Truth; }
            if (IsActiveDictionary["Timer"] == true) { CombinedString = CombinedString + ", " + DT.Stats[i].Timer; }
            tw.WriteLine(CombinedString);
        }
        DT.Stats.Clear();
        tw.Close();
    }
    
    public void PrintRestrictionStats(CurrentLearn motion, List<SingleFrameRestrictionValues> info)
    {
        TextWriter tw = new StreamWriter(RestrictionLocation(), false);
        string LineWrite = "";
        for (int i = 0; i < info[0].OutputRestrictions.Count; i++) // write first lines
        {
            LineWrite = LineWrite + RestrictionSystem.RestrictionManager.instance.RestrictionSettings.MotionRestrictions[(int)motion - 1].Restrictions[i].Label + ", ";
        }
        LineWrite = LineWrite + "Active";
        tw.WriteLine(LineWrite);


        for (int i = 0; i < info.Count; i++) // write stats
        {
            string NewLineWrite = "";
            for (int j = 0; j < info[i].OutputRestrictions.Count; j++)
            {
                LineWrite = LineWrite + info[i].OutputRestrictions[j] + ", ";
            }
            LineWrite = LineWrite + info[i].AtMotionState + ", ";
            tw.WriteLine(LineWrite);
        }
        tw.Close();
    }
    
    public void PrintSpace()
    {
        TextWriter tw = new StreamWriter(Location(), true); 
        tw.WriteLine("");
        tw.Close();
    }
    IEnumerator CallPrintStats()
    {
        while (true)
        {
            PrintStats();
            yield return new WaitForSeconds(WriteToSpreadsheetInterval);
        }
    }
    void Start()
    {
        //StartCoroutine(CallPrintStats());

        //ProceduralTesting.OnBeforeRestart += BeforeRestart;
    }

    public void BeforeRestart()
    {
        //PrintStats();
        PrintSpace();
        //seperate spreadsheet;
    }
}

