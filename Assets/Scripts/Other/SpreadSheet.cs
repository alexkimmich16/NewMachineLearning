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

    public bool Written;

    private float Interval = 60f;

    public Dictionary<string, bool> IsActiveDictionary;
            //
    public void PrintStats()
    {
        TextWriter tw;
        if (Written == false)
        {
            tw = new StreamWriter(Location(), false);
            string LineWrite = "Motion, Set";
            if (IsActiveDictionary["MotionFinishedCount"] == true) { LineWrite = LineWrite + ", MotionFinishedCount"; }
            if (IsActiveDictionary["Guess"] == true) { LineWrite = LineWrite + ", Guess"; }
            if (IsActiveDictionary["Truth"] == true) { LineWrite = LineWrite + ", Truth"; }
            tw.WriteLine(LineWrite);
            Written = true;
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
            tw.WriteLine(CombinedString);
        }
        DT.Stats.Clear();
        tw.Close();
    }
    public void UpdateSpreadSheet()
    {
        Debug.Log("update SpreadSheet");
        //Debug.Log(Location());
        PrintStats();
    }
    IEnumerator CallPrintStats()
    {
        while (true)
        {
            PrintStats();
            yield return new WaitForSeconds(Interval);
        }
    }
    void Start()
    {
        Motions = LearnManager.instance.motions;
        if (Motions.Motions.Count == 0)
            return;
        StartCoroutine(CallPrintStats());
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
            UpdateSpreadSheet();
    }
}

