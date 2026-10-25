using UnityEngine;
using System.Diagnostics;

public class PythonIntegration : MonoBehaviour
{
    [Header("Script Paths")]
    [Tooltip("Path to the python executable (e.g., C:/Python39/python.exe or just 'python' if in PATH)")]
    public string pythonExecutablePath = "python";
    
    [Tooltip("Name of the python script to run")]
    public string pythonScriptName = "Pipeline_V2.py";

    public void CallPythonScript()
    {
        // Assuming the python script is placed inside the Unity project's Assets folder
        string scriptFullPath = Application.dataPath + "/Scripts/PythonScripts/" + pythonScriptName;

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = pythonExecutablePath,
            Arguments = $"\"{scriptFullPath}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        try
        {
            using (Process process = Process.Start(startInfo))
            {
                // Read the output
                string result = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                
                process.WaitForExit();

                if (!string.IsNullOrEmpty(result))
                {
                    UnityEngine.Debug.Log("Python Output:\n" + result);
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
}