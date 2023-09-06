using UnityEngine;
using UnityEditor;
using RestrictionSystem;
public class ScriptableObjectSave : EditorWindow
{
    private AthenaSpell myScriptableObject;

    [MenuItem("Window/My ScriptableObject Editor")]
    static void OpenWindow()
    {
        ScriptableObjectSave window = EditorWindow.GetWindow<ScriptableObjectSave>();
        window.Show();
    }

    private void OnGUI()
    {
        myScriptableObject = EditorGUILayout.ObjectField("AthenaSpell", myScriptableObject, typeof(AthenaSpell), false) as AthenaSpell;

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
