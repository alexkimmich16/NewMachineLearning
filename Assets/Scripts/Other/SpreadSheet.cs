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

    private bool HasWritten;

    public float WriteToSpreadsheetInterval = 60f;

    public Dictionary<string, bool> IsActiveDictionary;
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
        StartCoroutine(CallPrintStats());

        ProceduralTesting.OnBeforeRestart += BeforeRestart;
    }

    public void BeforeRestart()
    {
        //PrintStats();
        PrintSpace();
        //seperate spreadsheet;
    }
}

