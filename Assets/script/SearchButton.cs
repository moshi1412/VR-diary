using UnityEngine;

public class SearchButton : MonoBehaviour
{
    private Animator UIpanelgroup;
    private void Start()
    {
        UIpanelgroup=GameObject.FindWithTag("PanelGroup").GetComponent<Animator>();
    }

    public void MainPanelMoveout()
    {
        if (other.CompareTag("Ball"))
        {
            UIpanelgroup.SetBool("searchin",false);
            UIpanelgroup.SetBool("searchout",true);
        }
    }
    public void MainPanelMovein()
    {
        if (other.CompareTag("Ball"))
            UIpanelgroup.SetBool("searchout",false);
            UIpanelgroup.SetBool("searchin",true);
    }
}
