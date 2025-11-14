using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class AudioAnalyzer : MonoBehaviour
{
    // 硅基流动API配置
    [Header("硅基流动API配置")]
    private string siliconFlowApiKey ="sk-hrxhrcovururgimnfqxwoggwysrzgahwaplycsyowitwgxnf" ;

    // 百度智能云API配置
    [Header("百度智能云API配置")]
    private string baiduApiKey = "UbjdHrXLaZWF3B0jiuBCOvfx";
    private string baiduSecretKey ="qXm1FPw9RtLzyq0ryqJ9tf4bnH47uMEC";

    // 音频文件路径（外部传入）
    [Header("文件路径")]
    public string audioFilePath = "";

    // 分析结果回调（包含情绪结果和标签字符串）
    private Action<string, string, string> onAnalysisCompleted; // 参数：情绪结果、关键词、合并标签

    /// <summary>
    /// 外部调用此方法开始处理音频并获取结果
    /// </summary>
    /// <param name="onCompleted">回调函数：(情绪结果, 关键词字符串, 合并标签字符串)</param>
    public void ProcessAudioAndGetResults(Action<string, string, string> onCompleted)
    {
        onAnalysisCompleted = onCompleted;

        // 参数校验
        if (string.IsNullOrEmpty(siliconFlowApiKey))
        {
            InvokeCallback("错误：请设置硅基流动API密钥", "", "");
            return;
        }

        if (string.IsNullOrEmpty(baiduApiKey) || string.IsNullOrEmpty(baiduSecretKey))
        {
            InvokeCallback("错误：请设置百度智能云API密钥和Secret Key", "", "");
            return;
        }
        DataManager dm=GameObject.FindWithTag("DataManager").GetComponent<DataManager>();
        BallMemory.MemoryData? md=dm.BallOnProcess.GetComponent<BallMemory>().BallData;
        if(!md.HasValue)
        {
            InvokeCallback($"错误：球中不存在记录", "", "");
            return;
        }
        audioFilePath=md.Value.recordingpath;
        if (string.IsNullOrEmpty(audioFilePath) || !File.Exists(audioFilePath))
        {
            InvokeCallback($"错误：音频文件不存在 → {audioFilePath}", "", "");
            return;
        }

        StartCoroutine(ProcessAudioCoroutine());
    }

    /// <summary>
    /// 处理音频的协程
    /// </summary>
    private IEnumerator ProcessAudioCoroutine()
    {
        // 1. 音频转文字
        string recognizedText = null;
        yield return StartCoroutine(AudioToText(audioFilePath, siliconFlowApiKey, (result) => recognizedText = result));

        if (string.IsNullOrEmpty(recognizedText))
        {
            InvokeCallback("识别失败", "", "[UNITY_RESULT]|识别失败|无");
            yield break;
        }

        Debug.Log($"[UNITY_RESULT]|{recognizedText}|无");

        // 2. 文本分析
        AnalysisResult analysisResult = null;
        yield return StartCoroutine(AnalyzeText(recognizedText, baiduApiKey, baiduSecretKey, (result) => analysisResult = result));

        // 3. 生成结果并返回
        if (analysisResult != null)
        {
            string sentimentResult = string.Join("、", analysisResult.sentimentTags);
            string keywords = string.Join("、", analysisResult.keywordTags);
            string combinedTags = GenerateCombinedTags(sentimentResult, keywords);
            
            InvokeCallback(sentimentResult, keywords, combinedTags);
        }
        else
        {
            InvokeCallback("文本分析失败", "", "文本分析失败，无法生成标签");
        }
    }

    /// <summary>
    /// 生成合并的标签字符串
    /// </summary>
    private string GenerateCombinedTags(string sentiment, string keywords)
    {
        return $"情绪标签：{sentiment} | 关键词标签：{keywords}";
    }

    /// <summary>
    /// 调用回调函数
    /// </summary>
    private void InvokeCallback(string sentiment, string keywords, string combined)
    {
        onAnalysisCompleted?.Invoke(sentiment, keywords, combined);
        onAnalysisCompleted = null; // 清空回调避免重复调用
    }

    /// <summary>
    /// 音频转文字（硅基流动API）
    /// </summary>
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
            Debug.LogError($"读取音频文件失败：{e.Message}");
            onComplete?.Invoke(null);
            yield break;
        }

        form.AddBinaryData("file", audioData, Path.GetFileName(audioPath));

        UnityWebRequest request = UnityWebRequest.Post(url, form);
        request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

        Debug.Log($"开始识别音频：{audioPath}");
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
                Debug.LogError($"解析响应失败：{e.Message}");
                onComplete?.Invoke(null);
            }
        }
        else
        {
            Debug.LogError($"识别失败：{request.error}");
            onComplete?.Invoke(null);
        }

        request.Dispose();
    }

    /// <summary>
    /// 文本分析（百度API）
    /// </summary>
    private IEnumerator AnalyzeText(string text, string apiKey, string secretKey, Action<AnalysisResult> onComplete)
    {
        if (string.IsNullOrEmpty(text))
        {
            onComplete?.Invoke(null);
            yield break;
        }

        // 获取访问令牌
        string accessToken = null;
        yield return StartCoroutine(GetBaiduAccessToken(apiKey, secretKey, (token) => accessToken = token));

        if (string.IsNullOrEmpty(accessToken))
        {
            onComplete?.Invoke(null);
            yield break;
        }

        // 情绪分析
        SentimentResult sentimentResult = null;
        yield return StartCoroutine(AnalyzeSentiment(text, accessToken, (result) => sentimentResult = result));

        // 关键词提取
        KeywordResult keywordResult = null;
        yield return StartCoroutine(ExtractKeywords(text, accessToken, (result) => keywordResult = result));

        // 整理结果
        AnalysisResult result = new AnalysisResult();
        
        if (sentimentResult != null && sentimentResult.items != null && sentimentResult.items.Length > 0)
        {
            int sentiment = sentimentResult.items[0].sentiment;
            string[] sentimentLabels = { "负面", "中性", "正面" };
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

    /// <summary>
    /// 获取百度API访问令牌
    /// </summary>
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

    /// <summary>
    /// 情绪分析
    /// </summary>
    private IEnumerator AnalyzeSentiment(string text, string accessToken, Action<SentimentResult> onComplete)
    {
        string url = $"https://aip.baidubce.com/rpc/2.0/nlp/v1/sentiment_classify?access_token={accessToken}";

        string limitedText = text.Length > 2000 ? text.Substring(0, 2000) : text;
        var data = new SentimentRequestData { text = limitedText };
        string jsonData = JsonUtility.ToJson(data);

        UnityWebRequest request = UnityWebRequest.PostWwwForm(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
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

    /// <summary>
    /// 提取关键词
    /// </summary>
    private IEnumerator ExtractKeywords(string text, string accessToken, Action<KeywordResult> onComplete)
    {
        string url = $"https://aip.baidubce.com/rpc/2.0/nlp/v1/keyword?access_token={accessToken}";

        string limitedText = text.Length > 2000 ? text.Substring(0, 2000) : text;
        string title = text.Length > 20 ? text.Substring(0, 20) : text;
        
        var data = new KeywordRequestData { content = limitedText, title = title };
        string jsonData = JsonUtility.ToJson(data);

        UnityWebRequest request = UnityWebRequest.PostWwwForm(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
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

    // 数据模型类
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
        public List<string> sentimentTags = new List<string>();
        public List<string> keywordTags = new List<string>();
    }
}