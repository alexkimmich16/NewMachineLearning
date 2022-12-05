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
    
    //public string OutputName;
    public string Location() { return Application.dataPath + "/SpreadSheets/AIStatHolder.csv"; }
    private AllMotions Motions;

    public bool Written;

    public float Interval;

    public void PrintStats()
    {
        TextWriter tw;
        if (Written == false)
        {
            tw = new StreamWriter(Location(), false);
            tw.WriteLine("Guess, Truth, Correct, Index, Set");
            Written = true;
        }
        else
        {
            tw = new StreamWriter(Location(), true);
        }
        
        DataTracker DT = DataTracker.instance;
        for (int i = 0; i < DT.Stats.Count; i++)
        {
            tw.WriteLine(DT.Stats[i].Guess + "," + DT.Stats[i].Truth + "," + DT.Stats[i].Correct + "," + DT.Stats[i].Index + "," + DT.Stats[i].Set);
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
        if (Input.GetKeyDown(KeyCode.R))
            UpdateSpreadSheet();
    }
}

