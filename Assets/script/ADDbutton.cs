using UnityEngine;

public class RealButton : MonoBehaviour
{
    private GameObject ballmanager;
    // private DataManager datamanager;
    private BallOperation balloperation;
    private Animator UIpanelgroup;
    private void Start()
    {
        // datamanager=GameObject.FindWithTag("DataManager");
        ballmanager=GameObject.FindWithTag("BallManager");
        balloperation=ballmanager.GetComponent<BallOperation>();
        
    }

    public void PressedDownAdd()
    {

        Debug.Log("the player has pressed the button");
        BallMemory.MemoryData? bdt=new BallMemory.MemoryData?();
        balloperation.BallGenerate(bdt);
    } 
}
