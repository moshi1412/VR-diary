using UnityEngine;

public class Deleteoperation : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private GameObject ball;
    
    public void DeleteBall()
    {
        Debug.Log("Start to launch the ball");
        ball=GameObject.FindWithTag("DataManager").GetComponent<DataManager>().BallOnProcess;
        ball.GetComponent<MagLevHover>().TriggerLaunchSequence();
    }
}
