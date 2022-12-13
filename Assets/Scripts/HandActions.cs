using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class HandActions : MonoBehaviour
{
    public XRNode inputSource;

    public Rigidbody RB;

    public float Grip;
    public float Trigger;

    public bool TopButtonPressed;
    public bool TopButtonTouched;
    public bool BottomButtonPressed;
    public bool BottomButtonTouched;

    [HideInInspector]
    public bool Playing = false;
    [HideInInspector]
    public Vector2 Direction;
    //private float Speed;

    public bool Test;
    public bool SettingStats = false;

    public SkinnedMeshRenderer mesh;
    public Material True;
    public Material False;

    public static int PastFrameCount = 5;
    [HideInInspector]
    public List<Vector3> PastFrames = new List<Vector3>();
    public Vector3 Child;
    public Vector3 CamLocal;
    public Vector3 LocalRotation;
    public Vector3 Velocity;
    public Vector3 Acceleration;

    public float Magnitude;

    public delegate void OnRelease();
    public event OnRelease OnTriggerRelease;

    private bool lastTrigger;

    public Vector3 GetVelocity()
    {
        InputDevice device = InputDevices.GetDeviceAtXRNode(inputSource);
        device.TryGetFeatureValue(CommonUsages.deviceVelocity, out Vector3 Velocity);
        return Velocity;
    }
    void Update()
    {
        //SetRemoteStats();
        //CheckColliders();
        
        Child = transform.GetChild(1).eulerAngles;
        CamLocal = Camera.main.transform.eulerAngles;
        if (CamLocal.y > 270)
            CamLocal.y -= 360;
        if (Child.y > 270)
            Child.y -= 360;
        LocalRotation = new Vector3(Child.x, CamLocal.y - Child.y, Child.z);


        if(TriggerPressed() == true && lastTrigger == false)
        {
            lastTrigger = true;
        }
        else if (TriggerPressed() == false && lastTrigger == true)
        {
            lastTrigger = false;
            if(OnTriggerRelease != null)
                OnTriggerRelease();
        }
        /*
        if (LearningAgent.instance.Learning == false)
        {
            if (LearningAgent.instance.Guess)
            {
                mesh.material = True;
            }
            else
            {
                mesh.material = False;
            }
        }
        */
    }
    #region Checks
    public bool TriggerPressed()
    {
        InputDevice device = InputDevices.GetDeviceAtXRNode(inputSource);
        device.TryGetFeatureValue(CommonUsages.trigger, out Trigger);
        return Trigger > 0.5;
    }
    public bool GripPressed()
    {
        InputDevice device = InputDevices.GetDeviceAtXRNode(inputSource);
        device.TryGetFeatureValue(CommonUsages.grip, out Grip);
        return Grip > 0.5;
    }
    public void SetRemoteStats()
    {
        InputDevice device = InputDevices.GetDeviceAtXRNode(inputSource);

        device.TryGetFeatureValue(CommonUsages.secondaryButton, out BottomButtonPressed);
        device.TryGetFeatureValue(CommonUsages.secondaryTouch, out BottomButtonTouched);
        device.TryGetFeatureValue(CommonUsages.primaryButton, out TopButtonPressed);
        device.TryGetFeatureValue(CommonUsages.primaryTouch, out TopButtonTouched);

        device.TryGetFeatureValue(CommonUsages.primary2DAxis, out Direction);

        device.TryGetFeatureValue(CommonUsages.deviceVelocity, out Velocity);
        device.TryGetFeatureValue(CommonUsages.deviceAcceleration, out Acceleration);

        Magnitude = Velocity.magnitude;
        if (Direction != Vector2.zero)
        {
            //Debug.Log("touchpad" + Direction);
        }
    }


    #endregion

}