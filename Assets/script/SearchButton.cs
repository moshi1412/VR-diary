using UnityEngine;

public class SearchButton : MonoBehaviour
{
    public Animator UIpanelgroup;
    public bool IsTriggered;
    private void Start()
    {
        IsTriggered=true;
        // UIpanelgroup=GameObject.FindWithTag("PanelGroup").GetComponent<Animator>();
    }
    
    public void OperationAfterChange()
    {
        Debug.Log("the player has touched the search button!");
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
            UIpanelgroup.SetBool("searchin",true);
            UIpanelgroup.SetBool("searchout",false);
        //}
    }
    public void MainPanelMovein()
    {
        //if (other.CompareTag("Ball"))
            UIpanelgroup.SetBool("searchout",true);
            UIpanelgroup.SetBool("searchin",false);
    }
    
}
