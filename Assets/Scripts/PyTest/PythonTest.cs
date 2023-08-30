using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Barracuda;
using RestrictionSystem;
using Sirenix.OdinInspector;
using System.Linq;
using System.IO;
public class PythonTest : SerializedMonoBehaviour
{
    public bool Active;
    public NNModel modelAsset;
    private Model runtimeModel;
    private IWorker worker;
    private IWorker CheckWorker;

    //public int FramesAgo;

    public int FramesAgoBuild;
    public int InputsPerFrameBuild = 13;

    MovementControl MC => MovementControl.instance;

    [FoldoutGroup("Info")] public bool UseFalseMotions;
    public FinalMotion CalculatedMotion;

    public bool UsePreprocessing;

    [Button]
    public void TestModel()
    {
        runtimeModel = ModelLoader.Load(modelAsset);
        CheckWorker = WorkerFactory.CreateWorker(WorkerFactory.Type.ComputePrecompiled, runtimeModel);

        Vector2 TrueFalse = new Vector2(0, 0);
        for (int i = 0; i < MC.MovementCount(Spell.Fireball); i++)
        {
            for (int j = FramesAgoBuild; j < MC.FrameCount(Spell.Fireball, i); j++)
            {
                List<SingleInfo> Info = new List<SingleInfo>();
                string BuildString = "";
                for (int k = j - FramesAgoBuild; k < j + 1; k++)
                {
                    BuildString = BuildString + k + " ";
                    
                    Info.Add(MC.AtFrameInfo(Spell.Fireball, i, k));
                }
                if (i == 0)
                {
                    //Debug.Log("j: " + j + "  Includes:  " + BuildString);
                }
                    List<float> Inputs = GetInputs(Info);

                if (i == 0)
                {
                    //Debug.Log("  Inputs: " + Inputs.Count);
                }


                //Debug.Log(Inputs.Count);
                bool State = MC.FrameWorks(Spell.Fireball, i, j);
                //Debug.Log("j: " + j + "  Includes:  " + BuildString + "  Inputs: " + Inputs.Count);
                // Debugging section
                if (i == 0)
                {
                    string debugString = "frame: " + j;
                    for (int x = 0; x < Inputs.Count; x++)
                    {
                        debugString += " 'Inputs[" + x + "]':" + Inputs[x].ToString("f3");
                    }
                   // Debug.Log(debugString);
                }

                Tensor CheckWorks = new Tensor(1, 1, FramesAgoBuild, InputsPerFrameBuild, Inputs.ToArray());

                // Execute the neural network with the given input
                CheckWorker.Execute(CheckWorks);

                // Fetch the result
                Tensor output = CheckWorker.PeekOutput();

                // Convert the output to a boolean
                bool predictedState = output[0] > 0.5f;

                bool Correct = predictedState == State;

                TrueFalse = Correct ? new Vector2(TrueFalse.x + 1, TrueFalse.y) : new Vector2(TrueFalse.x, TrueFalse.y + 1);
                //Debug.Log("Output: " + output[0]);

                CheckWorks.Dispose();
                output.Dispose();
            }
            //return;
            //return;
        }

        Debug.Log("Accuracy: " + (TrueFalse.x / (TrueFalse.x +TrueFalse.y)).ToString("f3") + "Correct: " + TrueFalse.x + "  Incorrect: " + TrueFalse.y);
    }

    public string JSONDirectory { get { return "B:/WildfireLearning"; } }
    [Button]
    public void ReloadJSON()
    {
        ReloadJSONType(Spell.Fireball);
    }
    public void ReloadJSONType(Spell spell)
    {
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
        bool Pred = PredictGestureState(Side.right);
        //Debug.Log("returned: " + Pred);
        DebugRestrictions.instance.SetSideColor(Side.right, Pred ? 1 : 0);
    }

    private List<float> GetInputs(List<SingleInfo> Frames)
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
                
                FrameInputs.AddRange(new List<float>() { Frame.HandPos.x, Frame.HandPos.y, Frame.HandPos.z, Frame.HandRot.x / 360f, Frame.HandRot.y / 360f, Frame.HandRot.z / 360f, Time() });
            }
            float Time() { return Frames[i + 1].SpawnTime - Frames[i].SpawnTime; }
        }
        return FrameInputs;
        
    }

    public bool PredictGestureState(Side side)
    {
        // Load the NNModel
        runtimeModel = ModelLoader.Load(modelAsset);

        // Create a Barracuda worker (responsible for executing the model)
        worker = WorkerFactory.CreateWorker(WorkerFactory.Type.ComputePrecompiled, runtimeModel);

        List<SingleInfo> frames = PastFrameRecorder.instance.GetFramesList(side, FramesAgoBuild + 1);
        float[] Inputs = new float[0];
        for (int i = 1; i < FramesAgoBuild + 1; i++)
        {
            //Debug.Log("i: " + i + "i+1: " + (i + 1));
            List<float> FrameInputs = GetInputs(new List<SingleInfo>() { frames[i - 1], frames[i] });
            //Debug.Log(FrameInputs.Count);
            Inputs = Inputs.Concat(FrameInputs.ToArray()).ToArray();
            //Debug.Log(i + "inputs: " + Inputs);
        }
        //Debug.Log("Inputs: " + Inputs.Length);
        // Create input tensor with the correct shape (1, sequenceLength, 12)
        Tensor input = new Tensor(1, 1, FramesAgoBuild, InputsPerFrameBuild, Inputs);

        // Execute the neural network with the given input
        worker.Execute(input);

        // Fetch the result
        Tensor output = worker.PeekOutput();

        // Convert the output to a boolean
        bool predictedState = output[0] > 0.5f;
        //Debug.Log("Output: " + output[0]);
        // Dispose of the worker and input tensor when done
        input.Dispose();
        worker.Dispose();

        return predictedState;
    }

    public FinalMotion GetAllMotionList(Spell spell)
    {
        List<int> UseMotions = Enumerable.Range(0, MC.MotionCount()).ToList();
        if (!UseFalseMotions)
            UseMotions.Remove(0);


        List<Motion> ReturnList = new List<Motion>();

        foreach(int SpellType in UseMotions)
        {
            bool CanBeTrue = (int)spell == SpellType;
            for (int i = 0; i < MC.MovementCount((Spell)SpellType); i++)
            {
                List<Frame> frames = new List<Frame>();
                for (int j = 1; j < MC.FrameCount((Spell)SpellType, i); j++)
                {
                    SingleInfo lastFrame = MC.AtFrameInfo((Spell)SpellType, i, j - 1);
                    SingleInfo info = MC.AtFrameInfo((Spell)SpellType, i, j);
                    bool State = MC.FrameWorks((Spell)SpellType, i, j) && CanBeTrue;

                    List<float> FramesInfo = GetInputs(new List<SingleInfo>() { lastFrame, info });
                    
                    Frame frame = new Frame(FramesInfo.ToArray(), State, TimeSinceLast());
                    frames.Add(frame);


                    float TimeSinceLast()
                    {
                        if (j > 0)
                        {
                            float MyFrameTime = MC.AtFrameInfo((Spell)SpellType, i, j).SpawnTime;
                            float lastFrameTime = MC.AtFrameInfo((Spell)SpellType, i, j - 1).SpawnTime;
                            return MyFrameTime - lastFrameTime;
                        }
                        return 0f;
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
        public List<Motion> Motions;
        public FinalMotion(List<Motion> Motions)
        {
            this.Motions = Motions;
        }
    }

    [System.Serializable]
    public class Motion
    {
        public List<Frame> Frames;
        public Motion(List<Frame> Frames)
        {
            this.Frames = Frames;
        }
    }
    [System.Serializable]
    public class Frame
    {
        public float[] FrameInfo;
        public bool Active;
        public float Time;
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

        public Frame(float[] FrameInfo, bool Active, float Time)
        {
            this.FrameInfo = FrameInfo;
            this.Active = Active;
            this.Time = Time;
        }
    }



    ///pass values
}
