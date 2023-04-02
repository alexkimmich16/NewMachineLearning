using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using Sirenix.OdinInspector;
public enum EditSide
{
    left = 0,
    right = 1,
}
public enum EditSettings
{
    Editing = 0,
    DisplayingBrute = 1,
    DisplayingMotion = 2,
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

    public Toggle TestAllMotions;

    

    public Slider DoneSlider;

    public MotionPlayback display;

    public Toggle DisplayingRightStats;
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
    public RestrictionSystem.CurrentLearn CurrentTestMotion;


    public void TestCurrentButton()
    {
        if (MotionType == CurrentLearn.Nothing)
            return;
        float Value = RestrictionSystem.RegressionSystem.instance.GetTestRegressionStats(RestrictionSystem.RestrictionManager.instance.coefficents.RegressionStats[(int)MotionType - 1].GetCoefficents(), (RestrictionSystem.CurrentLearn)(int)MotionType);
        TestValue.text = "'" + MotionType.ToString() + "' Correct: " + Value.ToString("f5");
    }

    public void ChangeToNextTest()
    {
        CurrentTestMotion += 1;
        if ((int)CurrentTestMotion > Enum.GetValues(typeof(CurrentLearn)).Length - 1)
            CurrentTestMotion = (RestrictionSystem.CurrentLearn)1;
        else if ((int)CurrentTestMotion < 0)
            CurrentTestMotion = (RestrictionSystem.CurrentLearn)1;

        OnChangeMotion?.Invoke();
    }
    private void Start()
    {
        input.ActivateInputField();
        RestrictionSystem.AutoPickExtension.OnCompletionUpdate += RecieveSliderInfo; 
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


        if (RestrictionSystem.PastFrameRecorder.IsReady())
        {
            for (int i = 0; i < CurrentMotionTests.Length; i++)
            {
                RestrictionSystem.SingleRestriction Restriction = RestrictionSystem.RestrictionManager.instance.RestrictionSettings.MotionRestrictions[0].Restrictions[i];
                RestrictionSystem.Side side = (RestrictionSystem.Side)(DisplayingRightStats.isOn ? 0 : 1);
                float Value = RestrictionSystem.RestrictionManager.RestrictionDictionary[Restriction.restriction].Invoke(Restriction, RestrictionSystem.PastFrameRecorder.instance.PastFrame(side), RestrictionSystem.PastFrameRecorder.instance.GetControllerInfo(side));

                CurrentMotionTests[i].text = Restriction.Label + ": " + Value.ToString("f4");
                CurrentMotionTests[i].color = Graph.instance.Colors[i];
                //
                //RM.RestrictionSettings.MotionRestrictions[0].
            }
        }
        


        input.ActivateInputField();
        sideText.text = "#" + MotionNum;
        FrameNum.text = "Frame: " + display.Frame;
        MotionTypeText.text = "Motion: " + MotionType.ToString();
        MotionTestDisplay.text = "Test: " + CurrentTestMotion.ToString();
        SettingText.text = "Settings: " + Setting.ToString();
        display.PlaybackSpeed += SpeedChangeAdd();
        PlaybackSpeed.text = "Speed: " + display.PlaybackSpeed.ToString("F2");
        Max.text = "Max: " + LearnManager.instance.MovementList[(int)MotionType].Motions[MotionNum].Infos.Count;
        if(LearnManager.instance.MovementList[(int)MotionType].Motions[MotionNum].TrueRanges.Count == 1)
            CurrentValue.text = "X: " + LearnManager.instance.MovementList[(int)MotionType].Motions[MotionNum].TrueRanges[MaxMinEditing].x + "\n" + "Y: " + LearnManager.instance.MovementList[(int)MotionType].Motions[MotionNum].TrueRanges[MaxMinEditing].y;
        else
            CurrentValue.text = "X: " + "\n" + "Y: ";
        if(Input.GetKey(KeyCode.LeftAlt) == false)
        {
            //Debug.Log("check");
            if (Input.GetKeyDown(KeyCode.UpArrow) && MotionNum < LearnManager.instance.MovementList[(int)MotionType].Motions.Count - 1)
            {
                MotionNum += 1;
                display.Motion = MotionNum;
                OnChangeMotion?.Invoke();
            }
            if (Input.GetKeyDown(KeyCode.DownArrow) && MotionNum > 0)
            {
                MotionNum -= 1;
                display.Motion = MotionNum;
                OnChangeMotion?.Invoke();
            }
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
