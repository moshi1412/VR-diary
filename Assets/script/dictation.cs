using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Oculus.Voice.Dictation;
using UnityEngine.UI;
public class VoiceScript : MonoBehaviour
{
    
    public AppDictationExperience voiceExperience;
    public Toggle dic_toggle;  
    // Start is called before the first frame update
    void Start()
    {
        dic_toggle.isOn=false;
    }

    // Update is called once per frame
    void Update()
    {
        if (dic_toggle.isOn==true)
        {
            voiceExperience.Activate();
        }
    }
}