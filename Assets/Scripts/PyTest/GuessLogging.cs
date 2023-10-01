using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
public struct GuessLogging
{


    public float4 Guesses;
    public GuessLogging(float4 Guesses)
    {
        this.Guesses = Guesses;
    }

    public void UpdateGuesses(bool Correct, int State)
    {
        Guesses.w += (Correct && State == 1) ? 1f : 0f;
        Guesses.x += (!Correct && State == 1) ? 1f : 0f;
        Guesses.y += (Correct && State == 0) ? 1f : 0f;
        Guesses.z += (!Correct && State == 0) ? 1f : 0f;
    }

    public float Total() { return Guesses.w + Guesses.x + Guesses.y + Guesses.z; }
    public string PercentComplexString() { return "Correct: " + CorrectPercent() + "  Wrong: " + InCorrectPercent() + "  True: " + OnTruePercent() + "  False: " + OnFalsePercent(); }
    public string PercentSimpleString() { return "CorrectOnTrue: " + CorrectOnTruePercent() + "/" + IncorrectOnTruePercent() + "   CorrectOnFalse: " + CorrectOnFalsePercent() + "/" + IncorrectOnFalsePercent(); }

    //public string CorrectPercentString() { return "CorrectOnTrue/Incorrect%: " + } 

    public string MotionCountString() { return "True/False Frames: " + CorrectOnTrue() + IncorrectOnTrue() + "/" + CorrectOnFalse() + IncorrectOnFalse(); }
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
