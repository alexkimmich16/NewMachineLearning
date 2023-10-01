using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Athena;
public class RuntimeDebugger : MonoBehaviour
{
    int lastFrame;
    public GameObject DebugBall;
    public void StateChange(Spell spell, Side side, int State)
    {
        if(side == Side.right && spell == Spell.Fireball)
        {

            if (State == 0 && lastFrame == 1)
                Cast(Side.right);


            lastFrame = State;

        }
    }

    public void Cast(Side side)
    {
        GameObject debugBall = Instantiate(DebugBall, PastFrameRecorder.instance.PlayerHands[(int)side].position, Quaternion.Euler(PastFrameRecorder.instance.PlayerHands[(int)side].eulerAngles));
        Destroy(debugBall, 3f);
    }
    void Start()
    {
        Runtime.AllSpellStates += StateChange;
    }
}
