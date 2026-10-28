using UnityEngine;
using System.Diagnostics;
using System.IO;

public class PythonIntegration : MonoBehaviour
{
    [Header("Script Settings")]
    [Tooltip("Name of the compiled standalone executable in StreamingAssets")]
    public string executableName = "Pipeline_V3.exe";

    [System.Serializable]
    public class AIPrediction
    {
        public float background_brightness;
        public float obstacle_contrast;
        public float player_color;
    }

    public void CallPythonScript()
    {
        string exePath = Path.Combine(Application.streamingAssetsPath, executableName);
        string baselinePath = Path.Combine(Application.dataPath, "VisualAccessibility_Baseline.csv");
        string playerPath = Path.Combine(Application.dataPath, "VisualAccessibility_Player.csv");

        if (!File.Exists(exePath))
        {
            UnityEngine.Debug.LogError("Standalone AI Executable not found at: " + exePath);
            return;
        }

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = $"\"{baselinePath}\" \"{playerPath}\"",
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
                    UnityEngine.Debug.Log("Standalone AI Output:\n" + result);
                    ParseAndApplyResults(result);
                }
                
                if (!string.IsNullOrEmpty(error))
                {
                    UnityEngine.Debug.LogError("Standalone AI Error:\n" + error);
                }
            }
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError("Failed to launch AI Executable: " + e.Message);
        }
    }

    private void ParseAndApplyResults(string pythonOutput)
    {
        try 
        {
            string jsonString = "";
            string[] lines = pythonOutput.Split('\n');
            
            foreach (string line in lines)
            {
                if (line.TrimStart().StartsWith("AI_RESULT:"))
                {
                    jsonString = line.Substring(line.IndexOf("AI_RESULT:") + 10).Trim();
                    break;
                }
            }

            if (!string.IsNullOrEmpty(jsonString))
            {
                AIPrediction prediction = JsonUtility.FromJson<AIPrediction>(jsonString);

                PlayerPrefs.SetFloat("Background_AIPreset_Brightness", prediction.background_brightness);
                PlayerPrefs.SetFloat("Obstacle_AIPreset_Contrast", prediction.obstacle_contrast);
                PlayerPrefs.SetFloat("Player_AIPreset_Color", prediction.player_color);
                
                PlayerPrefs.SetInt("Background_GraphicsMode", 2);
                PlayerPrefs.SetInt("Obstacle_GraphicsMode", 2);
                PlayerPrefs.SetInt("Platform_GraphicsMode", 2);
                PlayerPrefs.SetInt("Player_GraphicsMode", 2);
                
                PlayerPrefs.Save();
                UnityEngine.Debug.Log("AI Preset Saved and Applied!");
            }
            else 
            {
                UnityEngine.Debug.LogWarning("Could not find 'AI_RESULT:' string in output.");
            }
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError("Error parsing AI results: " + e.Message);
        }
    }
}
