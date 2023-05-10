using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Linq;
using Sirenix.OdinInspector;
using RestrictionSystem;
public enum EditSide
{
    left = 0,
    right = 1,
}
public enum EditSettings
{
    Editing = 0,
    DisplayingMotion = 1,
}
public class MotionEditor : SerializedMonoBehaviour
{
    public static MotionEditor instance;
    private void Awake() { instance = this; }

    public int MotionNum;
    public CurrentLearn MotionType;
    public EditSettings Setting;
    public bool Typing;
    
    public TMP_InputField input;
    public TextMeshProUGUI sideText;
    public TextMeshProUGUI CurrentValue;
    public TextMeshProUGUI Max;

    public TextMeshProUGUI FrameNum;
    public TextMeshProUGUI PlaybackSpeed;
    public TextMeshProUGUI MotionTypeText;
    public TextMeshProUGUI SettingText;

    public TextMeshProUGUI PercentDone;
    public TextMeshProUGUI ETA;

    public TextMeshProUGUI TestValue;

    public TextMeshProUGUI MotionTestDisplay;
    public TextMeshProUGUI CurrentMotionNum;

    public TextMeshProUGUI MiscDisplay;

    public Toggle TestAllMotions;

    

    public Slider DoneSlider;

    public MotionPlayback display;

    public Toggle DisplayingRightStats;
    public Toggle DisplayingVR;
    public TextMeshProUGUI[] CurrentMotionTests;

    public void RecieveSliderInfo(float PercentDone, int ETAInSeconds)
    {
        this.PercentDone.text = PercentDone.ToString("f8") ;
        DoneSlider.value = PercentDone;

        int CurrentETA = ETAInSeconds;
        int Hours = TimeSpan.FromSeconds(CurrentETA).Hours;
        CurrentETA -= TimeSpan.FromSeconds(CurrentETA).Hours * 3600;
        int Minutes = TimeSpan.FromSeconds(CurrentETA).Minutes;
        CurrentETA -= TimeSpan.FromSeconds(CurrentETA).Minutes * 60;
        int Seconds = TimeSpan.FromSeconds(CurrentETA).Seconds;

        ETA.text = "ETA: " + DigitString(Hours) + ":" + DigitString(Minutes) + ":" + DigitString(Seconds);
        string DigitString(int Info)
        {
            return Info > 10 ? Info.ToString() : "0" + Info.ToString();
        }
    }
    //public List<RectTransform> ArrowSpots;
    public List<TextMeshProUGUI> TrueRangeTexts;

    //public float PlayBackSpeed;

    public int MaxMinEditing;

    public float SpeedChangePerSecond;
    public float SpeedMultiplier;

    public delegate void ChangeMotion();
    public static event ChangeMotion OnChangeMotion;

    //[ShowIf("Setting", EditSettings.DisplayingMotion)] 

    public bool SafetyCheck;


    [FoldoutGroup("Functions"), Button(ButtonSizes.Small)]
    public void DeleteThisMotion()
    {
        if (!SafetyCheck)
            return;
        
        LearnManager.instance.MovementList[(int)MotionType].Motions.RemoveAt(MotionNum);

        OnChangeMotion?.Invoke();
    }
    public void TestCurrentButton()
    {
        if (MotionType == CurrentLearn.Nothing)
            return;

        RestrictionStatManager stats = RestrictionStatManager.instance;
        MotionSettings MS = RestrictionManager.instance.RestrictionSettings;

        List<SingleFrameRestrictionValues> FrameInfo = stats.GetRestrictionsForMotions(MotionType, MS.MotionRestrictions[(int)MotionType - 1]);
        double[] Coefficents = MS.Coefficents[(int)MotionType - 1].GetCoefficents().Select(f => (double)f).ToArray();

        float Value = new LogisticRegression(RegressionSystem.GetInputValues(FrameInfo), RegressionSystem.GetOutputValues(FrameInfo), RegressionSystem.instance.EachTotalDegree, Coefficents).CorrectPercent();
        Debug.Log("Value: " + Value);
        TestValue.text = "'" + MotionType.ToString() + "' Correct: " + Value.ToString("f4");
    }
    private void Start()
    {
        input.ActivateInputField();
        AutoPickExtension.OnCompletionUpdate += RecieveSliderInfo;
        SafetyCheck = false;
    }
    public float SpeedChangeAdd()
    {
        float Change = 0;
        if (Input.GetKey(KeyCode.LeftControl) == false)
            return 0;

        if (Input.GetKey(KeyCode.UpArrow))
            Change += ((Input.GetKey(KeyCode.LeftShift)) ? SpeedMultiplier : 1) * SpeedChangePerSecond * Time.deltaTime;

        if (Input.GetKey(KeyCode.DownArrow))
            Change -= ((Input.GetKey(KeyCode.LeftShift)) ? SpeedMultiplier : 1) * SpeedChangePerSecond * Time.deltaTime;

        return Change;
    }
    public void ChangeMotionType(int Change)
    {
        MotionType += Change;
        if ((int)MotionType > Enum.GetValues(typeof(CurrentLearn)).Length - 1)
            MotionType = 0;
        else if((int)MotionType < 0)
            MotionType = (CurrentLearn)Enum.GetValues(typeof(CurrentLearn)).Length - 1;

        if (MotionNum > LearnManager.instance.MovementList[(int)MotionType].Motions.Count - 1)
            MotionNum = LearnManager.instance.MovementList[(int)MotionType].Motions.Count - 1;

        display.Frame = 0;
        OnChangeMotion?.Invoke();
    }

    public void ChangeMotionNum(int Change)
    {
        if(MotionNum + Change > LearnManager.instance.MovementList[(int)MotionType].Motions.Count - 1)
        {
            MotionNum = LearnManager.instance.MovementList[(int)MotionType].Motions.Count - 1;
            return;
        }
        else if(MotionNum + Change < 0)
        {
            MotionNum = 0;
            return;
        }

        MotionNum += Change;
        display.Motion = MotionNum;
        OnChangeMotion?.Invoke();
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.LeftAlt))
            if(Input.GetKeyDown(KeyCode.UpArrow))
                ChangeMotionType(1);
            else if(Input.GetKeyDown(KeyCode.DownArrow))
                ChangeMotionType(-1);

        if (MotionNum >= LearnManager.instance.MovementList[(int)MotionType].Motions.Count)
            MotionNum = LearnManager.instance.MovementList[(int)MotionType].Motions.Count - 1;

        int TrueRangeCount = LearnManager.instance.MovementList[(int)MotionType].Motions[MotionNum].TrueRanges.Count;
        if (Input.GetKeyDown(KeyCode.PageDown) && MaxMinEditing < TrueRangeCount - 1)
            MaxMinEditing += 1;
        if (Input.GetKeyDown(KeyCode.PageUp) && MaxMinEditing > 0 || MaxMinEditing > TrueRangeCount)
            MaxMinEditing -= 1;

        if (Input.GetKeyDown(KeyCode.KeypadMinus))
            if (TrueRangeCount > 0)
                LearnManager.instance.MovementList[(int)MotionType].Motions[MotionNum].TrueRanges.RemoveAt(TrueRangeCount - 1);

        if (Input.GetKeyDown(KeyCode.KeypadPlus) && TrueRangeCount - 1 < TrueRangeTexts.Count)
            LearnManager.instance.MovementList[(int)MotionType].Motions[MotionNum].TrueRanges.Add(Vector2.zero);

        Side side = DisplayingRightStats.isOn ? Side.right : Side.left;
        if (PastFrameRecorder.IsReady() && (DisplayingVR.isOn == false && display.Frame == 0) == false)
        {
            if (MotionType != CurrentLearn.Nothing)
            {
                for (int i = 0; i < CurrentMotionTests.Length; i++)
                {
                    SingleInfo Frame1 = DisplayingVR.isOn ? PastFrameRecorder.instance.PastFrame(side) : LearnManager.instance.MovementList[(int)MotionType].Motions[MotionNum].Infos[display.Frame - 1];
                    SingleInfo Frame2 = DisplayingVR.isOn ? PastFrameRecorder.instance.GetControllerInfo(side) : LearnManager.instance.MovementList[(int)MotionType].Motions[MotionNum].Infos[display.Frame];

                    bool Inside = i < RestrictionManager.instance.RestrictionSettings.MotionRestrictions[(int)MotionType - 1].Restrictions.Count;
                    if (Inside)
                    {
                        SingleRestriction Restriction = RestrictionManager.instance.RestrictionSettings.MotionRestrictions[(int)MotionType - 1].Restrictions[i];

                        float Value = RestrictionManager.RestrictionDictionary[Restriction.restriction].Invoke(Restriction, Frame1, Frame2);

                        CurrentMotionTests[i].text = Restriction.Label + ": " + Value.ToString("f4");
                        CurrentMotionTests[i].color = Graph.instance.Colors[i];
                    }
                    else
                    {
                        CurrentMotionTests[i].text = "";
                    }
                }
            }
            else
            {
                for (int i = 0; i < CurrentMotionTests.Length; i++)
                {
                    CurrentMotionTests[i].text = "";
                    //CurrentMotionTests[i].color = Graph.instance.Colors[i];
                }
            }
            
        }
        


        input.ActivateInputField();
        sideText.text = "#" + MotionNum;
        FrameNum.text = "Frame: " + display.Frame;
        MotionTypeText.text = "Motion: " + MotionType.ToString();
        //MotionTestDisplay.text = "Test: " + MotionType.ToString();
        SettingText.text = "Settings: " + Setting.ToString();
        display.PlaybackSpeed += SpeedChangeAdd();
        PlaybackSpeed.text = "Speed: " + display.PlaybackSpeed.ToString("F2");
        Max.text = "Max: " + LearnManager.instance.MovementList[(int)MotionType].Motions[MotionNum].Infos.Count;
        CurrentMotionNum.text = "MotionNum: " + MotionNum + "/" + (LearnManager.instance.MovementList[(int)MotionType].Motions.Count - 1);

        /*
        SingleInfo Frame = PastFrameRecorder.instance.GetControllerInfo(side);
        Vector3 Adjusted = new Vector3(Frame.HandPos.x, 0, Frame.HandPos.z).normalized;

        Quaternion quat = Quaternion.Euler(new Vector3(Frame.HeadPos.x, 0, Frame.HeadPos.z));//inside always 0
        Vector3 forwardDir = (quat * Vector3.forward).normalized;

        float Angle = Frame.HeadRot.y + Vector3.SignedAngle(Adjusted, forwardDir, Vector3.up) + 180f;
        MiscDisplay.text = "MiscVal: " + Frame.HeadRot.y.ToString("f3") + "   Angle(" + Angle.ToString("f3") + ")";
        */

        if (LearnManager.instance.MovementList[(int)MotionType].Motions[MotionNum].TrueRanges.Count == 1)
            CurrentValue.text = "X: " + LearnManager.instance.MovementList[(int)MotionType].Motions[MotionNum].TrueRanges[MaxMinEditing].x + "\n" + "Y: " + LearnManager.instance.MovementList[(int)MotionType].Motions[MotionNum].TrueRanges[MaxMinEditing].y;
        else
            CurrentValue.text = "X: " + "\n" + "Y: ";


        if(!Input.GetKey(KeyCode.LeftAlt) && !Input.GetKey(KeyCode.LeftControl))
        {
            //Debug.Log("check");
            if (Input.GetKeyDown(KeyCode.UpArrow))
                ChangeMotionNum(!Input.GetKey(KeyCode.LeftShift) ? 1 : 10);
            if (Input.GetKeyDown(KeyCode.DownArrow))
                ChangeMotionNum(!Input.GetKey(KeyCode.LeftShift) ? -1 : -10);
        }
        

        if (Input.GetKeyDown(KeyCode.LeftArrow) && Input.GetKey(KeyCode.LeftControl) == false && Input.GetKey(KeyCode.LeftShift) == false)
        {
            CheckLeftOverText(EditSide.left);
            Typing = true;
            input.ActivateInputField();
        }
        if (Input.GetKeyDown(KeyCode.RightArrow) && Input.GetKey(KeyCode.LeftControl) == false && Input.GetKey(KeyCode.LeftShift) == false)
        {
            CheckLeftOverText(EditSide.right);
            Typing = true;
            input.ActivateInputField();
        }

        for (int i = 0; i < TrueRangeTexts.Count; i++)
        {
            if(LearnManager.instance.MovementList[(int)MotionType].Motions[MotionNum].TrueRanges.Count > i)
            {
                Vector2 TrueRange = LearnManager.instance.MovementList[(int)MotionType].Motions[MotionNum].TrueRanges[i];
                TrueRangeTexts[i].text = "X: " + TrueRange.x + " Y: " + TrueRange.y;
                if (i == MaxMinEditing)
                    TrueRangeTexts[i].color = Color.red;
                else
                    TrueRangeTexts[i].color = Color.white;
            }
            else
            {
                TrueRangeTexts[i].text = "";
                TrueRangeTexts[i].color = Color.white;
            }
            
        }
        input.text = input.text.Replace("+", "");
        input.text = input.text.Replace("-", "");

        void CheckLeftOverText(EditSide side)
        {
            if (Typing == true && input.text != "")
            {
                Set(int.Parse(input.text), side);
                input.text = "";
            }
        }
    }

    public void Set(int ToSet, EditSide side)
    {
        if (LearnManager.instance.MovementList[(int)MotionType].Motions[MotionNum].TrueRanges.Count == 0)
            LearnManager.instance.MovementList[(int)MotionType].Motions[MotionNum].TrueRanges.Add(new Vector2(-1, -1));
        Vector2 Range = LearnManager.instance.MovementList[(int)MotionType].Motions[MotionNum].TrueRanges[MaxMinEditing];
        if (side == EditSide.left)
            LearnManager.instance.MovementList[(int)MotionType].Motions[MotionNum].TrueRanges[MaxMinEditing] = new Vector2(ToSet, Range.y);
        else if(side == EditSide.right)
            LearnManager.instance.MovementList[(int)MotionType].Motions[MotionNum].TrueRanges[MaxMinEditing] = new Vector2(Range.x, ToSet);
    }
}
