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

    public bool ModelSequenceAll;

    [Button]public void RunFullModelSequence()
    {
        GetComponent<Lock>().LockAll();
        ReloadJSON();
        ExecutePythonScript();
    }

    //public List<int> AllActiveMotions { get { return Enumerable.Range(0, Cycler.MotionCount()).Where(motion => MotionsToUse[motion]).ToList(); } }

    #region RunModel
    public static string JSONDirectory { get { return Path.Combine(Path.GetDirectoryName(Application.dataPath), "WildfireLearning"); } }
    public static string pythonScriptPath { get { return Path.Combine(JSONDirectory, "DeepLearningModel.py"); } }
    public static string pythonVersionPath { get { return Path.Combine(JSONDirectory, "venv\\Scripts\\python.exe"); } }

    [Button]
    public void ExecutePythonScript()
    {
        int arg = Runtime.FramesAgoBuild;
        int SpellBakeType = ModelSequenceAll ? 3 : (int)ME.MotionType - 1;
        // Log the paths just for verification
        //Debug.Log("JSON Directory: " + JSONDirectory);
        Debug.Log("Python Script Path: " + pythonScriptPath);
        Debug.Log("Python Version Path: " + pythonVersionPath);

        ProcessStartInfo start = new ProcessStartInfo
        {
            FileName = pythonVersionPath,
            Arguments = $"\"{pythonScriptPath}\" {arg} {SpellBakeType}",
            UseShellExecute = true,  // Use the system shell to start the process
        };

        Process process = new Process { StartInfo = start };
        process.Start();
    }
    #endregion
    #region copyfolder
    public static string dataLocation { get { return Path.Combine(Application.dataPath, "Scripts/AthenaExport"); } }
    [Button]public void TransferData()
    {
        // 1. Extract root GitProjects directory from dataLocation
        DirectoryInfo dataDirInfo = new DirectoryInfo(dataLocation);
        DirectoryInfo gitProjectsDir = dataDirInfo.Parent;

        // Navigate up to GitProjects folder
        while (gitProjectsDir != null && gitProjectsDir.Name != "GitProjects")
        {
            gitProjectsDir = gitProjectsDir.Parent;
        }

        // If we can't find the GitProjects directory, exit early
        if (gitProjectsDir == null)
        {
            Debug.LogError("Could not find GitProjects directory.");
            return;
        }

        // 2. Append the relative path to derive the exportLocation
        string exportLocation = Path.Combine(gitProjectsDir.FullName, @"WildfireVR\Assets\Scripts\AthenaExport");

        // Ensure exportLocation exists or create it
        if (!Directory.Exists(exportLocation))
        {
            Directory.CreateDirectory(exportLocation);
        }

        // 3. Copy contents from dataLocation to exportLocation
        foreach (string file in Directory.GetFiles(dataLocation))
        {
            string destFile = Path.Combine(exportLocation, Path.GetFileName(file));
            File.Copy(file, destFile, true); // This will overwrite files with the same name in exportLocation
        }

        Debug.Log($"Copied files from {dataLocation} to {exportLocation}");
    }
    #endregion


    /*
    public void TestModel()
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
                    List<float> Inputs = Runtime.instance.FrameToValues(AllInfos);


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
                        //int IsTrue = (Cycler.FrameWorks(SpellType, i, j) && SpellType == ME.MotionType) ? 1 : 0;
                        //int Guess = R.PredictState(Inputs, SpellType);
                        //bool Correct = Guess == IsTrue;
                        //Logging.UpdateGuesses(Correct, IsTrue);


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
    */

    [Button]public void ReloadJSON()
    {
        foreach(Spell spell in Cycler.Movements.Keys)
        {
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
            if (spell == spellType || Cycler.MotionWorks(spell, motionIndex))
            {
                float[] FramesInfo = R.FrameToValues(new List<AthenaFrame>() { frame }).ToArray();
                bool FrameState = spell == spellType && Cycler.FrameWorks(spell, motionIndex, frameIndex);
                int LastFrame = currentFrames.Count > 0 ? currentFrames[^1].State : 0;
                currentFrames.Add(new Frame(FramesInfo, GetState(FrameState, LastFrame)));
            }
            
            
            
        },
            () => { },//spell change
            () => { if (currentFrames.Count > 0) { ReturnList.Add(new Motion(currentFrames)); } currentFrames = new List<Frame>(); });//new motion
        return new FinalMotion(ReturnList);


        int GetState(bool State, int LastFrame)
        {
            if (spellType == Spell.Fireball)
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
            else if (spellType == Spell.Flames)
            {
                return State ? 1 : 0;
            }
            else if (spellType == Spell.Parry)
            {
                return State ? 1 : 0;
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