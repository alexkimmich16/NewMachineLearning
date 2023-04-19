using UnityEngine;

public static class Bug
{
    public static (string name, T value) Log<T>(T variable, [System.Runtime.CompilerServices.CallerMemberName] string variableName = "")
    {
        Debug.Log(variableName + ": " + variable.ToString());
        return (variableName, variable);
    }
}
