using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Linq;
using Sirenix.OdinInspector;
using RestrictionSystem;
using Athena;
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
    public Spell MotionType;
    
    public bool Typing;
    
    public TMP_InputField input;
    public TextMeshProUGUI sideText;
    public TextMeshProUGUI CurrentValue;
    public TextMeshProUGUI Max;

    public TextMeshProUGUI FrameNum;
    public TextMeshProUGUI PlaybackSpeed;
    public TextMeshProUGUI MotionTypeText;
    public TextMeshProUGUI SettingText;
    //public TextMeshProUGUI ETA;

    public TextMeshProUGUI TestValue;

    public TextMeshProUGUI CurrentMotionNum;

    public TextMeshProUGUI MiscDisplay;
    public TextMeshProUGUI FrameDisplay;

    public Toggle TestAllMotions;

    public MotionPlayback display;

    //public List<RectTransform> ArrowSpots;
    public List<TextMeshProUGUI> TrueRangeTexts;

    //public float PlayBackSpeed;

    public int MaxMinEditing;

    public float SpeedChangePerSecond;
    public float SpeedMultiplier;

    public delegate void ChangeMotion();
    public static event ChangeMotion OnChangeMotion;

    //[ShowIf("Setting", EditSettings.DisplayingMotion)] 
    //
    //public bool ShouldRead;
    public bool SafetyCheck;


    [FoldoutGroup("Functions"), Button(ButtonSizes.Small)]
    public void DeleteThisMotion()
    {
        if (!SafetyCheck)
            return;

        Cycler.Movements[MotionType].Motions.RemoveAt(MotionNum);

        OnChangeMotion?.Invoke();
    }
    private void Start()
    {
        input.ActivateInputField();
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
    public void ChangeMovement(int Change)
    {
        MotionType += Change;
        if ((int)MotionType > Enum.GetValues(typeof(Spell)).Length - 1)
            MotionType = (Spell)1;
        else if((int)MotionType <= 0)
            MotionType = (Spell)Enum.GetValues(typeof(Spell)).Length - 1;

        if (MotionNum > Cycler.MovementCount(MotionType) - 1)
            MotionNum = Cycler.MovementCount(MotionType) - 1;

        //if(MotionType == Spell.Nothing)
            //MotionType = (Spell)1;

        display.Frame = 0;
        OnChangeMotion?.Invoke();
    }

    public void ChangeMotionNum(int Change)
    {
        if(MotionNum + Change > Cycler.MovementCount(MotionType) - 1)
        {
            MotionNum = Cycler.MovementCount(MotionType) - 1;
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
        if (MotionType == Spell.Nothing)
            return;
        
        if (Input.GetKey(KeyCode.LeftAlt))
            if(Input.GetKeyDown(KeyCode.UpArrow))
                ChangeMovement(1);
            else if(Input.GetKeyDown(KeyCode.DownArrow))
                ChangeMovement(-1);

        if (MotionNum >= Cycler.MovementCount(MotionType))
            MotionNum = Cycler.MovementCount(MotionType) - 1;

        int TrueRangeCount = Cycler.TrueRangeCount(MotionType, MotionNum);
        if (Input.GetKeyDown(KeyCode.PageDown) && MaxMinEditing < TrueRangeCount - 1)
            MaxMinEditing += 1;
        if (Input.GetKeyDown(KeyCode.PageUp) && MaxMinEditing > 0 || MaxMinEditing > TrueRangeCount)
            MaxMinEditing -= 1;

        if (Input.GetKeyDown(KeyCode.KeypadMinus))
            if (TrueRangeCount > 0)
                Cycler.Movements[MotionType].Motions[MotionNum].TrueRanges.RemoveAt(TrueRangeCount - 1);

        if (Input.GetKeyDown(KeyCode.KeypadPlus) && TrueRangeCount - 1 < TrueRangeTexts.Count)
            Cycler.Movements[MotionType].Motions[MotionNum].TrueRanges.Add(Vector2.zero);


        input.ActivateInputField();
        sideText.text = "#" + MotionNum;
        FrameNum.text = "Frame: " + display.Frame;
        MotionTypeText.text = "Motion: " + MotionType.ToString();
        //MotionTestDisplay.text = "Test: " + MotionType.ToString();
        //SettingText.text = "Settings: " + Setting.ToString();
        display.PlaybackSpeed += SpeedChangeAdd();
        PlaybackSpeed.text = "Speed: " + display.PlaybackSpeed.ToString("F2");
        Max.text = "Max: " + Cycler.Movements[MotionType].Motions[MotionNum].Infos.Count;
        CurrentMotionNum.text = "MotionNum: " + MotionNum + "/" + (Cycler.Movements[MotionType].Motions.Count - 1);

        if (Cycler.Movements[MotionType].Motions[MotionNum].TrueRanges.Count == 1)
            CurrentValue.text = "X: " + Cycler.Movements[MotionType].Motions[MotionNum].TrueRanges[MaxMinEditing].x + "\n" + "Y: " + Cycler.Movements[MotionType].Motions[MotionNum].TrueRanges[MaxMinEditing].y;
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
        if (Cycler.Movements[MotionType].Motions[MotionNum].TrueRanges.Count == 0)
            Cycler.Movements[MotionType].Motions[MotionNum].TrueRanges.Add(new Vector2(-1, -1));
        Vector2 Range = Cycler.Movements[MotionType].Motions[MotionNum].TrueRanges[MaxMinEditing];
        if (side == EditSide.left)
            Cycler.Movements[MotionType].Motions[MotionNum].TrueRanges[MaxMinEditing] = new Vector2(ToSet, Range.y);
        else if(side == EditSide.right)
            Cycler.Movements[MotionType].Motions[MotionNum].TrueRanges[MaxMinEditing] = new Vector2(Range.x, ToSet);
    }
}
