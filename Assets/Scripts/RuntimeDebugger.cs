using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Athena;
using Sirenix.OdinInspector;
public class RuntimeDebugger : SerializedMonoBehaviour
{
    public GameObject DebugBall;

    public Dictionary<Side, MeshRenderer> Hands;
    public List<Material> Materials;

    public PastFrameRecorder P => PastFrameRecorder.instance;
    public MotionEditor ME => MotionEditor.instance;

    private void Start()
    {
        Runtime.AllSpellChangeState += StateChange;
    }

    public void StateChange(Spell spell, Side side, int State)
    {
        if (!ME.TestAllMotions.isOn && spell != MotionEditor.instance.MotionType)
            return;

        Hands[side].material = Materials[State];
        if (State == 0)
            Cast(side, spell);

    }

    public void Cast(Side side, Spell spell)
    {
        GameObject debugBall = Instantiate(DebugBall, PastFrameRecorder.instance.PlayerHands[(int)side].position, Quaternion.Euler(PastFrameRecorder.instance.PlayerHands[(int)side].eulerAngles));
        debugBall.GetComponent<MeshRenderer>().material = Materials[(int)spell - 1];
        Destroy(debugBall, 3f);
    }
}
