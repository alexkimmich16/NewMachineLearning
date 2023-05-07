using UnityEngine;

public class Fireball : MonoBehaviour
{
    public float Speed;

    //public float LifeTime = 3;
    void Update()
    {
        GetComponent<Rigidbody>().velocity = transform.forward * Time.deltaTime * Speed;
    }
}
