using System;
using System.IO;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.iOS;
using TMPro; // 引入TextMeshPro命名空间
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class TMP_ToggleAudioRecorder : MonoBehaviour
{
    [Header("UI组件")]
    public Toggle recordToggle;          // 控制录音的Toggle
    public TextMeshProUGUI statusText;   // TextMeshPro状态文本（替换原生Text）

    private AudioSource audioSource;
    private string microphoneDevice;
    private AudioClip recordedClip;
    private bool isRecording = false;

    // 保存路径
    private string savePath => Path.Combine(Application.persistentDataPath, "Recordings");


    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        recordToggle.isOn=false;
        // 绑定Toggle事件
        recordToggle.onValueChanged.AddListener(OnToggleValueChanged);

        // 初始状态（TextMeshPro文本设置）
        statusText.text = "Save your memory";

        // 请求麦克风权限
        RequestMicrophonePermission();
    }


    private void OnToggleValueChanged(bool isOn)
    {
        if (isOn)
        {
            Debug.Log("value changed ! Start_recording !");
            StartRecording(); // 开启Toggle：开始录音
        }
        else
        {
            StopAndSaveRecording(); // 关闭Toggle：停止并保存
        }
    }


    private void RequestMicrophonePermission()
    {
#if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {

            Permission.RequestUserPermission(Permission.Microphone);
        }
#elif UNITY_IOS
        if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
        {
            Application.RequestUserAuthorization(UserAuthorization.Microphone);
        }
#endif
    }


    private void StartRecording()
    {
        if (isRecording) return;

        string[] devices = Microphone.devices;
        
        if (devices.Length == 0)
        {
            statusText.text = "no micphone detected";
            recordToggle.isOn = false;
            return;
        }
        Debug.Log($"first available device:{devices[0]}");
        microphoneDevice = devices[0];
        recordedClip = Microphone.Start(microphoneDevice, false, 600, 44100);
        isRecording = true;

        // 更新TextMeshPro文本
        statusText.text = "Recording now...";
    }


    private void StopAndSaveRecording()
    {
        if (!isRecording) return;

        Microphone.End(microphoneDevice);
        isRecording = false;

        SaveRecording();
    }


    private void SaveRecording()
    {
        if (recordedClip == null)
        {
            statusText.text = "no data";
            return;
        }

        try
        {
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
            }

            string fileName = $"rec_{DateTime.Now:yyyyMMddHHmmss}.wav";
            string fullPath = Path.Combine(savePath, fileName);

            byte[] wavData = ConvertToWAV(recordedClip);
            File.WriteAllBytes(fullPath, wavData);

            // TextMeshPro支持换行符，显示更清晰
            statusText.text = $"already saved";
            Debug.Log($"save path:{fullPath}");
        }
        catch (Exception e)
        {
            statusText.text = $"error for saving：\n{e.Message}";
            Debug.LogError($"saving error：{e}");
        }
    }


    // 以下为WAV转换相关方法（与之前一致）
    private byte[] ConvertToWAV(AudioClip clip)
    {
        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        byte[] pcmBytes = new byte[samples.Length * 2];
        for (int i = 0; i < samples.Length; i++)
        {
            short pcmValue = (short)(samples[i] * short.MaxValue);
            byte[] valueBytes = BitConverter.GetBytes(pcmValue);
            pcmBytes[i * 2] = valueBytes[0];
            pcmBytes[i * 2 + 1] = valueBytes[1];
        }

        byte[] wavBytes = new byte[44 + pcmBytes.Length];
        WriteWavHeader(wavBytes, clip, pcmBytes.Length);
        pcmBytes.CopyTo(wavBytes, 44);

        return wavBytes;
    }


    private void WriteWavHeader(byte[] wavBytes, AudioClip clip, int pcmLength)
    {
        WriteString(wavBytes, 0, "RIFF");
        int fileSize = 36 + pcmLength;
        BitConverter.GetBytes(fileSize).CopyTo(wavBytes, 4);
        WriteString(wavBytes, 8, "WAVE");

        WriteString(wavBytes, 12, "fmt ");
        BitConverter.GetBytes(16).CopyTo(wavBytes, 16);
        BitConverter.GetBytes((short)1).CopyTo(wavBytes, 20);
        BitConverter.GetBytes((short)clip.channels).CopyTo(wavBytes, 22);
        BitConverter.GetBytes(clip.frequency).CopyTo(wavBytes, 24);
        int byteRate = clip.frequency * clip.channels * 2;
        BitConverter.GetBytes(byteRate).CopyTo(wavBytes, 28);
        short blockAlign = (short)(clip.channels * 2);
        BitConverter.GetBytes(blockAlign).CopyTo(wavBytes, 32);
        BitConverter.GetBytes((short)16).CopyTo(wavBytes, 34);

        WriteString(wavBytes, 36, "data");
        BitConverter.GetBytes(pcmLength).CopyTo(wavBytes, 40);
    }


    private void WriteString(byte[] array, int index, string value)
    {
        byte[] strBytes = System.Text.Encoding.ASCII.GetBytes(value);
        Array.Copy(strBytes, 0, array, index, strBytes.Length);
    }
}