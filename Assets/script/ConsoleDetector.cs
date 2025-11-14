using UnityEngine;
public class ConsoleDetector : MonoBehaviour
{
    public Animator UIpanelgroup;
    public GameObject datamanager;
    //private void Start()
    //{

    //    UIpanelgroup=GameObject.FindWithTag("PanelGroup").GetComponent<Animator>();
    //}
    private void OnTriggerStay(Collider other)
    {
        DataManager database=datamanager.GetComponent<DataManager>();
        if (other.CompareTag("Ball")&&database.BallOnProcess is not null)
        {
            Debug.Log("detect the ball");
            if(database==null)
                {Debug.Log("database is null");}
            database.BallOnProcess=other.gameObject;
            Debug.Log("当前线程ID：" + System.Threading.Thread.CurrentThread.ManagedThreadId); 
            Debug.Log(other.gameObject.tag);
            UIpanelgroup.SetBool("in",false);
            UIpanelgroup.SetBool("out",true);

        }
    }
    // private void OnTriggerExit(Collider other)
    // {
    //     DataManager database = datamanager.GetComponent<DataManager>();
    //     if (other.CompareTag("Ball"))
    //     {    database.BallOnProcess=null;
    //         UIpanelgroup.SetBool("out",false);
    //         UIpanelgroup.SetBool("in",true);
    //     }
    // }
}