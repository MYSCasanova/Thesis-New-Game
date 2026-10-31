using UnityEngine;
using System.Diagnostics;
using System.IO;

public class PythonIntegration : MonoBehaviour
{
    [Header("Script Settings")]
    public string pipelineV2Exe = "Pipeline_V2.exe";
    public string pipelineV3Exe = "Pipeline_V3.exe";

    [System.Serializable]
    public class AIPrediction
    {
        public float background_brightness;
        public float obstacle_contrast;
        public float player_color;
    }

    public void CallPythonScript()
    {
        // Start the sequence as a Coroutine
        StartCoroutine(RunPipelinesAndLoad());
    }

    private System.Collections.IEnumerator RunPipelinesAndLoad()
    {
        // 1. Force the loading screen to appear immediately
        if (S18_LoadingManager.Instance != null && S18_LoadingManager.Instance.loadingPanel != null)
        {
            S18_LoadingManager.Instance.loadingPanel.SetActive(true);
            
            // THE FIX: Set the blocks to half-full (50%) before the AI freezes the game. 
            // This prevents the "starts full, then empties" visual glitch.
            S18_LoadingManager.Instance.UpdateProgressBlocks(0.5f);
        }

        // 2. Wait for 1 frame so Unity physically renders the 50% loading screen before freezing
        yield return null; 

        string baselinePath = Path.Combine(Application.dataPath, "VisualAccessibility_Baseline.csv");
        string playerPath = Path.Combine(Application.dataPath, "VisualAccessibility_Player.csv");

        // 3. Run Pipeline V2 (Game will pause here, but the loading screen is safely displayed)
        string exeV2Path = Path.Combine(Application.streamingAssetsPath, pipelineV2Exe);
        RunPipeline(exeV2Path, baselinePath, playerPath, 2);

        // 4. Run Pipeline V3
        string exeV3Path = Path.Combine(Application.streamingAssetsPath, pipelineV3Exe);
        RunPipeline(exeV3Path, baselinePath, playerPath, 3);
        
        // 5. Set graphics mode to default to V.A. #3 (Mode 3) after saving
        PlayerPrefs.SetInt("Background_GraphicsMode", 3);
        PlayerPrefs.SetInt("Obstacle_GraphicsMode", 3);
        PlayerPrefs.SetInt("Platform_GraphicsMode", 3);
        PlayerPrefs.SetInt("Player_GraphicsMode", 3);
        PlayerPrefs.Save();
        
        // ... (Previous AI execution code remains exactly the same)
        
        UnityEngine.Debug.Log("Both AI Pipelines have finished running and presets are saved.");

        // 6. NOW transition to the Main Menu using the new continuous loading method
        if (S18_LoadingManager.Instance != null)
        {
            // Tells the loading screen to fill from 50% to 100%
            S18_LoadingManager.Instance.LoadScenesFromProgress(0.5f, "GYM_MainMenu");
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("GYM_MainMenu");
        }
    }
    private void RunPipeline(string exePath, string baseline, string player, int version)
    {
        if (!File.Exists(exePath))
        {
            UnityEngine.Debug.LogError($"Pipeline V{version} Executable not found at: {exePath}");
            return;
        }

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = $"\"{baseline}\" \"{player}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        try
        {
            using (Process process = Process.Start(startInfo))
            {
                string result = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (!string.IsNullOrEmpty(result))
                {
                    UnityEngine.Debug.Log($"Pipeline V{version} Output:\n" + result);
                    ParseAndApplyResults(result, version);
                }
                
                if (!string.IsNullOrEmpty(error))
                {
                    UnityEngine.Debug.LogError($"Pipeline V{version} Error:\n" + error);
                }
            }
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError($"Failed to launch Pipeline V{version}: " + e.Message);
        }
    }

    private void ParseAndApplyResults(string pythonOutput, int version)
    {
        try 
        {
            string jsonString = "";
            string[] lines = pythonOutput.Split('\n');
            string targetPrefix = $"AI_RESULT_V{version}:";
            string fallbackPrefix = "AI_RESULT:";
            
            foreach (string line in lines)
            {
                if (line.TrimStart().StartsWith(targetPrefix))
                {
                    jsonString = line.Substring(line.IndexOf(targetPrefix) + targetPrefix.Length).Trim();
                    break;
                }
                else if (line.TrimStart().StartsWith(fallbackPrefix))
                {
                    jsonString = line.Substring(line.IndexOf(fallbackPrefix) + fallbackPrefix.Length).Trim();
                    break;
                }
            }

            if (!string.IsNullOrEmpty(jsonString))
            {
                AIPrediction prediction = JsonUtility.FromJson<AIPrediction>(jsonString);
                string suffix = (version == 2) ? "V2" : "V3";

                // Save to distinct PlayerPrefs keys based on version
                PlayerPrefs.SetFloat($"Background_AIPreset{suffix}_Brightness", prediction.background_brightness);
                PlayerPrefs.SetFloat($"Obstacle_AIPreset{suffix}_Contrast", prediction.obstacle_contrast);
                PlayerPrefs.SetFloat($"Player_AIPreset{suffix}_Color", prediction.player_color);
                
                UnityEngine.Debug.Log($"AI Preset V.A. #{version} Saved!");
            }
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError($"Error parsing AI results for V{version}: " + e.Message);
        }
    }
}