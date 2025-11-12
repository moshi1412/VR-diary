using UnityEngine;
public class ConsoleDetector : MonoBehaviour
{
    private Animator UIpanelgroup;
    public DataManager database;
    private void Start()
    {
        UIpanelgroup=GameObject.FindWithTag("PanelGroup").GetComponent<Animator>();
    }
    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Ball")&&database.BallOnProcess is not null)
        {
            database.BallOnProcess=other.gameObject;
            UIpanelgroup.SetBool("in",false);
            UIpanelgroup.SetBool("out",true);

        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Ball"))
            database.BallOnProcess=null;
            UIpanelgroup.SetBool("out",false);
            UIpanelgroup.SetBool("in",true);
    }
}