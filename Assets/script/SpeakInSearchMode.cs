using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.Networking; // 用于 UnityWebRequest 网络请求
[RequireComponent(typeof(AudioSource))]
public class ReverseToggleRecorderWithAnalysis : MonoBehaviour
{
    [Header("UI Components")]
    public Toggle recordToggle;          // Toggle to control recording
    public TextMeshProUGUI statusText;   // Text to display current status

    [Header("Recording Settings")]
    public int maxRecordingSeconds = 600; // Maximum recording duration (seconds)
    public int sampleRate = 44100;       // Sample rate

    [Header("API Configuration")]
    public string siliconFlowApiKey = "";  // Silicon Flow API key
    public string baiduApiKey = "";        // Baidu API key
    public string baiduSecretKey = "";     // Baidu Secret Key

    // Core recording variables
    private AudioSource audioSource;
    private string microphoneDevice;
    private AudioClip recordedClip;
    private bool isRecording = false;
    private string currentRecordingPath;  // Current recording save path
    private AudioAnalyzer audioAnalyzer;  // Audio analyzer instance

    // Recording save path
    private string savePath => Path.Combine(Application.persistentDataPath, "Recordings");

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        recordToggle.onValueChanged.AddListener(OnToggleValueChanged);
        UpdateStatusText("Ready");

        // Initialize audio analyzer
        audioAnalyzer = gameObject.AddComponent<AudioAnalyzer>();

        // Request microphone permission
        TMP_ToggleAudioRecorderExtensions.RequestMicrophonePermissionStatic();
    }

    // Triggered when toggle value changes (core logic)
    private void OnToggleValueChanged(bool isOn)
    {
        if (!isOn)
        {
            // From true→false: Start recording
            if (!isRecording) StartRecording();
        }
        else
        {
            // From false→true: Stop and save recording
            if (isRecording) StopAndSaveRecording();
        }
    }

    // Start recording
    private void StartRecording()
    {
        string[] devices = Microphone.devices;
        if (devices.Length == 0)
        {
            UpdateStatusText("No microphone detected");
            recordToggle.isOn = true; // Restore state
            return;
        }

        microphoneDevice = devices[0];
        recordedClip = Microphone.Start(microphoneDevice, false, maxRecordingSeconds, sampleRate);
        isRecording = true;
        UpdateStatusText("Recording in progress...");
        Debug.Log("Started recording");
    }

    // Stop and save recording
    private void StopAndSaveRecording()
    {
        if (!isRecording || recordedClip == null) return;

        // Stop recording
        Microphone.End(microphoneDevice);
        isRecording = false;
        UpdateStatusText("Saving recording...");

        try
        {
            // Create save directory if not exists
            if (!Directory.Exists(savePath))
                Directory.CreateDirectory(savePath);

            // Generate unique filename
            string fileName = $"rec_{DateTime.Now:yyyyMMddHHmmss}.wav";
            currentRecordingPath = Path.Combine(savePath, fileName);

            // Convert to WAV and save (reuse original class method)
            byte[] wavData = TMP_ToggleAudioRecorderExtensions.ConvertToWAVStatic(recordedClip);
            File.WriteAllBytes(currentRecordingPath, wavData);

            UpdateStatusText($"Recording saved: {fileName}\nAnalyzing audio...");
            Debug.Log($"Recording path: {currentRecordingPath}");

            // Start audio analysis process
            StartAudioAnalysis();
        }
        catch (Exception e)
        {
            UpdateStatusText($"Save failed: {e.Message}");
            Debug.LogError($"Save error: {e}");
        }
    }

    // Start audio analysis
    private void StartAudioAnalysis()
    {
        // Configure analyzer parameters
        audioAnalyzer.siliconFlowApiKey = siliconFlowApiKey;
        audioAnalyzer.baiduApiKey = baiduApiKey;
        audioAnalyzer.baiduSecretKey = baiduSecretKey;
        audioAnalyzer.audioFilePath = currentRecordingPath;

        // Start analysis and handle results
        audioAnalyzer.ProcessAudioAndGetResults((sentiment, keywords, combined) => 
        {
            UpdateStatusText(
                $"Analysis complete!\n" +
                $"Sentiment: {sentiment}\n" +
                $"Keywords: {keywords}"
            );
            Debug.Log($"[Analysis Result] {combined}");
        });
    }

    // Update status text
    private void UpdateStatusText(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }

    // Stop recording when application quits
    private void OnApplicationQuit()
    {
        if (isRecording)
            Microphone.End(microphoneDevice);
    }
}

// Original class method extensions (reusable utility methods)
public static class TMP_ToggleAudioRecorderExtensions
{
    // Static microphone permission request method
    public static void RequestMicrophonePermissionStatic()
    {
#if UNITY_ANDROID
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Microphone))
        {
            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Microphone);
        }
#elif UNITY_IOS
        if (!UnityEngine.iOS.Application.HasUserAuthorization(UnityEngine.iOS.UserAuthorization.Microphone))
        {
            _ = UnityEngine.iOS.Application.RequestUserAuthorization(UnityEngine.iOS.UserAuthorization.Microphone);
        }
#endif
    }

    // Static WAV conversion method
    public static byte[] ConvertToWAVStatic(AudioClip clip)
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
        WriteWavHeaderStatic(wavBytes, clip, pcmBytes.Length);
        pcmBytes.CopyTo(wavBytes, 44);

        return wavBytes;
    }

    // Static WAV header writing method
    private static void WriteWavHeaderStatic(byte[] wavBytes, AudioClip clip, int pcmLength)
    {
        WriteStringStatic(wavBytes, 0, "RIFF");
        int fileSize = 36 + pcmLength;
        BitConverter.GetBytes(fileSize).CopyTo(wavBytes, 4);
        WriteStringStatic(wavBytes, 8, "WAVE");

        WriteStringStatic(wavBytes, 12, "fmt ");
        BitConverter.GetBytes(16).CopyTo(wavBytes, 16);
        BitConverter.GetBytes((short)1).CopyTo(wavBytes, 20);
        BitConverter.GetBytes((short)clip.channels).CopyTo(wavBytes, 22);
        BitConverter.GetBytes(clip.frequency).CopyTo(wavBytes, 24);
        int byteRate = clip.frequency * clip.channels * 2;
        BitConverter.GetBytes(byteRate).CopyTo(wavBytes, 28);
        short blockAlign = (short)(clip.channels * 2);
        BitConverter.GetBytes(blockAlign).CopyTo(wavBytes, 32);
        BitConverter.GetBytes((short)16).CopyTo(wavBytes, 34);

        WriteStringStatic(wavBytes, 36, "data");
        BitConverter.GetBytes(pcmLength).CopyTo(wavBytes, 40);
    }

    // Static string writing method
    private static void WriteStringStatic(byte[] array, int index, string value)
    {
        byte[] strBytes = System.Text.Encoding.ASCII.GetBytes(value);
        Array.Copy(strBytes, 0, array, index, strBytes.Length);
    }
}

// Audio analyzer class (integrated from original AudioAnalyzer logic)
public class AudioAnalyzer : MonoBehaviour
{
    [Header("Silicon Flow API Config")]
    public string siliconFlowApiKey = "";

    [Header("Baidu API Config")]
    public string baiduApiKey = "";
    public string baiduSecretKey = "";

    [Header("File Path")]
    public string audioFilePath = "";

    private Action<string, string, string> onAnalysisCompleted;

    public void ProcessAudioAndGetResults(Action<string, string, string> onCompleted)
    {
        onAnalysisCompleted = onCompleted;

        // Parameter validation
        if (string.IsNullOrEmpty(siliconFlowApiKey))
        {
            InvokeCallback("Error: Please set Silicon Flow API key", "", "");
            return;
        }

        if (string.IsNullOrEmpty(baiduApiKey) || string.IsNullOrEmpty(baiduSecretKey))
        {
            InvokeCallback("Error: Please set Baidu API key and Secret Key", "", "");
            return;
        }

        if (string.IsNullOrEmpty(audioFilePath) || !File.Exists(audioFilePath))
        {
            InvokeCallback($"Error: Audio file not found → {audioFilePath}", "", "");
            return;
        }

        StartCoroutine(ProcessAudioCoroutine());
    }

    private IEnumerator ProcessAudioCoroutine()
    {
        // 1. Convert audio to text
        string recognizedText = null;
        yield return StartCoroutine(AudioToText(audioFilePath, siliconFlowApiKey, (result) => recognizedText = result));

        if (string.IsNullOrEmpty(recognizedText))
        {
            InvokeCallback("Recognition failed", "", "[UNITY_RESULT]|Recognition failed|None");
            yield break;
        }

        Debug.Log($"[Recognition Result] {recognizedText}");

        // 2. Analyze text
        AnalysisResult analysisResult = null;
        yield return StartCoroutine(AnalyzeText(recognizedText, baiduApiKey, baiduSecretKey, (result) => analysisResult = result));

        // 3. Generate and return results
        if (analysisResult != null)
        {
            string sentimentResult = string.Join(", ", analysisResult.sentimentTags);
            string keywords = string.Join(", ", analysisResult.keywordTags);
            string combinedTags = GenerateCombinedTags(sentimentResult, keywords);
            
            InvokeCallback(sentimentResult, keywords, combinedTags);
        }
        else
        {
            InvokeCallback("Text analysis failed", "", "Text analysis failed, unable to generate tags");
        }
    }

    private string GenerateCombinedTags(string sentiment, string keywords)
    {
        return $"Sentiment tags: {sentiment} | Keyword tags: {keywords}";
    }

    private void InvokeCallback(string sentiment, string keywords, string combined)
    {
        onAnalysisCompleted?.Invoke(sentiment, keywords, combined);
        onAnalysisCompleted = null;
    }

    private IEnumerator AudioToText(string audioPath, string apiKey, Action<string> onComplete)
    {
        string url = "https://api.siliconflow.cn/v1/audio/transcriptions";

        WWWForm form = new WWWForm();
        form.AddField("model", "FunAudioLLM/SenseVoiceSmall");

        byte[] audioData;
        try
        {
            audioData = File.ReadAllBytes(audioPath);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to read audio file: {e.Message}");
            onComplete?.Invoke(null);
            yield break;
        }

        form.AddBinaryData("file", audioData, Path.GetFileName(audioPath));

        UnityWebRequest request = UnityWebRequest.Post(url, form);
        request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

        Debug.Log($"Starting audio recognition: {audioPath}");
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            try
            {
                var responseJson = JsonUtility.FromJson<SiliconFlowResponse>(request.downloadHandler.text);
                onComplete?.Invoke(responseJson.text);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to parse response: {e.Message}");
                onComplete?.Invoke(null);
            }
        }
        else
        {
            Debug.LogError($"Recognition failed: {request.error}");
            onComplete?.Invoke(null);
        }

        request.Dispose();
    }

    private IEnumerator AnalyzeText(string text, string apiKey, string secretKey, Action<AnalysisResult> onComplete)
    {
        if (string.IsNullOrEmpty(text))
        {
            onComplete?.Invoke(null);
            yield break;
        }

        // Get access token
        string accessToken = null;
        yield return StartCoroutine(GetBaiduAccessToken(apiKey, secretKey, (token) => accessToken = token));

        if (string.IsNullOrEmpty(accessToken))
        {
            onComplete?.Invoke(null);
            yield break;
        }

        // Sentiment analysis
        SentimentResult sentimentResult = null;
        yield return StartCoroutine(AnalyzeSentiment(text, accessToken, (result) => sentimentResult = result));

        // Keyword extraction
        KeywordResult keywordResult = null;
        yield return StartCoroutine(ExtractKeywords(text, accessToken, (result) => keywordResult = result));

        // Organize results
        AnalysisResult result = new AnalysisResult();
        
        if (sentimentResult != null && sentimentResult.items != null && sentimentResult.items.Length > 0)
        {
            int sentiment = sentimentResult.items[0].sentiment;
            string[] sentimentLabels = { "Negative", "Neutral", "Positive" };
            if (sentiment >= 0 && sentiment < sentimentLabels.Length)
            {
                result.sentimentTags.Add(sentimentLabels[sentiment]);
            }
        }

        if (keywordResult != null && keywordResult.items != null)
        {
            foreach (var item in keywordResult.items)
            {
                result.keywordTags.Add(item.tag);
            }
        }

        onComplete?.Invoke(result);
    }

    private IEnumerator GetBaiduAccessToken(string apiKey, string secretKey, Action<string> onComplete)
    {
        string url = $"https://aip.baidubce.com/oauth/2.0/token?grant_type=client_credentials&client_id={apiKey}&client_secret={secretKey}";

        UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            try
            {
                var response = JsonUtility.FromJson<BaiduTokenResponse>(request.downloadHandler.text);
                onComplete?.Invoke(response.access_token);
            }
            catch
            {
                onComplete?.Invoke(null);
            }
        }
        else
        {
            onComplete?.Invoke(null);
        }

        request.Dispose();
    }

    private IEnumerator AnalyzeSentiment(string text, string accessToken, Action<SentimentResult> onComplete)
    {
        string url = $"https://aip.baidubce.com/rpc/2.0/nlp/v1/sentiment_classify?access_token={accessToken}";

        string limitedText = text.Length > 2000 ? text.Substring(0, 2000) : text;
        var data = new SentimentRequestData { text = limitedText };
        string jsonData = JsonUtility.ToJson(data);

        UnityWebRequest request = UnityWebRequest.PostWwwForm(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            try
            {
                var result = JsonUtility.FromJson<SentimentResult>(request.downloadHandler.text);
                onComplete?.Invoke(result);
            }
            catch
            {
                onComplete?.Invoke(null);
            }
        }
        else
        {
            onComplete?.Invoke(null);
        }

        request.Dispose();
    }

    private IEnumerator ExtractKeywords(string text, string accessToken, Action<KeywordResult> onComplete)
    {
        string url = $"https://aip.baidubce.com/rpc/2.0/nlp/v1/keyword?access_token={accessToken}";

        string limitedText = text.Length > 2000 ? text.Substring(0, 2000) : text;
        string title = text.Length > 20 ? text.Substring(0, 20) : text;
        
        var data = new KeywordRequestData { content = limitedText, title = title };
        string jsonData = JsonUtility.ToJson(data);

        UnityWebRequest request = UnityWebRequest.PostWwwForm(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            try
            {
                var result = JsonUtility.FromJson<KeywordResult>(request.downloadHandler.text);
                onComplete?.Invoke(result);
            }
            catch
            {
                onComplete?.Invoke(null);
            }
        }
        else
        {
            onComplete?.Invoke(null);
        }

        request.Dispose();
    }

    // Data model classes
    [Serializable]
    private class SiliconFlowResponse { public string text; }

    [Serializable]
    private class BaiduTokenResponse { public string access_token; }

    [Serializable]
    private class SentimentRequestData { public string text; }

    [Serializable]
    private class SentimentResult { public SentimentItem[] items; }

    [Serializable]
    private class SentimentItem { public int sentiment; }

    [Serializable]
    private class KeywordRequestData { public string content; public string title; }

    [Serializable]
    private class KeywordResult { public KeywordItem[] items; }

    [Serializable]
    private class KeywordItem { public string tag; }

    [Serializable]
    private class AnalysisResult
    {
        public System.Collections.Generic.List<string> sentimentTags = new System.Collections.Generic.List<string>();
        public System.Collections.Generic.List<string> keywordTags = new System.Collections.Generic.List<string>();
    }
}