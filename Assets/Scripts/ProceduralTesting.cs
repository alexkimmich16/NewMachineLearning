using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.SceneManagement;
using System.IO;
public class ProceduralTesting : SerializedMonoBehaviour
{
    public static ProceduralTesting instance;
    [FoldoutGroup("Settings")] public bool UseProceduralTesting;
    [FoldoutGroup("Settings"), ShowIf("UseProceduralTesting")] public bool UseRandomTesting;
    [FoldoutGroup("Settings"), ShowIf("UseProceduralTesting")] public int NewTestThreshold;
    [FoldoutGroup("Settings"), ReadOnly] public string XMLPath;
    [FoldoutGroup("Settings")] public PastTrialHolder PastTrialInfoHolder;
    [FoldoutGroup("Settings")] public string InfoPath;
    
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

    public TextAsset XMLInfo;

    public float GetCorrectPercentStat()
    {
        int FrameKeepCount = DataTracker.instance.PastFrameInfoKeep;
        int TrueCount = 0;
        for (int i = 0; i < DataTracker.instance.PastFrameInfoKeepForTesting.Count; ++i)
            if (DataTracker.instance.PastFrameInfoKeepForTesting[i].Guess == DataTracker.instance.PastFrameInfoKeepForTesting[i].Truth)
                TrueCount += 1;
        return (float)TrueCount / (float)FrameKeepCount;

    }

    private void SetComplexity()
    {
        //TextAsset Asset = (TextAsset)AssetDatabase.LoadAssetAtPath("Assets/UnitySharpNEAT/Resources/experiment.config.xml", typeof(TextAsset));  //(TextAsset) xmlFile.text;
        TextAsset Asset = XMLInfo;
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


        if (UseProceduralTesting)
        {
            ComplexityCurrent = StatChanges["Complexity"] && UseRandomTesting ? (int)Random.Range(ComplexityRange.x, ComplexityRange.y) : ComplexityBase;
            SetComplexity();

            TrialDurationCurrent = StatChanges["TrialDuration"] && UseRandomTesting ? Random.Range(TrialDurationRange.x, TrialDurationRange.y) : TrialDurationBase;
            GetComponent<UnitySharpNEAT.NeatSupervisor>().TrialDuration = TrialDurationCurrent;

            GridFadeSpeedCurrent = StatChanges["GridFadeSpeed"] && UseRandomTesting ? Random.Range(GridFadeSpeedRange.x, GridFadeSpeedRange.y) : GridFadeSpeedBase;
            MatrixManager.instance.STimeMultiplier = GridFadeSpeedCurrent;
        }
    }
    /*
    public void AddToScriptableObject(SinglePastTrialInfo info)
    {
        FileStream stream = new FileStream(Path, FileMode.Open, FileAccess.ReadWrite);
        BinaryFormatter formatter = new BinaryFormatter();
        string Path = "B:/GitProjects/NewMachineLearning/NewMachineLearning/Assets/Scripts/Test.asset";
        

        PastTrialHolder data = formatter.Deserialize(stream) as PastTrialHolder;
        Debug.Log(data.AllInfoList.Count);
        
        data.AllInfoList.Add(info);
        formatter.Serialize(stream, data);
        
        stream.Close();
    }
    */

    public void NewFrame()
    {
        if (DataTracker.instance.TotalLogCount <= NewTestThreshold)
            return;
        if (!UseProceduralTesting)
            return;

        OnBeforeRestart();
        //AddToScriptableObject(new SinglePastTrialInfo(ComplexityCurrent, TrialDurationCurrent, GridFadeSpeedCurrent, GetCorrectPercentStat()));
        PastTrialInfoHolder.AllInfoList.Add(new SinglePastTrialInfo(ComplexityCurrent, TrialDurationCurrent, GridFadeSpeedCurrent, GetCorrectPercentStat()));

        //reload scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
