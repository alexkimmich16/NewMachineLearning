using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System;
public class LagTester : SerializedMonoBehaviour
{
    // Start is called before the first frame update
    public bool TestWithLag;
    public int TargetFPS = 45;
    void Start()
    {
        
        
        //QualitySettings.vSyncCount = 0;  // VSync must be disabled
        //Application.targetFrameRate = TargetFrame;
    }

    void Awake()
    {
        if (!TestWithLag)
            return;

        Application.targetFrameRate = -1;
        QualitySettings.vSyncCount = 0;
    }

    void Update()
    {
        if (!TestWithLag)
            return;

        long lastTicks = DateTime.Now.Ticks;
        long currentTicks = lastTicks;
        float delay = 1f / TargetFPS;
        float elapsedTime;

        if (TargetFPS <= 0)
            return;

         while(true)
         {
            currentTicks = DateTime.Now.Ticks;
            elapsedTime = (float)TimeSpan.FromTicks(currentTicks - lastTicks).TotalSeconds;
            if (elapsedTime >= delay)
            {
                break;
            }
        }
    }
}
