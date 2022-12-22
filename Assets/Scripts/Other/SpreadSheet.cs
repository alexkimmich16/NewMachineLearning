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
    private AllMotions Motions;

    private bool HasWritten;

    public float WriteToSpreadsheetInterval = 60f;

    public Dictionary<string, bool> IsActiveDictionary;
            //
    public void PrintStats()
    {
        TextWriter tw;
        if (HasWritten == false)
        {
            tw = new StreamWriter(Location(), false);
            string LineWrite = "Motion, Set";
            if (IsActiveDictionary["MotionFinishedCount"] == true) { LineWrite = LineWrite + ", MotionFinishedCount"; }
            if (IsActiveDictionary["Guess"] == true) { LineWrite = LineWrite + ", Guess"; }
            if (IsActiveDictionary["Truth"] == true) { LineWrite = LineWrite + ", Truth"; }
            if (IsActiveDictionary["Timer"] == true) { LineWrite = LineWrite + ", Timer"; }
            tw.WriteLine(LineWrite);
            HasWritten = true;
        }
        else
        {
            tw = new StreamWriter(Location(), true);
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
        Motions = LearnManager.instance.motions;
        if (Motions.Motions.Count == 0)
            return;
        StartCoroutine(CallPrintStats());
    }
}

