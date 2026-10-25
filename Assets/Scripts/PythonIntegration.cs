using UnityEngine;
using System.Diagnostics;
using System.IO;

public class PythonIntegration : MonoBehaviour
{
    [Header("Script Paths")]
    [Tooltip("Path to the python executable (e.g., C:/Python39/python.exe or just 'python' if in PATH)")]
    public string pythonExecutablePath = "python";
    
    [Tooltip("Name of the python script to run")]
    public string pythonScriptName = "Pipeline_V2.py";

    // Class to map the JSON data coming from Python
    [System.Serializable]
    public class AIPrediction
    {
        public float background_brightness;
        public float obstacle_contrast;
        public float player_color;
        // Add your other 9 variables here if needed
    }

    public void CallPythonScript()
    {
        string scriptFullPath = Application.dataPath + "/Scripts/PythonScripts/" + pythonScriptName;
        string csvFullPath = Application.dataPath + "/SettingsExport.csv"; 

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = pythonExecutablePath,    
            Arguments = $"\"{scriptFullPath}\" \"{csvFullPath}\"", 
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
                    UnityEngine.Debug.Log("Python Output:\n" + result);
                    ParseAndApplyResults(result);
                }
                
                if (!string.IsNullOrEmpty(error))
                {
                    UnityEngine.Debug.LogError("Python Error:\n" + error);
                }
            }
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError("Failed to launch Python script: " + e.Message);
        }
    }

    private void ParseAndApplyResults(string pythonOutput)
    {
        try 
        {
            string jsonString = "";
            string[] lines = pythonOutput.Split('\n');
            
            // Find the specific line containing the final prediction
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
                // Deserialize the JSON string into our C# class
                AIPrediction prediction = JsonUtility.FromJson<AIPrediction>(jsonString);

                // Convert percentages (0-100) back to actual min/max scales
                float bgBrightnessRaw = (prediction.background_brightness / 100f) * 2f;
                float obsContrastRaw = (prediction.obstacle_contrast / 100f) * 2f;
                float playerColorRaw = (prediction.player_color / 100f) * 360f;

                // Save to PlayerPrefs
                PlayerPrefs.SetFloat("Background_AIPreset_Brightness", bgBrightnessRaw);
                PlayerPrefs.SetFloat("Obstacle_AIPreset_Contrast", obsContrastRaw);
                PlayerPrefs.SetFloat("Player_AIPreset_Color", playerColorRaw);
                
                // Automatically switch the UI to the AI Preset Mode (Mode 2)
                PlayerPrefs.SetInt("Background_GraphicsMode", 2);
                PlayerPrefs.SetInt("Obstacle_GraphicsMode", 2);
                PlayerPrefs.SetInt("Platform_GraphicsMode", 2);
                PlayerPrefs.SetInt("Player_GraphicsMode", 2);
                
                PlayerPrefs.Save();
                UnityEngine.Debug.Log("AI Preset Saved and Applied!");
            }
            else 
            {
                UnityEngine.Debug.LogWarning("Could not find 'AI_RESULT:' string in the Python output.");
            }
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError("Error parsing Python results: " + e.Message);
        }
    }
}