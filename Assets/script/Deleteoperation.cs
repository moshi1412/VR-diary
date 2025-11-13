using UnityEngine;

public class Deleteoperation : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private GameObject ball;
    
    public void DeleteBall()
    {
        ball=GameObject.FindWithTag("DataManager").GetComponent<DataManager>().BallOnProcess;
        ball.GetComponent<MagLevHover>().TriggerLaunchSequence();
    }
}
