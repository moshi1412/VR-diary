using UnityEngine;

public class SearchButton : MonoBehaviour
{
    private Animator UIpanelgroup;
    public bool IsTriggered;
    private void Start()
    {
        IsTriggered=true;
        UIpanelgroup=GameObject.FindWithTag("PanelGroup").GetComponent<Animator>();
    }
    
    public void OperationAfterChange()
    {
        if (!IsTriggered)
        {
            MainPanelMovein();
            IsTriggered = false;
        }
        else
        {
            MainPanelMoveout();
            IsTriggered = true;
        }
    }
    public void MainPanelMoveout()
    {
        //if (other.CompareTag("Ball"))
        //{
            UIpanelgroup.SetBool("searchin",false);
            UIpanelgroup.SetBool("searchout",true);
        //}
    }
    public void MainPanelMovein()
    {
        //if (other.CompareTag("Ball"))
            UIpanelgroup.SetBool("searchout",false);
            UIpanelgroup.SetBool("searchin",true);
    }
    
}
