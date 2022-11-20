using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public enum EditSide
{
    left = 0,
    right = 1,
}
public class MotionEditor : MonoBehaviour
{
    public int MotionNum;
    public bool Typing;
    public TMP_InputField input;
    public TextMeshProUGUI sideText;
    public TextMeshProUGUI CurrentValue;
    public TextMeshProUGUI Max;

    public TextMeshProUGUI FrameNum;
    public TextMeshProUGUI PlaybackSpeed;
    public MotionPlayback display;

    //public List<RectTransform> ArrowSpots;
    public List<TextMeshProUGUI> TrueRangeTexts;

    //public float PlayBackSpeed;

    public int MaxMinEditing;

    public float SpeedChangePerSecond;
    public float SpeedMultiplier;

    private void Start()
    {
        input.ActivateInputField();
    }
    public float SpeedChangeAdd()
    {
        float RealSpeedChange()
        {
            return SpeedChangePerSecond * Time.deltaTime;
        }
        float Change = 0;
        if (Input.GetKey(KeyCode.LeftControl) == false)
            return 0;

        if (Input.GetKey(KeyCode.UpArrow))
            if (Input.GetKey(KeyCode.LeftShift))
                Change += RealSpeedChange() * SpeedMultiplier;
            else
                Change += RealSpeedChange();
        if (Input.GetKey(KeyCode.DownArrow))
            if (Input.GetKey(KeyCode.LeftShift))
                Change -= RealSpeedChange() * SpeedMultiplier;
            else
                Change -= RealSpeedChange();

        return Change;
    }

    void Update()
    {
        int TrueRangeCount = LearnManager.instance.motions.Motions[MotionNum].TrueRanges.Count;
        if (Input.GetKeyDown(KeyCode.PageDown) && MaxMinEditing < TrueRangeCount - 1)
            MaxMinEditing += 1;
        if (Input.GetKeyDown(KeyCode.PageUp) && MaxMinEditing > 0 || MaxMinEditing > TrueRangeCount)
            MaxMinEditing -= 1;

        if (Input.GetKeyDown(KeyCode.KeypadMinus))
            if (TrueRangeCount > 0)
                LearnManager.instance.motions.Motions[MotionNum].TrueRanges.RemoveAt(TrueRangeCount - 1);

        if (Input.GetKeyDown(KeyCode.KeypadPlus) && TrueRangeCount - 1 < TrueRangeTexts.Count)
            LearnManager.instance.motions.Motions[MotionNum].TrueRanges.Add(Vector2.zero);


        input.ActivateInputField();
        sideText.text = "#" + MotionNum;
        FrameNum.text = "Frame: " + display.Frame;
        display.PlaybackSpeed += SpeedChangeAdd();
        PlaybackSpeed.text = "Speed: " + display.PlaybackSpeed.ToString("F2");
        Max.text = "Max: " + LearnManager.instance.motions.Motions[MotionNum].Infos.Count;
        if(LearnManager.instance.motions.Motions[MotionNum].TrueRanges.Count == 1)
            CurrentValue.text = "X: " + LearnManager.instance.motions.Motions[MotionNum].TrueRanges[MaxMinEditing].x + "\n" + "Y: " + LearnManager.instance.motions.Motions[MotionNum].TrueRanges[MaxMinEditing].y;
        else
            CurrentValue.text = "X: " + "\n" + "Y: ";

        if (Input.GetKeyDown(KeyCode.UpArrow) && MotionNum < LearnManager.instance.motions.Motions.Count - 1)
        {
            MotionNum += 1;
            display.Motion = MotionNum;
        }
        if (Input.GetKeyDown(KeyCode.DownArrow) && MotionNum > 0)
        {
            MotionNum -= 1;
            display.Motion = MotionNum;
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
            if(LearnManager.instance.motions.Motions[MotionNum].TrueRanges.Count > i)
            {
                Vector2 TrueRange = LearnManager.instance.motions.Motions[MotionNum].TrueRanges[i];
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
        if (LearnManager.instance.motions.Motions[MotionNum].TrueRanges.Count == 0)
            LearnManager.instance.motions.Motions[MotionNum].TrueRanges.Add(new Vector2(-1, -1));
        Vector2 Range = LearnManager.instance.motions.Motions[MotionNum].TrueRanges[MaxMinEditing];
        if (side == EditSide.left)
            LearnManager.instance.motions.Motions[MotionNum].TrueRanges[MaxMinEditing] = new Vector2(ToSet, Range.y);
        else if(side == EditSide.right)
            LearnManager.instance.motions.Motions[MotionNum].TrueRanges[MaxMinEditing] = new Vector2(Range.x, ToSet);
    }
}
