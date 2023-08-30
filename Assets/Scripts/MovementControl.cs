using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEditor;
using RestrictionSystem;
using System;
using System.Linq;
using System.IO;
public class MovementControl : SerializedMonoBehaviour
{
    public static MovementControl instance;
    public string FollowingPath;
    public string FullPath { get { return Application.dataPath + FollowingPath; }}
    

    private void Awake()
    {
        instance = this;
        CollectScriptableObjects();
    }

    [FoldoutGroup("Movements")] public List<AllMotions> Movements;
    [FoldoutGroup("Movements"), Button(ButtonSizes.Small)]
    public void CollectMotions() { CollectScriptableObjects(); }
    //[FoldoutGroup("Movements")]


    [FoldoutGroup("NewMotion")] public string NewMotionName;
    [FoldoutGroup("NewMotion"), Button(ButtonSizes.Small)]
    public void AddNewMotion()
    {
        //add new motion stats
        //create new motion container
        AllMotions newObject = ScriptableObject.CreateInstance<AllMotions>();
        //newObject.name = NewMotionName;


        string assetName = NewMotionName;
        string assetPath = FullPath + "/" + assetName + ".asset";

        // Save the Scriptable Object as an asset
        AssetDatabase.CreateAsset(newObject, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        MotionSettings Settings = RestrictionManager.instance.RestrictionSettings;


        Settings.Coefficents.Add(new RegressionInfo());
        Settings.MotionRestrictions.Add(new MotionRestriction());
        Settings.MotionRestrictions[^1].Motion = NewMotionName;
        Settings.LogicInfo.Add(new FrameLogicInfo());
        Settings.MotionConditions.Add(new MotionConditionInfo());
        Settings.MotionConditions[^1].Motion = NewMotionName;
    }

    
    public int MotionCount() { return Movements.Count; }
    public int MovementCount(Spell Spell) { return Movements[(int)Spell].Motions.Count; }
    
    public int FrameCount(Spell Spell, int Motion) { return Movements[(int)Spell].Motions[Motion].Infos.Count; }
    public SingleInfo AtFrameInfo(Spell Spell, int Motion, int Frame) { return Movements[(int)Spell].Motions[Motion].Infos[Frame]; }
    public int TrueRangeCount(Spell Spell, int Motion) { return Movements[(int)Spell].Motions[Motion].TrueRanges.Count; }
    public bool FrameWorks(Spell Spell, int Motion, int Frame) { return Movements[(int)Spell].Motions[Motion].AtFrameState(Frame); }
    public void CollectScriptableObjects()
    {
        Movements = new List<AllMotions>(Enumerable.Repeat<AllMotions>(null, Enum.GetValues(typeof(Spell)).Length));

        string[] guids = AssetDatabase.FindAssets("t:AllMotions"); // Find all assets of type ScriptableObject within the specified folder
        foreach (string guid in guids)
        {
            //Debug.Log(guid);
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            AllMotions scriptableObject = AssetDatabase.LoadAssetAtPath<AllMotions>(assetPath);

            for (int i = 0; i < Enum.GetValues(typeof(Spell)).Length; i++)
            {
                if(scriptableObject.name == ((Spell)i).ToString())
                {
                    Movements[i] = scriptableObject;
                }
            }
        }
    }

    

    /*
    [FoldoutGroup("Rename"), Button(ButtonSizes.Small)]
    public void RenameMotions()
    {
        for (int i = 0; i < Enum.GetValues(typeof(Spell)).Length; i++)
        {
            RestrictionManager.instance.RestrictionSettings.MotionRestrictions[i].Motion = ((Spell)i).ToString();
            RestrictionManager.instance.RestrictionSettings.MotionConditions[i].Motion = ((Spell)i).ToString();
        }
    }
    */
}
