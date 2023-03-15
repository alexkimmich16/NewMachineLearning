using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Sirenix.OdinInspector;
using RestrictionSystem;
using System.Data;
using OfficeOpenXml;
public class SpreadSheet : SerializedMonoBehaviour
{
    public static SpreadSheet instance;
    private void Awake() { instance = this; }

    public static string ReadExcelCell(int row, int column)
    {
        string cellValue = null;
        /*
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
        */
        return cellValue;
    }

    public string DegreeLocation() { return Application.dataPath + "/SpreadSheets/"+ gameObject.GetComponent<RegressionSystem>().CurrentMotion.ToString() + ".csv"; }
    public string DegreeLocation2() { return Application.dataPath + "/SpreadSheets/Tester.xlsx"; }
    public string Location() { return Application.dataPath + "/SpreadSheets/AIStatHolder.csv"; }
    public string RestrictionLocation() { return Application.dataPath + "/SpreadSheets/RestrictionStats.csv"; }
    public string MotionsLocation() { return Application.dataPath + "/SpreadSheets/MotionStats.csv"; }

    [Button(ButtonSizes.Small)]
    public void PrintDegreeStats()
    {
        RegressionSystem RS = gameObject.GetComponent<RegressionSystem>();
        Debug.Log("Print: " + RS.CurrentMotion.ToString());
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

    [Button(ButtonSizes.Small)]
    public void PrintDegreeStats2()
    {
        RegressionSystem RS = gameObject.GetComponent<RegressionSystem>();
        Debug.Log("Print: " + RS.CurrentMotion.ToString());
        MotionRestriction settings = RS.UploadRestrictions;
        List<SingleFrameRestrictionValues> Motions = GetComponent<BruteForce>().GetRestrictionsForMotions(RS.CurrentMotion, settings);
        int Degrees = RS.EachTotalDegree;
        //ExcelWorksheet worksheet = new ExcelWorksheet(DegreeLocation2());


        FileInfo existingFile = new FileInfo(DegreeLocation2());
        using (ExcelPackage package = new ExcelPackage(existingFile))
        {
            //get the first worksheet in the workbook
            ExcelWorksheet worksheet = package.Workbook.Worksheets[1];
        }


            /*
            using (var ew = new ExcelWriter(DegreeLocation2()))
            {
                //set headers

                string HeaderWrite = "";
                for (int i = 0; i < Motions[0].OutputRestrictions.Count; i++)
                {
                    for (int j = 0; j < Degrees; j++)
                    {
                        HeaderWrite = HeaderWrite + settings.Restrictions[i].Label + "^" + (j + 1).ToString() + ",";
                    }
                }

                for (var row = 1; row <= 100; row++)
                {
                    for (var col = 1; col <= 10; col++)
                    {
                        ew.Write($"row:{row}-col:{col}", col, row);
                    }
                }
            }
        */
        }

    public void PrintSpace()
    {
        TextWriter tw = new StreamWriter(Location(), true); 
        tw.WriteLine("");
        tw.Close();
    }
}

