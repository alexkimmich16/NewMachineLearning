using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Barracuda;
using RestrictionSystem;
using Sirenix.OdinInspector;
using System.Linq;
using System.IO;
using System;
//using untiy.mat
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
    public int InputsPerFrameBuild = 13;

    [Range(1,20)]public int PrintDecimals = 5;

    public MovementControl MC;

    public FinalMotion CalculatedMotion;

    public bool UsePreprocessing;

    public int ExcelMotions;

    public bool RunAccuracyTest;

    public List<Spell> SpellsToUse;

    //public List<int> AllActiveMotions { get { return Enumerable.Range(0, MC.MotionCount()).Where(motion => MotionsToUse[motion]).ToList(); } }

    [Button]
    public void TestModel()
    {
        PythonTest.instance = this;


        runtimeModel = ModelLoader.Load(modelAsset);
        CheckWorker = WorkerFactory.CreateWorker(WorkerFactory.Type.ComputePrecompiled, runtimeModel);

        Vector2 TrueFalse = new Vector2(0, 0);


        List<float> FrameInputData = new List<float>();
        int TotalCount = 0;
        foreach (Spell SpellType in SpellsToUse)
        {
            //Debug.Log(SpellType);
            for (int i = 0; i < MC.MovementCount(SpellType); i++)
            {
                for (int j = FramesAgoBuild; j < MC.FrameCount(SpellType, i); j++)
                {
                    List<SingleInfo> Info = new List<SingleInfo>();
                    string BuildString = "";
                    for (int k = j - FramesAgoBuild; k < j + 1; k++)
                    {
                        BuildString = BuildString + k + " ";

                        Info.Add(MC.AtFrameInfo(SpellType, i, k));
                    }
                    List<float> Inputs = FrameToValues(Info);
                    Debug.Log(Inputs.Count);
                    if (i == 0)
                    {
                        //Debug.Log("  Inputs: " + Inputs.Count);
                    }


                    //Debug.Log(Inputs.Count);
                    bool State = MC.FrameWorks(SpellType, i, j);
                    //if (i == 0 && SpellType == 1)
                        //Debug.Log(BuildString);
                        //Debug.Log("Start: " + 1 + "  finish: " + );
                        //SpellType == 1 && i < 10 && !ExportTriggered
                    if (true)
                    {
                        TotalCount++;
                        
                        //Debug.Log("TotalCount: " + TotalCount + " SpellType: " + SpellType + "  Movement: " + i + "  TopFrame: " + j);
                        if (TotalCount < ExcelMotions)
                        {
                            for (int x = 0; x < Inputs.Count; x++)
                            {
                                //if (TotalCount >= 34 && TotalCount <= 36 && SpellType == 1)
                                    //Debug.Log("Current: : " + (TotalCount - (10 - x)).ToString() + "val: " + Inputs[x]);
                                if(Inputs[x] == -0.36851f)
                                    Debug.Log("Movement: " + i  + "frame: " + j + "  TotalCount: : " + TotalCount + "  x: " + x + "  val: " + Inputs[x]);
                                FrameInputData.Add(Inputs[x]);
                                //debugString += " 'Inputs[" + x + "]':" + Inputs[x].ToString("f3");
                            }
                        }
                       
                        
                    }
                    if (RunAccuracyTest)
                    {
                        bool Prediction = PredictState(Info);
                        bool Correct = Prediction == State;
                        TrueFalse = Correct ? new Vector2(TrueFalse.x + 1, TrueFalse.y) : new Vector2(TrueFalse.x, TrueFalse.y + 1);
                    }
                    
                }
            }
        }
        //Debug.Log(FrameInputData.Count);
        SpreadSheet.OutputExcelInfo(FrameInputData.ToArray());

        if(RunAccuracyTest)
            Debug.Log("Accuracy: " + (TrueFalse.x / (TrueFalse.x +TrueFalse.y)).ToString("f3") + "Correct: " + TrueFalse.x + "  Incorrect: " + TrueFalse.y);
    }

    public string JSONDirectory { get { return "B:/GitProjects/NewMachineLearning/NewMachineLearning/WildfireLearning"; } }
    [Button]
    public void ReloadJSON()
    {
        ReloadJSONType(Spell.Fireball);
    }
    public void ReloadJSONType(Spell spell)
    {
        PythonTest.instance = this;
        CalculatedMotion = GetAllMotionList(spell);
        string directory = Path.Combine(JSONDirectory, spell.ToString() + ".json");
        Debug.Log(directory);
        // Convert the ScriptableObject to a JSON string
        string json = JsonUtility.ToJson(CalculatedMotion);

        // Write the JSON string to a file
        File.WriteAllText(directory, json);
    }
    private void Update()
    {
        if (!Active)
            return;
        
        if(PastFrameRecorder.IsReady())
            GetPred();
    }

    public void GetPred()
    {
        List<SingleInfo> frames = PastFrameRecorder.instance.GetFramesList(Side.right, FramesAgoBuild + 1);
        bool Pred = PredictState(frames);
        //Debug.Log("returned: " + Pred);
        DebugRestrictions.instance.SetSideColor(Side.right, Pred ? 1 : 0);
    }

    private List<float> FrameToValues(List<SingleInfo> Frames)
    {
        List<float> FrameInputs = new List<float>();

        for (int i = 0; i < Frames.Count - 1; i++)
        {
            //Debug.Log("I: " + i);
            if (UsePreprocessing)
            {
                FrameInputs.AddRange(RestrictionManager.instance.GetRestrictionValues(Frames[i], Frames[i+1], Spell.Fireball));
                FrameInputs.Add(Time());
            }
            else
            {
                SingleInfo Frame = Frames[i];
                
                //FrameInputs.AddRange(new List<float>() { Frame.HandPos.x, Frame.HandPos.y, Frame.HandPos.z, Frame.HandRot.x / 360f, Frame.HandRot.y / 360f, Frame.HandRot.z / 360f });
                FrameInputs.AddRange(new List<float>() { Frame.HandPos.x, Frame.HandPos.y, Frame.HandPos.z, Frame.HandRot.x / 360f, Frame.HandRot.y / 360f, Frame.HandRot.z / 360f, Time() });
            }
            float Time() { return Frames[i + 1].SpawnTime - Frames[i].SpawnTime; }
        }
        //Debug.Log(FrameInputs[0]);
        FrameInputs = FrameInputs.Select(x => MathF.Round(x, PrintDecimals)).ToList();
        //Debug.Log(FrameInputs[0]);
        return FrameInputs;
        
    }
    public bool PredictState(List<SingleInfo> frames)
    {
        // Load the NNModel
        runtimeModel = ModelLoader.Load(modelAsset);
        worker = WorkerFactory.CreateWorker(WorkerFactory.Type.ComputePrecompiled, runtimeModel);

        //List<SingleInfo> frames = PastFrameRecorder.instance.GetFramesList(side, FramesAgoBuild + 1);
        float[] Inputs = FrameToValues(frames).ToArray();

        /*
        float[] Inputs = new float[0];
        for (int i = 1; i < FramesAgoBuild + 1; i++)
        {
            List<float> FrameInputs = FrameToValues(new List<SingleInfo>() { frames[i - 1], frames[i] });
            Inputs = Inputs.Concat(FrameInputs.ToArray()).ToArray();
        }
        */
        Tensor input = new Tensor(1, 1, FramesAgoBuild, InputsPerFrameBuild, Inputs);
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
            for (int i = 0; i < MC.MovementCount(SpellType); i++)
            {
                
                List<Frame> frames = new List<Frame>();
                //if (i == 0 && SpellType == Spell.Fireball)
                    //Debug.Log(MC.FrameCount(SpellType, i));
                for (int j = 1; j < MC.FrameCount(SpellType, i); j++)
                {
                    //if(i==0&&SpellType == Spell.Fireball)
                        //Debug.Log(j);
                    SingleInfo lastFrame = MC.AtFrameInfo(SpellType, i, j - 1);
                    SingleInfo info = MC.AtFrameInfo(SpellType, i, j);
                    bool State = MC.FrameWorks(SpellType, i, j) && CanBeTrue;



                    List<float> FramesInfo = FrameToValues(new List<SingleInfo>() { lastFrame, info });
                    
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
        public string Time;


        /*
        public SingleInfo info;
        public float TimeSinceLast;
        public Frame(SingleInfo info, bool Active, float TimeSinceLast)
        {
            this.info = info;
            this.Active = Active;
            this.TimeSinceLast = TimeSinceLast;
        }
        */

        public Frame(float[] FrameInfo, bool Active)
        {
            this.Time = FrameInfo[FrameInfo.Length - 1].ToString();

            List<float> RealFrameInfo = FrameInfo.ToList();
            RealFrameInfo.RemoveAt(RealFrameInfo.Count - 1);
            
            this.FrameInfo = RealFrameInfo.Select(x => x.ToString()).ToArray();
            this.Active = Active;
            
        }
    }



    ///pass values
}
