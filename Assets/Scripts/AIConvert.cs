using UnityEngine;

public class AIConvert : MonoBehaviour
{
    //public LearningAgent agent;
    public float Cooldown;
    public float Timer;
    public bool Casting;
    public ParticleSystem fire;
    ///make both side(invert)
    ///check for moved distance

    ///check for velocity block
    private void Start()
    {
        fire.Stop();
    }
    /*
    private void Update()
    {
        if(agent.Guess == true)
        {
            fire.Play();
            Casting = true;
            Timer = 0;
        }
        if(Casting == true)
        {
            Timer += Time.deltaTime;
            if (Timer > Cooldown)
            {
                Casting = false;
                fire.Stop();
            }
        }
            
    }
    */
}
