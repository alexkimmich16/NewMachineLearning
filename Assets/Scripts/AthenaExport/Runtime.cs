using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Barracuda;
using Sirenix.OdinInspector;
using System.Linq;
using System.IO;
using System;
using Unity.Mathematics;

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
}
namespace Athena
{
    public delegate void AnonymousSpellStateEvent(Side side, int state);
    public delegate void SpellStateEvent(Spell spell, Side side, int state);
    public class Runtime : SerializedMonoBehaviour
    {
        public static Runtime instance;
        private void Awake() { instance = this; }

        public const int FramesAgoBuild = 2;
        public Model GetModel(Spell spell)
        {
            if(Spells.ContainsKey(spell))
                return ModelLoader.Load(Spells[spell].Model);
            Debug.LogError("Couldn't Find: " + spell.ToString());
            return null;
        }

        [Range(1, 20)] public const int PrintDecimals = 5;


        private IWorker worker;

       

        public delegate void Annonymous(Side side, int state);
        public static event SpellStateEvent AllSpellStates;

        public Dictionary<Spell, SpellReferences> Spells = new Dictionary<Spell, SpellReferences>();

        public struct SpellReferences
        {
            public NNModel Model;
            public event AnonymousSpellStateEvent SpellEvent;

            public void DoEvent(Side side, int state) { SpellEvent?.Invoke(side, state); }
        }

        public int PredictState(List<float> Inputs, Spell spell)
        {
            worker = WorkerFactory.CreateWorker(WorkerFactory.Type.ComputePrecompiled, GetModel(spell));

            int ActiveInputCount = Inputs.Count / FramesAgoBuild;
            //Debug.Log(ActiveInputCount); 
            Tensor input = new Tensor(1, 1, FramesAgoBuild, ActiveInputCount, Inputs.ToArray());
            worker.Execute(input);
            Tensor output = worker.PeekOutput();
            //bool predictedState = output[0] > 0.5f;
            int predictedClass = output.ArgMax()[0];
            input.Dispose();
            worker.Dispose();

            //Debug.Log(predictedState);

            return predictedClass;
        }

        public void RunModel()
        {
            foreach (Side side in new List<Side>() { Side.right, Side.left })
            {
                List<AthenaFrame> Frames = PastFrameRecorder.instance.GetFramesList(side, FramesAgoBuild);
                foreach (Spell spell in Spells.Keys)
                {
                    int State = PredictState(FrameToValues(Frames), spell);
                    AllSpellStates?.Invoke(spell, side, State);
                    Spells[spell].DoEvent(side, State);
                }
            }

            

                
           


            //run model with controller inputs
            //set color of controllers
        }

        public static List<float> FrameToValues(List<AthenaFrame> Frames)
        {
            //List<float> FrameInputs = Frames.SelectMany(x => x.AsInputs()).ToList();
            List<float> FrameInputs = Frames.SelectMany(x => x.AsInputs()).ToList();
            FrameInputs = FrameInputs.Select(x => MathF.Round(x, PrintDecimals)).ToList();
            return FrameInputs;
        }
    }
}

