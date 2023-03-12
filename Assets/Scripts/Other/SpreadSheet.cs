using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Sirenix.OdinInspector;
using RestrictionSystem;
using ExcelDataReader;
using System.Data;
public class SpreadSheet : SerializedMonoBehaviour
{
    public static SpreadSheet instance;
    private void Awake() { instance = this; }



    //public static List<double> Lowest = new List<double>() { 0.001, 0.00001, 0.000000001};

    //public string OutputName;

    public static string ReadExcelCell(int row, int column)
    {
        string cellValue = null;

        FileStream stream = File.Open(Application.dataPath + "/SpreadSheets/Tester.xlsx", FileMode.Open, FileAccess.Read);

        IExcelDataReader excelReader = ExcelReaderFactory.CreateOpenXmlReader(stream);
        DataSet result = excelReader.AsDataSet();

        DataTable table = result.Tables["Sheet1"];

        // Read the value of the cell
        if (row < table.Rows.Count && column < table.Columns.Count)
        {
            cellValue = table.Rows[row][column].ToString();
        }

        excelReader.Close();
        stream.Close();

        return cellValue;
    }

    public string DegreeLocation() { return Application.dataPath + "/SpreadSheets/"+ gameObject.GetComponent<RegressionSystem>().CurrentMotion.ToString() + ".csv"; }
    public string Location() { return Application.dataPath + "/SpreadSheets/AIStatHolder.csv"; }
    public string RestrictionLocation() { return Application.dataPath + "/SpreadSheets/RestrictionStats.csv"; }
    public string MotionsLocation() { return Application.dataPath + "/SpreadSheets/MotionStats.csv"; }

    [Button(ButtonSizes.Small)]
    public void PrintDegreeStats()
    {
        RegressionSystem RS = gameObject.GetComponent<RegressionSystem>();
        Debug.Log(RS.CurrentMotion.ToString());
        MotionRestriction settings = RS.UploadRestrictions;
        List<SingleFrameRestrictionValues> Motions = GetComponent<BruteForce>().GetRestrictionsForMotions(RS.CurrentMotion, settings);
        int Degrees = RS.EachTotalDegree;

        string HeaderWrite = "";
        for (int i = 0; i < Motions[0].OutputRestrictions.Count; i++)
        {
            for (int j = 0; j < Degrees; j++)
            {
                HeaderWrite = HeaderWrite + settings.Restrictions[i].Label + "^" + (j + 1).ToString() + ",";
            }
        }
        HeaderWrite = HeaderWrite + "States";
        TextWriter tw = new StreamWriter(DegreeLocation(), false);
        tw.WriteLine(HeaderWrite);
        //
        ///states to 0/1
        for (int i = 0; i < Motions.Count; i++)
        {
            string LineWrite = "";
            for (int j = 0; j < Motions[0].OutputRestrictions.Count; j++)
            {
                for (int k = 0; k < Degrees; k++)
                {
                    float DegreeValue = Mathf.Pow(Motions[i].OutputRestrictions[j], k + 1);
                    float Value = DegreeValue > 0.01f ? DegreeValue : 0.01f;
                    LineWrite = LineWrite + Value + ",";
                }
            }
            LineWrite = LineWrite + (Motions[i].AtMotionState ? 1 : 0);
            tw.WriteLine(LineWrite);
        }
        tw.Close();
    }


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
}

