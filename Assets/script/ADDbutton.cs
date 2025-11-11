using UnityEngine;

public class RealButton : MonoBehaviour
{
    private GameObject ballmanager;
    private BallOperation balloperation;
    private Animator UIpanelgroup;
    private void Start()
    {
        ballmanager=GameObject.FindWithTag("BallManager");
        balloperation=ballmanager.GetComponent<BallOperation>();
        
    }

    public void PressedDownAdd()
    {
        Debug.Log("the player has pressed the button");
        balloperation.BallGenerate();
        //Debug
        GameObject voiceManager=GameObject.Find("VoiceInteractionManager");
        VoiceInteractionManager vm=voiceManager.GetComponent<VoiceInteractionManager>();
        vm.ConvertLocalAudioToText();
    } 
}
