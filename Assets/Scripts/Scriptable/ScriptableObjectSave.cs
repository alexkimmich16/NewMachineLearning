using UnityEngine;
using UnityEditor;
using RestrictionSystem;
public class ScriptableObjectSave : EditorWindow
{
    private MotionSettings myScriptableObject;

    [MenuItem("Window/My ScriptableObject Editor")]
    static void OpenWindow()
    {
        ScriptableObjectSave window = EditorWindow.GetWindow<ScriptableObjectSave>();
        window.Show();
    }

    private void OnGUI()
    {
        myScriptableObject = EditorGUILayout.ObjectField("MotionSettings", myScriptableObject, typeof(MotionSettings), false) as MotionSettings;

        EditorGUI.BeginChangeCheck();
        // Display and edit the properties of the Scriptable Object here
        Debug.Log("changecheck");
        if (EditorGUI.EndChangeCheck())
        {
            // Changes were made, so mark the Scriptable Object as dirty
            EditorUtility.SetDirty(myScriptableObject);
            Debug.Log("dirty");
            // Save the changes immediately
            AssetDatabase.SaveAssets();
        }
    }
}
