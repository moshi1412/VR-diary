using System;
using System.IO;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.iOS;
using TMPro;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class TMP_ToggleAudioRecorder : MonoBehaviour
{
    [Header("UI组件")]
    public Toggle recordToggle;          
    public TextMeshProUGUI statusText;   

    [Header("数据管理")]
    public int targetMemoryId; // 录音关联的唯一记忆ID（需手动设置或通过其他逻辑获取）
    private DataManager dataManager; 
    private AudioSource audioSource;
    private string microphoneDevice;
    private AudioClip recordedClip;
    private bool isRecording = false;

    private string savePath => Path.Combine(Application.persistentDataPath, "Recordings");


    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        recordToggle.isOn = false;
        recordToggle.onValueChanged.AddListener(OnToggleValueChanged);
        statusText.text = "Save your memory";

        // 查找DataManager（通过标签）
        dataManager = GameObject.FindWithTag("DataManager")?.GetComponent<DataManager>();
        if (dataManager == null)
        {
            Debug.LogError("DataManager not found! Ensure it has tag 'DataManager'.");
        }

        RequestMicrophonePermission();
    }


    private void OnToggleValueChanged(bool isOn)
    {
        if (isOn)
        {
            Debug.Log("Start recording !");
            StartRecording();
        }
        else
        {
            StopAndSaveRecording();
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
            statusText.text = "No microphone detected";
            recordToggle.isOn = false;
            return;
        }
        
        microphoneDevice = devices[0];
        recordedClip = Microphone.Start(microphoneDevice, false, 600, 44100);
        isRecording = true;
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
            statusText.text = "No recording data";
            return;
        }

        try
        {
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
            }

            // 生成唯一文件名（包含时间戳）
            string fileName = $"rec_{DateTime.Now:yyyyMMddHHmmss}.wav";
            string fullPath = Path.Combine(savePath, fileName);

            // 保存WAV文件
            byte[] wavData = ConvertToWAV(recordedClip);
            File.WriteAllBytes(fullPath, wavData);

            // 保存路径到DataManager（适配新逻辑）
            SavePathToDataManager(fullPath);

            statusText.text = $"Saved:\n{fileName}";
            Debug.Log($"Saved to: {fullPath}");
        }
        catch (Exception e)
        {
            statusText.text = $"Save failed:\n{e.Message}";
            Debug.LogError($"Save error: {e}");
        }
    }


    // 核心：将录音路径存入DataManager（适配新逻辑）
    private void SavePathToDataManager(string recordingPath)
    {
        if (dataManager == null)
        {
            Debug.LogError("DataManager is null, cannot save path!");
            return;
        }

        // 1. 先尝试获取该ID已有的数据（如果存在）
        bool hasExistingData = dataManager.FetchDataById(targetMemoryId);
        DataManager.MemoryData newData;

        if (hasExistingData)
        {
            // 2. 若存在，更新录音路径（保留其他字段）
            newData = dataManager.GetCurrentData();
            newData.recordingpath = recordingPath;
            newData.createTime = DateTime.Now.ToString(); // 更新时间戳
        }
        else
        {
            // 3. 若不存在，创建新数据（必须包含唯一memoryId）
            newData = new DataManager.MemoryData()
            {
                memoryId = targetMemoryId, // 关键：指定唯一ID
                recordingpath = recordingPath,
                createTime = DateTime.Now.ToString(),
                description = "Audio recording", // 默认描述（可扩展）
                picturepath = "" // 留空，可后续补充
            };
        }

        // 4. 保存数据（覆盖式存储，新DataManager会处理文件读写）
        dataManager.AddData(newData);
        Debug.Log($"Recording path saved to memory ID: {targetMemoryId}");
    }


    // WAV转换相关方法（保持不变）
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