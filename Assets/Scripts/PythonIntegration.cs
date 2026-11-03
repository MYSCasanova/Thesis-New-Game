using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class PythonIntegration : MonoBehaviour
{
    [Header("Script Settings")]
    public string pipelineV2Exe = "Pipeline_V2.exe";
    public string pipelineV3Exe = "Pipeline_V3.exe";

    [Tooltip("Kill a pipeline process that runs longer than this many seconds.")]
    public int pipelineTimeoutSeconds = 180;

    [Header("V3 Output Ranges  (must match your slider ranges in S14_CategoryUI)")]
    // Pipeline_V2 / V3 export brightness/contrast on 0..2 and color on 0..360 (see run_inference()).
    // Unity's color slider is also 0..360, so color passes through unchanged (identity mapping).
    // If you ever change the color slider range, edit colorMin/colorMax and the value is rescaled.
    public float brightnessMin = 0f;
    public float brightnessMax = 2f;
    public float contrastMin = 0f;
    public float contrastMax = 2f;
    public float colorMin = 0f;
    public float colorMax = 360f;
    public float v3ColorSourceMax = 360f;

    // ---------------------------------------------------------------------
    // JSON models (shared by V2 and V3)
    // ---------------------------------------------------------------------

    // V2 and V3 payload (identical format): { "predicted_accessibility_level": "...", "recommended_settings": { 12 values } }
    [Serializable]
    public class V3Settings
    {
        public float background_color, background_contrast, background_brightness;
        public float obstacle_color, obstacle_contrast, obstacle_brightness;
        public float platform_color, platform_contrast, platform_brightness;
        public float player_color, player_contrast, player_brightness;
    }

    [Serializable]
    public class AIResultV3
    {
        public string predicted_accessibility_level;
        public V3Settings recommended_settings;
    }

    private class ProcessResult
    {
        public bool launched;
        public int exitCode = -1;
        public bool timedOut;
        public string stdout = "";
        public string stderr = "";
        public string launchError = "";
    }

    // ---------------------------------------------------------------------
    // Entry point
    // ---------------------------------------------------------------------
    public void CallPythonScript()
    {
        StartCoroutine(RunPipelinesAndLoad());
    }

    private IEnumerator RunPipelinesAndLoad()
    {
        // 1. Show loading screen at 50% before heavy work starts
        if (S18_LoadingManager.Instance != null && S18_LoadingManager.Instance.loadingPanel != null)
        {
            S18_LoadingManager.Instance.loadingPanel.SetActive(true);
            S18_LoadingManager.Instance.UpdateProgressBlocks(0.5f);
        }
        yield return null;

        // CSV locations are unchanged
        string baselinePath = Path.Combine(Application.dataPath, "VisualAccessibility_Baseline.csv");
        string playerPath = Path.Combine(Application.dataPath, "VisualAccessibility_Player.csv");

        bool v2Ok = false;
        bool v3Ok = false;

        if (!File.Exists(baselinePath) || !File.Exists(playerPath))
        {
            Debug.LogError($"Pipeline CSV missing. Baseline exists: {File.Exists(baselinePath)} ({baselinePath}), " +
                           $"Player exists: {File.Exists(playerPath)} ({playerPath})");
        }
        else
        {
            // 2. Pipeline V2 = same code as V3 with Pearson OFF (default in Pipeline_V2.py).
            //    Uses the same argparse flags as V3 and its own output folder so results never mix.
            string exeV2 = Path.Combine(Application.streamingAssetsPath, pipelineV2Exe);
            string outDirV2 = Path.Combine(Application.persistentDataPath, "PipelineV2_Outputs");
            Directory.CreateDirectory(outDirV2);

            string aiResultPathV2 = Path.Combine(outDirV2, "AI_RESULT.json");
            if (File.Exists(aiResultPathV2)) File.Delete(aiResultPathV2);

            string argsV2 = $"--mode all --baseline \"{baselinePath}\" --player \"{playerPath}\" " +
                            $"--output-dir \"{outDirV2}\" --cv-folds 5";
            Task<ProcessResult> taskV2 = Task.Run(() => RunProcess(exeV2, argsV2, outDirV2, pipelineTimeoutSeconds));
            while (!taskV2.IsCompleted) yield return null;
            v2Ok = HandleResult(taskV2.Result, 2, aiResultPathV2);

            // 3. Pipeline V3 (argparse flags are REQUIRED; positional args are rejected).
            //    Outputs go to a writable folder, not the process's current directory.
            string exeV3 = Path.Combine(Application.streamingAssetsPath, pipelineV3Exe);
            string outDirV3 = Path.Combine(Application.persistentDataPath, "PipelineV3_Outputs");
            Directory.CreateDirectory(outDirV3);

            // Remove any stale result so an old file is never mistaken for a fresh one
            string aiResultPath = Path.Combine(outDirV3, "AI_RESULT.json");
            if (File.Exists(aiResultPath)) File.Delete(aiResultPath);

            string argsV3 = $"--mode all --baseline \"{baselinePath}\" --player \"{playerPath}\" " +
                            $"--output-dir \"{outDirV3}\" --cv-folds 5";
            Task<ProcessResult> taskV3 = Task.Run(() => RunProcess(exeV3, argsV3, outDirV3, pipelineTimeoutSeconds));
            while (!taskV3.IsCompleted) yield return null;
            v3Ok = HandleResult(taskV3.Result, 3, aiResultPath);
        }

        // 4. Default the graphics mode to the newest preset that actually succeeded
        int modeToUse = v3Ok ? 3 : (v2Ok ? 2 : -1);
        if (modeToUse != -1)
        {
            PlayerPrefs.SetInt("Background_GraphicsMode", modeToUse);
            PlayerPrefs.SetInt("Obstacle_GraphicsMode", modeToUse);
            PlayerPrefs.SetInt("Platform_GraphicsMode", modeToUse);
            PlayerPrefs.SetInt("Player_GraphicsMode", modeToUse);
        }
        PlayerPrefs.Save();

        Debug.Log($"AI pipelines finished. V2 ok: {v2Ok}, V3 ok: {v3Ok}. Graphics mode set to: {modeToUse}");

        // 5. Continue to the Main Menu (50% -> 100%)
        if (S18_LoadingManager.Instance != null)
            S18_LoadingManager.Instance.LoadScenesFromProgress(0.5f, "GYM_MainMenu");
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene("GYM_MainMenu");
    }

    // ---------------------------------------------------------------------
    // Result handling
    // ---------------------------------------------------------------------
    private bool HandleResult(ProcessResult r, int version, string aiResultFilePath)
    {
        if (!r.launched)
        {
            Debug.LogError($"Pipeline V{version} could not run: {r.launchError}");
            return false;
        }
        if (r.timedOut)
        {
            Debug.LogError($"Pipeline V{version} timed out and was killed.");
            return false;
        }
        if (!string.IsNullOrEmpty(r.stdout)) Debug.Log($"Pipeline V{version} Output:\n" + r.stdout);
        if (!string.IsNullOrEmpty(r.stderr))
        {
            // Python warnings also land in stderr, so only treat it as an error when the exit code is non-zero
            if (r.exitCode != 0) Debug.LogError($"Pipeline V{version} Error (exit {r.exitCode}):\n" + r.stderr);
            else Debug.LogWarning($"Pipeline V{version} stderr:\n" + r.stderr);
        }
        if (r.exitCode != 0) return false;

        return ParseAndApply(r.stdout, aiResultFilePath, version);
    }

    // ---------------------------------------------------------------------
    // Process runner (thread-safe: no Unity API calls inside)
    // ---------------------------------------------------------------------
    private static ProcessResult RunProcess(string exePath, string arguments, string workingDir, int timeoutSeconds)
    {
        var result = new ProcessResult();

        if (!File.Exists(exePath))
        {
            result.launchError = "Executable not found at: " + exePath;
            return result;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };
        if (!string.IsNullOrEmpty(workingDir)) startInfo.WorkingDirectory = workingDir;

        // Pipeline prints characters like an em dash; force UTF-8 so a non-UTF-8 Windows
        // locale can't raise UnicodeEncodeError inside the frozen exe.
        startInfo.EnvironmentVariables["PYTHONIOENCODING"] = "utf-8";
        startInfo.EnvironmentVariables["PYTHONUTF8"] = "1";

        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        try
        {
            using (Process process = new Process { StartInfo = startInfo })
            {
                // Async reads avoid the stdout/stderr pipe deadlock
                process.OutputDataReceived += (s, e) => { if (e.Data != null) lock (stdout) stdout.AppendLine(e.Data); };
                process.ErrorDataReceived += (s, e) => { if (e.Data != null) lock (stderr) stderr.AppendLine(e.Data); };

                process.Start();
                result.launched = true;
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                if (!process.WaitForExit(timeoutSeconds * 1000))
                {
                    result.timedOut = true;
                    try { process.Kill(); } catch { }
                }
                process.WaitForExit(); // flush async handlers
                result.exitCode = result.timedOut ? -1 : process.ExitCode;
            }
        }
        catch (Exception e)
        {
            result.launchError = e.Message;
        }

        lock (stdout) result.stdout = stdout.ToString();
        lock (stderr) result.stderr = stderr.ToString();
        return result;
    }

    // ---------------------------------------------------------------------
    // V3 parsing: AI_RESULT.json file first, multi-line stdout block as fallback
    // ---------------------------------------------------------------------
    private bool ParseAndApply(string stdout, string aiResultFilePath, int version)
    {
        try
        {
            string json = null;

            if (!string.IsNullOrEmpty(aiResultFilePath) && File.Exists(aiResultFilePath))
                json = File.ReadAllText(aiResultFilePath, Encoding.UTF8);

            // Pipeline_V3 prints "AI_RESULT: {" followed by an indented multi-line JSON block,
            // so a single-line parse would only see "{". Extract the full balanced block.
            if (string.IsNullOrEmpty(json))
                json = ExtractJsonBlock(stdout, "AI_RESULT:");

            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError($"Pipeline V{version} finished but no AI_RESULT.json / AI_RESULT block was found.");
                return false;
            }

            AIResultV3 result = JsonUtility.FromJson<AIResultV3>(json);
            if (result == null || result.recommended_settings == null)
            {
                Debug.LogError($"Pipeline V{version} result is missing 'recommended_settings'.");
                return false;
            }

            V3Settings s = result.recommended_settings;

            // Keys: {Tag}_AIPresetV{version}_{Brightness|Contrast|Color}
            // For V2 this still includes the three legacy keys (Background brightness,
            // Obstacle contrast, Player color) plus the other nine settings.
            string suffix = "V" + version;
            SaveSettings("Background", suffix, s.background_brightness, s.background_contrast, s.background_color);
            SaveSettings("Obstacle", suffix, s.obstacle_brightness, s.obstacle_contrast, s.obstacle_color);
            SaveSettings("Platform", suffix, s.platform_brightness, s.platform_contrast, s.platform_color);
            SaveSettings("Player", suffix, s.player_brightness, s.player_contrast, s.player_color);

            Debug.Log($"AI Preset V.A. #{version} Saved! Predicted level: {result.predicted_accessibility_level}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error parsing AI results for V{version}: " + e.Message);
            return false;
        }
    }

    private void SaveSettings(string tag, string suffix, float brightness, float contrast, float colorDegrees)
    {
        float b = Mathf.Clamp(brightness, brightnessMin, brightnessMax);
        float c = Mathf.Clamp(contrast, contrastMin, contrastMax);

        // 0..360 (pipeline) -> colorMin..colorMax (Unity)
        float colorNormalized = Mathf.Clamp01(colorDegrees / Mathf.Max(0.0001f, v3ColorSourceMax));
        float col = Mathf.Lerp(colorMin, colorMax, colorNormalized);

        PlayerPrefs.SetFloat($"{tag}_AIPreset{suffix}_Brightness", b);
        PlayerPrefs.SetFloat($"{tag}_AIPreset{suffix}_Contrast", c);
        PlayerPrefs.SetFloat($"{tag}_AIPreset{suffix}_Color", col);
    }

    private static string ExtractJsonBlock(string text, string marker)
    {
        if (string.IsNullOrEmpty(text)) return null;
        int markerIdx = text.IndexOf(marker, StringComparison.Ordinal);
        if (markerIdx < 0) return null;
        int start = text.IndexOf('{', markerIdx);
        if (start < 0) return null;

        int depth = 0;
        for (int i = start; i < text.Length; i++)
        {
            if (text[i] == '{') depth++;
            else if (text[i] == '}')
            {
                depth--;
                if (depth == 0) return text.Substring(start, i - start + 1);
            }
        }
        return null;
    }
}