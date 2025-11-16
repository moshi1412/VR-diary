using UnityEngine;
public class ConsoleDetector : MonoBehaviour
{
    public Animator UIpanelgroup;
    public GameObject datamanager;
    private BaseMagLevController basemag;
    private void Start()
    {
        basemag=gameObject.GetComponent<BaseMagLevController>();
        UIpanelgroup=GameObject.FindWithTag("PanelGroup").GetComponent<Animator>();
    }
    private void Update()
    {
        DataManager database=datamanager.GetComponent<DataManager>();
        if(basemag.balls.Count==0)
        { 
                // Debug.Log("no ball detected");
                if(database==null)
                    {Debug.Log("database is null");}
                database.BallOnProcess=null;
                UIpanelgroup.SetBool("out",false);
                UIpanelgroup.SetBool("in",true);
                // UIpanelgroup.SetBool("searchin",false);
                // UIpanelgroup.SetBool("searchout",false);

        }
        else{
            // Debug.Log("detect the ball");
            if(database==null)
                {Debug.Log("database is null");}                
            database.BallOnProcess=basemag.balls[basemag.balls.Count - 1].gameObject;
            // Debug.Log("当前线程ID：" + System.Threading.Thread.CurrentThread.ManagedThreadId); 
            // Debug.Log(other.gameObject.tag);
            UIpanelgroup.SetBool("in",false);
            UIpanelgroup.SetBool("out",true);
            UIpanelgroup.SetBool("searchin",false);
            UIpanelgroup.SetBool("searchout",true);

        }
       
    }
    

}