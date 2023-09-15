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
//using untiy.mat


public enum Side
{
    right = 0,
    left = 1,
}
public enum Spell
{
    Nothing = 0,
    Fireball = 1,
    Flames = 2,
    SideParry = 3,
    UpParry = 4,
}
public class PythonTest : SerializedMonoBehaviour
{
    public static PythonTest instance;
    private void Awake() { instance = this; }

    public bool Active;
    public NNModel modelAsset;
    private Model runtimeModel;
    private IWorker worker;
    private IWorker CheckWorker;

    //public int FramesAgo;

    public int FramesAgoBuild;

    [Range(1,20)]public int PrintDecimals = 5;

    public FinalMotion CalculatedMotion;

    //public bool UsePreprocessing;
    public bool DoExport;
    public int ExcelMotions;

    public bool RunAccuracyTest;

    public List<Spell> SpellsToUse;


    public bool TrackEndOnly;

    public Athena A => Athena.instance;
    public MotionEditor ME => MotionEditor.instance;

    //public List<int> AllActiveMotions { get { return Enumerable.Range(0, A.MotionCount()).Where(motion => MotionsToUse[motion]).ToList(); } }

    [Button]
    public void TestModel()
    {
        PythonTest.instance = this;


        runtimeModel = ModelLoader.Load(modelAsset);
        CheckWorker = WorkerFactory.CreateWorker(WorkerFactory.Type.ComputePrecompiled, runtimeModel);

        float4 Guesses = float4.zero;


        List<float> FrameInputData = new List<float>();
        int TotalCount = 0;
        GuessLogging Logging = new GuessLogging();
        
        foreach (Spell SpellType in SpellsToUse)
        {
            //Debug.Log(SpellType);
            for (int i = 0; i < A.MovementCount(SpellType); i++)
            {
                for (int j = FramesAgoBuild; j < A.FrameCount(SpellType, i); j++)
                {
                     //Debug.Log("1: " + (j - FramesAgoBuild) + "  2: " + (j + 1));
                    //Debug.Log(Enumss[0] + "  " + Enumss[^1]);
                    //for (int l = 0; l < Enumss.Count; l++)
                        //Debug.Log(Enumss[l]);

                    List<AthenaFrame> AllInfos = Enumerable.Range(j - FramesAgoBuild, FramesAgoBuild + 1).Select(x => A.AtFrameInfo(SpellType, i, x)).ToList();
                    List<float> Inputs = FrameToValues(AllInfos);


                    //Debug.Log(Inputs.Count);
                    /*
                    for (int k = j - FramesAgoBuild + 1; k <= j; k++)
                    {
                        //Debug.Log(k + " of " + j);
                        AthenaFrame lastFrame = A.AtFrameInfo(SpellType, i, k - 1);
                        AthenaFrame info = A.AtFrameInfo(SpellType, i, k);
                        Inputs.AddRange(FrameToValues(new List<AthenaFrame>() { lastFrame, info }));
                    }
                    */


                    if (DoExport)
                    {
                        TotalCount++;
                        
                        //Debug.Log("TotalCount: " + TotalCount + " SpellType: " + SpellType + "  Movement: " + i + "  TopFrame: " + j);
                        if (TotalCount < ExcelMotions)
                        {
                            for (int x = 0; x < Inputs.Count; x++)
                            {
                                //if (TotalCount >= 34 && TotalCount <= 36 && SpellType == 1)
                                    //Debug.Log("Current: : " + (TotalCount - (10 - x)).ToString() + "val: " + Inputs[x]);
                                //if(Inputs[x] == -0.36851f)
                                    //Debug.Log("Movement: " + i  + "frame: " + j + "  TotalCount: : " + TotalCount + "  x: " + x + "  val: " + Inputs[x]);
                                FrameInputData.Add(Inputs[x]);
                                //debugString += " 'Inputs[" + x + "]':" + Inputs[x].ToString("f3");
                            }
                        }
                       
                        
                    }
                    if (RunAccuracyTest)
                    {
                        bool IsTrue = A.FrameWorks(SpellType, i, j) && SpellType == Spell.Fireball;
                        bool Guess = PredictState(Inputs);
                        bool Correct = Guess == IsTrue;
                        Logging.UpdateGuesses(Correct, IsTrue);
                    }
                }
            }
        }

        /*
        ///testing:
        for (int m = 0; m < CalculatedMotion.Motions.Count; m++)
        {
            //Debug.Log(SpellType);
            for (int f = FramesAgoBuild - 1; f < CalculatedMotion.Motions[m].Frames.Count; f++)
            {
                List<float> InvolvedInputs = new List<float>();
                for (int k = f - FramesAgoBuild + 1; k <= f; k++)
                {
                    Debug.Log(k + " of " + f);
                    InvolvedInputs.AddRange(CalculatedMotion.Motions[m].Frames[k].InputList());
                }
                Debug.Log(InvolvedInputs.Count);
                if (RunAccuracyTest)
                {
                    bool IsTrue = CalculatedMotion.Motions[m].Frames[f].Active;

                    bool Guess = PredictState(InvolvedInputs);
                    bool Correct = Guess == IsTrue;
                    Logging.UpdateGuesses(Correct, IsTrue);
                }

            }
        }
        */

        //Debug.Log(FrameInputData.Count);
        if (DoExport)
            SpreadSheet.OutputExcelInfo(FrameInputData.ToArray());

        if (RunAccuracyTest)
            Debug.Log(Logging.PercentSimpleString());
            Debug.Log(Logging.GuessCountString());
            Debug.Log(Logging.MotionCountString());
            Debug.Log(Logging.OutcomesCountString());
    }

    public string JSONDirectory { get { return "B:/GitProjects/NewMachineLearning/NewMachineLearning/WildfireLearning"; } }
    
    
    public List<float> FrameToValues(List<AthenaFrame> Frames)
    {
        List<float> FrameInputs = Frames.SelectMany(x => x.AsInputs()).ToList();
        FrameInputs = FrameInputs.Select(x => MathF.Round(x, PrintDecimals)).ToList();
        return FrameInputs;
        
    }
    
    public bool PredictState(List<float> Inputs)
    {
        // Load the NNModel
        runtimeModel = ModelLoader.Load(modelAsset);


        worker = WorkerFactory.CreateWorker(WorkerFactory.Type.ComputePrecompiled, runtimeModel);

        int ActiveInputCount = Inputs.Count / FramesAgoBuild;
        Tensor input = new Tensor(1, 1, FramesAgoBuild, ActiveInputCount, Inputs.ToArray());
        worker.Execute(input);
        Tensor output = worker.PeekOutput();
        bool predictedState = output[0] > 0.5f;
        input.Dispose();
        worker.Dispose();

        return predictedState;
    }

    public FinalMotion GetAllMotionList(Spell spell)
    {
        List<Motion> ReturnList = new List<Motion>();

        foreach(Spell SpellType in SpellsToUse)
        {
            bool CanBeTrue = spell == SpellType;
            for (int i = 0; i < A.MovementCount(SpellType); i++)
            {
                List<Frame> frames = new List<Frame>();
                for (int j = 0; j < A.FrameCount(SpellType, i); j++)
                {
                    AthenaFrame info = A.AtFrameInfo(SpellType, i, j);
                    bool State = A.FrameWorks(SpellType, i, j) && CanBeTrue;

                    List<float> FramesInfo = FrameToValues(new List<AthenaFrame>() { info });
                    
                    Frame frame = new Frame(FramesInfo.ToArray(), State);
                    frames.Add(frame);
                    if(i == 0 && j == 1 && SpellType == Spell.Fireball)
                    {
                        //Debug.Log(FramesInfo[0]);
                    }
                }
                Motion motion = new Motion(frames);
                ReturnList.Add(motion);
            }
        }

        return new FinalMotion(ReturnList);
    }

    [Button]
    public void ReloadJSON()
    {
        ReloadJSONType(ME.MotionType);
        //RunPythonScript.ExecutePythonScript(JSONDirectory + "/DeepLearningModel.py");
    }
    public void ReloadJSONType(Spell spell)
    {
        PythonTest.instance = this;
        CalculatedMotion = GetAllMotionList(spell);
        string directory = Path.Combine(JSONDirectory, spell.ToString() + ".json");
        //Debug.Log(directory);
        // Convert the ScriptableObject to a JSON string
        string json = JsonUtility.ToJson(CalculatedMotion);

        // Write the JSON string to a file
        File.WriteAllText(directory, json);
    }
    private void Update()
    {
        if (!Active)
            return;

        if (PastFrameRecorder.IsReady())
            GetPred();
    }

    public void GetPred()
    {
        List<AthenaFrame> frames = PastFrameRecorder.instance.GetFramesList(Side.right, FramesAgoBuild + 1);
        bool Pred = PredictState(FrameToValues(frames));
        //Debug.Log("returned: " + Pred);
        //DebugRestrictions.instance.SetSideColor(Side.right, Pred ? 1 : 0);
    }
    [System.Serializable]
    public class FinalMotion
    {
        [ListDrawerSettings(ShowIndexLabels = true)]public List<Motion> Motions;
        public FinalMotion(List<Motion> Motions)
        {
            this.Motions = Motions;
        }
    }

    [System.Serializable]
    public class Motion
    {
        [ListDrawerSettings(ShowIndexLabels = true)] public List<Frame> Frames;
        public Motion(List<Frame> Frames)
        {
            this.Frames = Frames;
        }
    }
    [System.Serializable]
    public class Frame
    {
        public string[] FrameInfo;
        public bool Active;


        /*
        public AthenaFrame info;
        public float TimeSinceLast;
        public Frame(AthenaFrame info, bool Active, float TimeSinceLast)
        {
            this.info = info;
            this.Active = Active;
            this.TimeSinceLast = TimeSinceLast;
        }
        */

        public Frame(float[] FrameInfo, bool Active)
        {
            List<float> RealFrameInfo = FrameInfo.ToList();
            
            this.FrameInfo = RealFrameInfo.Select(x => x.ToString()).ToArray();
            this.Active = Active;
            
        }

        public List<float> InputList()
        {
            List<float> newList = FrameInfo.Select(x => float.Parse(x)).ToList();
            //newList.Add(float.Parse(Time));
            return newList;
        }
    }



    ///pass values
}
public struct GuessLogging
{
    public void UpdateGuesses(bool Correct, bool IsTrue)
    {
        Guesses.w += (Correct && IsTrue) ? 1f : 0f;
        Guesses.x += (!Correct && IsTrue) ? 1f : 0f;
        Guesses.y += (Correct && !IsTrue) ? 1f : 0f;
        Guesses.z += (!Correct && !IsTrue) ? 1f : 0f;
    }

    public float4 Guesses;
    public GuessLogging(float4 Guesses)
    {
        this.Guesses = Guesses;
    }
    public float Total() { return Guesses.w + Guesses.x + Guesses.y + Guesses.z; }
    public string PercentComplexString() { return "Correct: " + CorrectPercent() + "  Wrong: " + InCorrectPercent() + "  True: " + OnTruePercent() + "  False: " + OnFalsePercent(); }
    public string PercentSimpleString() { return "CorrectOnTrue: " + CorrectOnTruePercent() + "/" + IncorrectOnTruePercent() + "   CorrectOnFalse: " + CorrectOnFalsePercent() + "/" + IncorrectOnFalsePercent(); }

    public string MotionCountString() { return "True/False Frames: " + CorrectOnTrue() + IncorrectOnTrue() + "/" + CorrectOnFalse() +IncorrectOnFalse(); }
    public string GuessCountString() { return "True/False Guesses: " + CorrectOnTrue() + IncorrectOnFalse() + "/" + CorrectOnFalse() + IncorrectOnTrue(); }
    public string OutcomesCountString() { return "CorrectOnTrue: " + CorrectOnTrue() + "  IncorrectOnTrue: " + IncorrectOnTrue() + "  CorrectOnFalse: " + CorrectOnFalse() + "  IncorrectOnFalse: " + IncorrectOnFalse(); }

    public float CorrectOnTrue() { return Guesses.w; }
    public float IncorrectOnTrue() { return Guesses.x; }
    public float CorrectOnFalse() { return Guesses.y; }
    public float IncorrectOnFalse() { return Guesses.z; }

    public float CorrectOnTruePercent() { return CorrectOnTrue() / (CorrectOnTrue() + IncorrectOnTrue()) * 100f; }
    public float IncorrectOnTruePercent() { return IncorrectOnTrue() / (CorrectOnTrue() + IncorrectOnTrue()) * 100f; }
    public float CorrectOnFalsePercent() { return CorrectOnFalse() / (CorrectOnFalse() + IncorrectOnFalse()) * 100f; }
    public float IncorrectOnFalsePercent() { return IncorrectOnFalse() / (CorrectOnFalse() + IncorrectOnFalse()) * 100f; }


    public float CorrectPercent() { return (CorrectOnTrue() + CorrectOnFalse()) / Total() * 100f; }
    public float InCorrectPercent() { return (IncorrectOnTrue() + IncorrectOnFalse()) / Total() * 100f; }
    public float OnTruePercent() { return (CorrectOnTrue() + IncorrectOnTrue()) / Total() * 100f; }
    public float OnFalsePercent() { return (CorrectOnFalse() + IncorrectOnFalse()) / Total() * 100f; }
}