using UnityEngine;
using UnityEngine.UI;
public class VideoButton : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public  Animator mainpanel;
    public Animator Videopanel;
    public Toggle vb;
    void Start()
    {
        mainpanel.SetBool("videoin",false);
        mainpanel.SetBool("videoout",false);
        Videopanel.SetBool("videoin",false);
        Videopanel.SetBool("videoin",false);
        vb.isOn=false;
    }

    public void ToggleChanged()
    {
        if (vb.isOn)
        {
            
            mainpanel.SetBool("videoin",true);
            mainpanel.SetBool("videoout",false);
            Videopanel.SetBool("videoin",true);
            Videopanel.SetBool("videoout",false);
        }
        else
        {
            mainpanel.SetBool("videoin",false);
            mainpanel.SetBool("videoout",true);
            Videopanel.SetBool("videoin",false);
            Videopanel.SetBool("videoout",true);
        }
    }
}
