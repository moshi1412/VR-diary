using UnityEngine;
public class ConsoleDetector : MonoBehaviour
{
    private Animator UIpanelgroup;
    private void Start()
    {
        UIpanelgroup=GameObject.FindWithTag("PanelGroup").GetComponent<Animator>();
    }
    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Ball"))
        {
            UIpanelgroup.SetBool("in",false);
            UIpanelgroup.SetBool("out",true);

        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Ball"))
            UIpanelgroup.SetBool("out",false);
            UIpanelgroup.SetBool("in",true);
    }
}