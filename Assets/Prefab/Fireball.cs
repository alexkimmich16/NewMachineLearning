using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.XR.Interaction.Toolkit;

public class Fireball : MonoBehaviour
{
    public float Speed;

    //public float LifeTime = 3;
    void Update()
    {
        GetComponent<Rigidbody>().velocity = transform.forward * Time.deltaTime * Speed;
    }
}
