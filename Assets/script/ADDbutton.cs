using UnityEngine;

public class RealButton : MonoBehaviour
{
    private GameObject ballmanager;
    private BallOperation balloperation;
    private Animator UIpanelgroup;
    private void Start()
    {
        ballmanager=GameObject.FindWithTag("BallManager");
        balloperation=ballmanager.GetComponent<BallOperation>();
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("the player has pressed the button");
        balloperation.BallGenerate();
    } 
}
