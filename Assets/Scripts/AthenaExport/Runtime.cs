using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Barracuda;
using Sirenix.OdinInspector;
using System.Linq;
using System;
using System.Threading;
using System.Threading.Tasks;
public enum Side
{
    right = 0,
    left = 1,
}
public enum Spell
{
    Nothing = 0,
    Fireball = 1,
    Parry = 2,
}
namespace Athena
{
    public delegate void AnonymousSpellStateEvent(Side side, int state);
    public delegate void SpellStateEvent(Spell spell, Side side, int state);
    public class Runtime : SerializedMonoBehaviour
    {
        public static Runtime instance;
        private void Awake() { instance = this; }

        public const int FramesAgoBuild = 6;
        public AthenaSpellHolder SpellHolder;

        private Dictionary<Spell, Dictionary<Side, IWorker>> Workers = new Dictionary<Spell, Dictionary<Side, IWorker>>();

        [Range(1, 20)] public const int PrintDecimals = 5;

        private Dictionary<Spell, Dictionary<Side, int>> SideStates = new Dictionary<Spell, Dictionary<Side, int>>();
        public Model GetModel(Spell spell) { return SpellHolder.GetModel(spell); }

        public static List<Side> Sides = new List<Side>() { Side.right, Side.left };

        public delegate void Annonymous(Side side, int state);
        public static event SpellStateEvent AllSpellChangeState;

        public IWorker NewWorker(Spell spell) { return WorkerFactory.CreateWorker(WorkerFactory.Type.ComputePrecompiled, GetModel(spell)); }

        private void Start()
        {
            Workers = new Dictionary<Spell, Dictionary<Side, IWorker>>();
            foreach (Spell spell in SpellHolder.Spells.Keys)
            {
                Workers.Add(spell, new Dictionary<Side, IWorker>() { { Side.right, NewWorker(spell) }, { Side.left, NewWorker(spell) } });
                SideStates.Add(spell, new Dictionary<Side, int>() { { Side.right, 0 }, { Side.left, 0 } });
            }                
        }
        public void RunModel()
        {
            // Use Parallel for non-Unity tasks (like data preparation)
            Parallel.ForEach(Sides, side =>
            {
                List<AthenaFrame> Frames = PastFrameRecorder.instance.GetFramesList(side, FramesAgoBuild);

                foreach (Spell spell in SpellHolder.Spells.Keys)
                {
                    List<float> FrameValues = FrameToValues(Frames);
                    int ActiveInputCount = FrameValues.Count / FramesAgoBuild;
                    //Debug.Log(ActiveInputCount);
                    Tensor input = new Tensor(1, 1, FramesAgoBuild, ActiveInputCount, FrameValues.ToArray());

                    // Enqueue Unity-specific operations to be run on the main thread
                    MainThreadDispatcher.Enqueue(() =>
                    {
                        try
                        {
                            IWorker worker = Workers[spell][side];

                            worker.Execute(input);
                            Tensor output = worker.PeekOutput();

                            int State = output.ArgMax()[0];

                            if (SideStates[spell][side] != State)
                            {
                                AllSpellChangeState?.Invoke(spell, side, State);
                                SpellHolder.Spells[spell].StateChangeEvent(side, State);
                                SideStates[spell][side] = State;
                            }

                            input.Dispose();
                            output.Dispose();
                        }
                        catch (ArgumentException ae)
                        {
                            // Log the exception or handle it in a way you deem appropriate
                            Debug.LogError($"Tensor shape mismatch: {ae.Message}");

                            // If you want to clean up resources or take corrective action, do it here
                            input.Dispose(); // ensure that resources are always freed
                        }

                    });
                }
            });
        }

        public List<float> FrameToValues(List<AthenaFrame> Frames)
        {
            List<float> FrameInputs = new List<float>();
            foreach (AthenaFrame frame in Frames)
            {
                foreach (RestrictionListItem item in SpellHolder.restrictions)
                {
                    FrameInputs.AddRange(item.GetValue(frame));
                }
            }
            return FrameInputs;
            
        }
    }
}
