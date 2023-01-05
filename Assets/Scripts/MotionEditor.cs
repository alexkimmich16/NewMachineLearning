using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using Sirenix.OdinInspector;
public enum EditSide
{
    left = 0,
    right = 1,
}
public class MotionEditor : SerializedMonoBehaviour
{
    public static MotionEditor instance;
    private void Awake() { instance = this; }

    public int MotionNum;
    public CurrentLearn MotionType;
    public bool Typing;
    public TMP_InputField input;
    public TextMeshProUGUI sideText;
    public TextMeshProUGUI CurrentValue;
    public TextMeshProUGUI Max;

    public TextMeshProUGUI FrameNum;
    public TextMeshProUGUI PlaybackSpeed;
    public TextMeshProUGUI MotionTypeText;
    public MotionPlayback display;
    

    //public List<RectTransform> ArrowSpots;
    public List<TextMeshProUGUI> TrueRangeTexts;

    //public float PlayBackSpeed;

    public int MaxMinEditing;

    public float SpeedChangePerSecond;
    public float SpeedMultiplier;

    public delegate void ChangeMotion();
    public static event ChangeMotion OnChangeMotion;

    private void Start()
    {
        input.ActivateInputField();
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
        if(OnChangeMotion != null)
            OnChangeMotion();
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


        input.ActivateInputField();
        sideText.text = "#" + MotionNum;
        FrameNum.text = "Frame: " + display.Frame;
        MotionTypeText.text = "Motion: " + MotionType.ToString();
        display.PlaybackSpeed += SpeedChangeAdd();
        PlaybackSpeed.text = "Speed: " + display.PlaybackSpeed.ToString("F2");
        Max.text = "Max: " + LearnManager.instance.MovementList[(int)MotionType].Motions[MotionNum].Infos.Count;
        if(LearnManager.instance.MovementList[(int)MotionType].Motions[MotionNum].TrueRanges.Count == 1)
            CurrentValue.text = "X: " + LearnManager.instance.MovementList[(int)MotionType].Motions[MotionNum].TrueRanges[MaxMinEditing].x + "\n" + "Y: " + LearnManager.instance.MovementList[(int)MotionType].Motions[MotionNum].TrueRanges[MaxMinEditing].y;
        else
            CurrentValue.text = "X: " + "\n" + "Y: ";
        if(Input.GetKey(KeyCode.LeftAlt) == false)
        {
            if (Input.GetKeyDown(KeyCode.UpArrow) && MotionNum < LearnManager.instance.MovementList[(int)MotionType].Motions.Count - 1)
            {
                MotionNum += 1;
                display.Motion = MotionNum;
                if(OnChangeMotion != null)
                    OnChangeMotion();
            }
            if (Input.GetKeyDown(KeyCode.DownArrow) && MotionNum > 0)
            {
                
                MotionNum -= 1;
                display.Motion = MotionNum;
                if (OnChangeMotion != null)
                    OnChangeMotion();
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
