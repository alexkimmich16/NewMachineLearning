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
namespace Athena
{
    public class Runtime : MonoBehaviour
    {
        public static Runtime instance;
        private void Awake() { instance = this; }


        public int FramesAgoBuild;
        public NNModel modelAsset;
        public Model runtimeModel { get { return ModelLoader.Load(modelAsset); } }
        private IWorker worker;

        public delegate void StateHandler(Side side, int state);
        public static event StateHandler StateChange;

        public bool PredictState(List<float> Inputs)
        {
            // Load the NNModel
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

        public void RunModel()
        {
            ///AS ONE FOR NOW
            for (int i = 0; i < 2; i++)
            {
                Side side = (Side)i;
                List <AthenaFrame> Frame = PastFrameRecorder.instance.GetFramesList(side, FramesAgoBuild);
                bool State = PredictState(Frame.SelectMany(x => x.AsInputs()).ToList());
                StateChange?.Invoke(side, State ? 1 : 0);
            }
            //run model with controller inputs
            //set color of controllers
        }
    }
}

