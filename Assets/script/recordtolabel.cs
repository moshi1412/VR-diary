using UnityEngine;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine.UI;  // 用于UI显示（新增）

public class VoiceInteractionManager : MonoBehaviour
{
    [SerializeField] private string pythonScriptPath = "D://code//PythonProject//stt.py";
    [SerializeField] private Text recognizedTextUI;  // 显示识别文本的UI组件
    [SerializeField] private Text deepseekResultUI;  // 显示大模型结果的UI组件
    private string streamingAssetsPath => Application.streamingAssetsPath;

    // 核心方法：本地音频转文字 + 大模型处理
    public void ConvertLocalAudioToText(string localAudioPath)
    {
        if (!File.Exists(localAudioPath))
        {
            UnityEngine.Debug.LogError($"本地文件不存在：{localAudioPath}");
            UpdateUI("文件不存在", "");
            return;
        }
        UnityEngine.Debug.Log($"处理本地文件：{localAudioPath}");
        CallPythonScriptWithPath(localAudioPath);
    }

    // 调用Python并传递文件路径
    private void CallPythonScriptWithPath(string audioPath)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "python",  // 路径不正确则替换为完整Python路径
            Arguments = $"{pythonScriptPath} \"{audioPath}\"",
            WorkingDirectory = Application.dataPath + "/../",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8
        };

        Process process = new Process { StartInfo = startInfo };
        process.OutputDataReceived += OnPythonOutput;
        process.ErrorDataReceived += OnPythonError;
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();
        process.Close();
    }

    // 解析Python输出（包含识别文本和大模型结果）
    private void OnPythonOutput(object sender, DataReceivedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Data)) return;
        UnityEngine.Debug.Log($"[Python输出] {e.Data}");

        if (e.Data.StartsWith("[UNITY_RESULT]"))
        {
            string[] parts = e.Data.Split('|');
            if (parts.Length >= 3)
            {
                string recognizedText = parts[1];
                string deepseekResult = parts[2];
                UnityEngine.Debug.Log($"识别结果：{recognizedText}\n大模型处理结果：{deepseekResult}");
                // 更新UI显示（需在主线程执行）
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    UpdateUI(recognizedText, deepseekResult);
                });
            }
        }
    }

    // 更新UI文本
    private void UpdateUI(string recognizedText, string deepseekResult)
    {
        if (recognizedTextUI != null)
            recognizedTextUI.text = $"识别文本：{recognizedText}";
        if (deepseekResultUI != null)
            deepseekResultUI.text = $"关键信息标签：{deepseekResult}";
    }

    // 错误处理
    private void OnPythonError(object sender, DataReceivedEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Data))
        {
            UnityEngine.Debug.LogError($"[Python错误] {e.Data}");
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                UpdateUI("识别失败", e.Data);
            });
        }
    }

    // 测试按钮调用
    public void TestLocalAudioConversion()
    {
        string testAudioPath = Path.Combine(streamingAssetsPath, "local_rec.wav");
        ConvertLocalAudioToText(testAudioPath);
    }
}

// 辅助类：解决Unity多线程更新UI问题（直接复制到脚本中）
public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static UnityMainThreadDispatcher instance;
    private System.Collections.Generic.Queue<Action> actions = new System.Collections.Generic.Queue<Action>();

    public static UnityMainThreadDispatcher Instance()
    {
        if (instance == null)
        {
            instance = FindObjectOfType<UnityMainThreadDispatcher>();
            if (instance == null)
            {
                GameObject obj = new GameObject("UnityMainThreadDispatcher");
                instance = obj.AddComponent<UnityMainThreadDispatcher>();
            }
        }
        return instance;
    }

    private void Update()
    {
        lock (actions)
        {
            while (actions.Count > 0)
            {
                actions.Dequeue().Invoke();
            }
        }
    }

    public void Enqueue(Action action)
    {
        lock (actions)
        {
            actions.Enqueue(action);
        }
    }
}