using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Sirenix.OdinInspector;
using RestrictionSystem;
using OfficeOpenXml;
using System.Diagnostics;
public class SpreadSheet : SerializedMonoBehaviour
{
    public static SpreadSheet instance;
    private void Awake() { instance = this; }

    public static string ReadExcelCell(int row, int column)
    {
        FileInfo existingFile = new FileInfo(DegreeLocation2());
        ExcelPackage package = new ExcelPackage(existingFile);
        ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
        return worksheet.Cells[row + 1, column + 1].Value.ToString();
    }
    public static string DegreeLocation2() { return Application.dataPath + "/SpreadSheets/Tester.XLSX"; }

    //[Button(ButtonSizes.Small)]
    public void OpenExcel()
    {
        Process.Start(DegreeLocation2());
    }

    //[Button(ButtonSizes.Small)]
    public void PrintDegreeStats()
    {
        RegressionSystem RS = gameObject.GetComponent<RegressionSystem>();
        
        MotionRestriction settings = RS.UploadRestrictions;
        List<SingleFrameRestrictionValues> Motions = GetComponent<BruteForce>().GetRestrictionsForMotions(RS.CurrentMotion, settings);
        UnityEngine.Debug.Log("Print: " + RS.CurrentMotion.ToString());// + "  First: " + Motions[0].OutputRestrictions[0]
        int Degrees = RS.EachTotalDegree;
        
        FileInfo existingFile = new FileInfo(DegreeLocation2());
        ExcelPackage package = new ExcelPackage(existingFile);
        ExcelWorksheet worksheet = package.Workbook.Worksheets[0];

        //headers
        for (int i = 0; i < Motions[0].OutputRestrictions.Count; i++)
            for (int j = 0; j < Degrees; j++)
                worksheet.Cells[1, (i * Degrees) + j + 1].Value = settings.Restrictions[i].Label + "^" + (j + 1).ToString() + ",";
        worksheet.Cells[1, (Degrees * Motions[0].OutputRestrictions.Count) + 1].Value = "States";

        //Stats
        for (int i = 0; i < Motions.Count; i++)
        {
            for (int j = 0; j < Motions[0].OutputRestrictions.Count; j++)
            {
                for (int k = 0; k < Degrees; k++)
                {
                    float DegreeValue = Mathf.Pow(Motions[i].OutputRestrictions[j], k + 1);
                    float Value = DegreeValue > 0.01f ? DegreeValue : 0.01f;
                    worksheet.Cells[i + 2, (j * Degrees) + k + 1].Value = Value;
                }
            }
            worksheet.Cells[i + 2, (Motions[0].OutputRestrictions.Count * Degrees) + 1].Value = Motions[i].AtMotionState ? 1 : 0;
        }
            
                

        package.Save();

        OpenExcel();
    }

    /*
    [Button(ButtonSizes.Small)]
    public void GetPos()
    {
        Debug.Log(ReadExcelCell(0,0));
    }
    */
}

/*
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
*/