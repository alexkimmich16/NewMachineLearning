using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.SceneManagement;
using System.IO;
using System.Xml.Serialization;
using UnityEditor;
public class ProceduralTesting : SerializedMonoBehaviour
{
    public static ProceduralTesting instance;
    [FoldoutGroup("Settings")] public bool UseProceduralTesting;
    [FoldoutGroup("Settings"), ShowIf("UseProceduralTesting")] public bool UseRandomTesting;
    [FoldoutGroup("Settings"), ShowIf("UseProceduralTesting")] public int NewTestThreshold;
    [FoldoutGroup("Settings"), ReadOnly] public string XMLPath;
    
    [FoldoutGroup("Stats")]
    public Dictionary<string, bool> StatChanges;

    [FoldoutGroup("Stats")] public Vector2 ComplexityRange, TrialDurationRange, GridFadeSpeedRange;
    [FoldoutGroup("Stats")] public int ComplexityBase;
    [FoldoutGroup("Stats")] public float TrialDurationBase;
    [FoldoutGroup("Stats")] public float GridFadeSpeedBase;

    [FoldoutGroup("Stats")] public int ComplexityCurrent;
    [FoldoutGroup("Stats")] public float TrialDurationCurrent;
    [FoldoutGroup("Stats")] public float GridFadeSpeedCurrent;

    

    public delegate void BeforeRestart();
    public static event BeforeRestart OnBeforeRestart;
    private void SetComplexity()
    {
        TextAsset Asset = (TextAsset)AssetDatabase.LoadAssetAtPath("Assets/UnitySharpNEAT/Resources/experiment.config.xml", typeof(TextAsset));  //(TextAsset) xmlFile.text;
        string TrueText = Asset.text;
        int Start = TrueText.IndexOf("<ComplexityThreshold>");
        int End = TrueText.IndexOf("<Description>");
        string ToSearch = TrueText.Substring(Start, End - Start);
        string ToReplace = "<ComplexityThreshold>" + ComplexityCurrent + "</ComplexityThreshold>";
        File.WriteAllText(XMLPath, TrueText.Replace(ToSearch, ToReplace));
    }
    void Awake()
    {
        instance = this;

        XMLPath = Application.dataPath + "/UnitySharpNEAT/Resources/experiment.config.xml";

        UnitySharpNEAT.LearningAgent.OnLog += NewFrame;

        if(UseProceduralTesting && UseRandomTesting)
        {
            ComplexityCurrent = StatChanges["Complexity"] ? (int)Random.Range(ComplexityRange.x, ComplexityRange.y) : ComplexityBase;
            SetComplexity();

            TrialDurationCurrent = StatChanges["TrialDuration"] ? Random.Range(TrialDurationRange.x, TrialDurationRange.y) : TrialDurationBase;
            GetComponent<UnitySharpNEAT.NeatSupervisor>().TrialDuration = TrialDurationCurrent;

            GridFadeSpeedCurrent = StatChanges["GridFadeSpeed"] ? Random.Range(GridFadeSpeedRange.x, GridFadeSpeedRange.y) : GridFadeSpeedBase;
            MatrixManager.instance.STimeMultiplier = GridFadeSpeedCurrent;
        }
    }


    public void NewFrame()
    {
        if (DataTracker.instance.TotalLogCount <= NewTestThreshold)
            return;
        if (!UseProceduralTesting)
            return;

        OnBeforeRestart();

        //reload scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
