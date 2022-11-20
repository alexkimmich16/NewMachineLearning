using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class MovementProvider : MonoBehaviour
{
    #region Singleton
    public static MovementProvider instance;
    void Awake() { instance = this; }
    #endregion

    public XRNode inputSource;
    private XRRig rig;
    public Vector2 inputAxis;
    public float gravity = -9.81f;
    private float fallSpeed;
    public float speed;
    public LayerMask groundLayer;
    public float Additionalheight = 20;
    private Rigidbody RB;
    public Vector3 direction;
    public Vector3 directionAdd;
    public bool isGrounded;

    public static bool isHardwarePresent()
    {
        var xrDisplaySubsystems = new List<XRDisplaySubsystem>();
        SubsystemManager.GetInstances<XRDisplaySubsystem>(xrDisplaySubsystems);
        foreach (var xrDisplay in xrDisplaySubsystems)
        {
            if (xrDisplay.running)
            {
                return true;
            }
        }
        return false;
    }

    void Start()
    {
        rig = GetComponent<XRRig>();
        RB = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        InputDevice device = InputDevices.GetDeviceAtXRNode(inputSource);
        device.TryGetFeatureValue(CommonUsages.primary2DAxis, out inputAxis);

        InputDevice handR = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        handR.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotR);
        direction = rotR * Vector3.forward;
        //Debug.Log(isHardwarePresent());

        //Debug.Log(direction);
    }
    /*
    public void CapsuleFollowHeadset()
    {
        character.height = rig.cameraInRigSpaceHeight + Additionalheight;
        Vector3 capsuleCenter = transform.InverseTransformPoint(rig.cameraGameObject.transform.position);
        character.center = new Vector3(capsuleCenter.x, character.height / 2 + character.skinWidth, capsuleCenter.z);
    }

    bool CheckGrounded()
    {
        Vector3 raystart = transform.TransformPoint(character.center);
        float rayLength = character.center.y + 0.01f;
        bool HasHit = Physics.SphereCast(raystart, character.radius, Vector3.down, out RaycastHit hitInfo, rayLength, groundLayer);
        return HasHit;
    }

     */
    public void Move()
    {
        Quaternion headYaw = Quaternion.Euler(0, rig.cameraGameObject.transform.eulerAngles.y, 0);
        directionAdd = headYaw * new Vector3(inputAxis.x, 0, inputAxis.y);
        RB.AddForce(directionAdd * Time.fixedDeltaTime * speed);
    }
    private void FixedUpdate()
    {
        Move();

        /*
         * CapsuleFollowHeadset();
        //gravity
        isGrounded = CheckGrounded();
        if (isGrounded)
        {
            fallSpeed = 0;
        }
        else
        {
            fallSpeed += gravity * Time.fixedDeltaTime;
            character.Move(Vector3.up * fallSpeed * Time.fixedDeltaTime);
        }
        
        */
    }



}
