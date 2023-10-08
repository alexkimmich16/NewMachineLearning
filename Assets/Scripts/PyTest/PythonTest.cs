using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Barracuda;
using RestrictionSystem;
using Sirenix.OdinInspector;
using System.Linq;
using System.IO;
using System;
using Unity.Mathematics;
using Athena;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

//using untiy.mat



public class PythonTest : SerializedMonoBehaviour
{
    public static PythonTest instance;
    private void Awake() { instance = this; }

    [Range(1,20)]public const int PrintDecimals = 5;

    public FinalMotion CalculatedMotion;

    //public bool UsePreprocessing;
    public bool DoExport;
    public int ExcelMotions;

    public bool RunAccuracyTest;


    public bool TrackEndOnly;

    public bool differentLastFrame;

    public Runtime R = Runtime.instance;
    public MotionEditor ME = MotionEditor.instance;

    //public List<int> AllActiveMotions { get { return Enumerable.Range(0, Cycler.MotionCount()).Where(motion => MotionsToUse[motion]).ToList(); } }
    public static string JSONDirectory { get {return Path.Combine(Path.GetDirectoryName(Application.dataPath), "WildfireLearning"); } }
    
   
    
    [Button]public void TestModel()
    {
        PythonTest.instance = this;
        float4 Guesses = float4.zero;

        List<float> FrameInputData = new List<float>();
        int TotalCount = 0;
        GuessLogging Logging = new GuessLogging();
        
        foreach (Spell SpellType in Cycler.Movements.Keys)
        {
            //Debug.Log(SpellType);
            for (int i = 0; i < Cycler.MovementCount(SpellType); i++)
            {
                for (int j = Runtime.FramesAgoBuild; j < Cycler.FrameCount(SpellType, i); j++)
                {
                     //Debug.Log("1: " + (j - FramesAgoBuild) + "  2: " + (j + 1));
                    //Debug.Log(Enumss[0] + "  " + Enumss[^1]);
                    //for (int l = 0; l < Enumss.Count; l++)
                        //Debug.Log(Enumss[l]);

                    List<AthenaFrame> AllInfos = Enumerable.Range(j - Runtime.FramesAgoBuild, Runtime.FramesAgoBuild).Select(x => Cycler.AtFrameInfo(SpellType, i, x)).ToList();
                    List<float> Inputs = Runtime.FrameToValues(AllInfos);


                    if (DoExport)
                    {
                        TotalCount++;
                        
                        //Debug.Log("TotalCount: " + TotalCount + " SpellType: " + SpellType + "  Movement: " + i + "  TopFrame: " + j);
                        if (TotalCount < ExcelMotions)
                        {
                            for (int x = 0; x < Inputs.Count; x++)
                            {
                                FrameInputData.Add(Inputs[x]);
                                //debugString += " 'Inputs[" + x + "]':" + Inputs[x].ToString("f3");
                            }
                        }
                       
                        
                    }
                    if (RunAccuracyTest)
                    {
                        int IsTrue = (Cycler.FrameWorks(SpellType, i, j) && SpellType == ME.MotionType) ? 1 : 0;
                        int Guess = R.PredictState(Inputs, SpellType);
                        bool Correct = Guess == IsTrue;
                        Logging.UpdateGuesses(Correct, IsTrue);


                    }
                }
            }
        }

        //Debug.Log(FrameInputDatCycler.Count);
        if (DoExport)
            SpreadSheet.OutputExcelInfo(FrameInputData.ToArray());

        if (RunAccuracyTest)
            Debug.Log(Logging.PercentSimpleString());
            Debug.Log(Logging.GuessCountString());
            Debug.Log(Logging.MotionCountString());
            Debug.Log(Logging.OutcomesCountString());
            Debug.Log(Logging.PercentComplexString());
    }
    
    [Button]public void ReloadJSON()
    {
        foreach(Spell spell in Cycler.Movements.Keys)
        {
            PythonTest.instance = this;
            CalculatedMotion = GetAllMotionList(spell);
            string directory = Path.Combine(JSONDirectory, spell.ToString() + ".json");

            // Convert the ScriptableObject to a JSON string
            string json = JsonUtility.ToJson(CalculatedMotion);

            // Write the JSON string to a file
            File.WriteAllText(directory, json);
        }
        //RunPythonScript.ExecutePythonScript(JSONDirectory + "/DeepLearningModel.py");
    }


    public FinalMotion GetAllMotionList(Spell spellType)
    {
        List<Motion> ReturnList = new List<Motion>();
        List<Frame> currentFrames = new List<Frame>();
        Cycler.FrameLoop((spell, motionIndex, frameIndex, frame) => {

            float[] FramesInfo = Runtime.FrameToValues(new List<AthenaFrame>() { frame }).ToArray();
            bool MotionActiveState = spell == spellType && Cycler.FrameWorks(spell, motionIndex, frameIndex);

            int LastFrame = currentFrames.Count > 0 ? currentFrames[^1].State : 0;
            currentFrames.Add(new Frame(FramesInfo, GetState(MotionActiveState, LastFrame)));
        },
            () => { },//spell change
            () => { ReturnList.Add(new Motion(currentFrames)); currentFrames = new List<Frame>(); });//new motion
        return new FinalMotion(ReturnList);


        int GetState(bool State, int LastFrame)
        {
            if(spellType == Spell.Fireball)
            {
                if (State == false)
                {
                    if (LastFrame == 1 && differentLastFrame)
                        return 2;
                    else
                        return 0;
                }
                else
                    return 1;

            }
            else
                Debug.LogError("unfamiliar spelltype of type: " + spellType.ToString());


            return 100;
        }
    }

    
    
    
    [System.Serializable]public class FinalMotion
    {
        [ListDrawerSettings(ShowIndexLabels = true)]public List<Motion> Motions;
        public FinalMotion(List<Motion> Motions) { this.Motions = Motions; }
    }
    [System.Serializable]public class Motion
    {
        [ListDrawerSettings(ShowIndexLabels = true)] public List<Frame> Frames;
        public Motion(List<Frame> Frames) { this.Frames = Frames; }
    }
    [System.Serializable]public class Frame
    {
        public string[] FrameInfo;
        public int State;

        public Frame(float[] FrameInfo, int State)
        {
            List<float> RealFrameInfo = FrameInfo.ToList();
            
            this.FrameInfo = RealFrameInfo.Select(x => x.ToString()).ToArray();
            this.State = State;
            
        }

        public List<float> InputList()
        {
            List<float> newList = FrameInfo.Select(x => float.Parse(x)).ToList();
            //newList.Add(float.Parse(Time));
            return newList;
        }
    }
}



/*
public string scriptPath { get { return Path.Combine(JSONDirectory, "DeepLearningModel.py"); } }
    private string pythonPath = "python"; // or "python3" for some systems   
[Button]public void RunPythonMachineLearning()
   {
       ProcessStartInfo start = new ProcessStartInfo();
       start.FileName = pythonPath;
       start.Arguments = string.Format("\"{0}\"", scriptPath);
       start.UseShellExecute = false; // Do not use OS shell
       start.CreateNoWindow = true; // We don't need a new window
       start.RedirectStandardOutput = true; // Any output, generated by application will be redirected back
       start.RedirectStandardError = true; // Any error in standard output will be redirected back

       using (Process process = Process.Start(start))
       {
           using (System.IO.StreamReader reader = process.StandardOutput)
           {
               string stderr = process.StandardError.ReadToEnd(); // Here are the exceptions from our Python script
               string result = reader.ReadToEnd(); // Here is the result of StdOut(for example: print "test")

               if (!string.IsNullOrEmpty(stderr))
               {
                   Debug.LogError("Python Error: " + stderr);
               }

               Debug.Log(result);
           }
       }
   }
   */