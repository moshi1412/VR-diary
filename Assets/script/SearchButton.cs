using UnityEngine;

public class SearchButton : MonoBehaviour
{
    private Animator UIpanelgroup;
    private void Start()
    {
        UIpanelgroup=GameObject.FindWithTag("PanelGroup").GetComponent<Animator>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ball"))
        {
            UIpanelgroup.SetBool("searchin",false);
            UIpanelgroup.SetBool("searchout",true);

        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Ball"))
            UIpanelgroup.SetBool("searchout",false);
            UIpanelgroup.SetBool("searchin",true);
    }
}
