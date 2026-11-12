using UnityEngine;
using UnityEngine.UI;
using System.IO;
using UnityEngine.SceneManagement; 

public class DataExporter : MonoBehaviour
{
    [Header("Settings Data")]
    public VisualSettingsSO settingsData; 

    [Header("UI Slider References")]
    public Slider colorSlider;
    public Slider contrastSlider;
    public Slider brightnessSlider;

    public void ExportToCSV()
    {
        if (settingsData == null || colorSlider == null || contrastSlider == null || brightnessSlider == null)
        {
            Debug.LogError("Missing references in the Inspector!");
            return;
        }

        string header = "background_color,background_contrast,background_brightness," +
                        "obstacle_color,obstacle_contrast,obstacle_brightness," +
                        "platform_color,platform_contrast,platform_brightness," +
                        "player_color,player_contrast,player_brightness," +
                        "completion_time,error_count";

        var bg = settingsData.backgroundSettings;
        var ob = settingsData.obstacleSettings; 
        var pt = settingsData.platformSettings;
        var pl = settingsData.playerSettings;

        float completionTime = 0f;
        int errorCount = 0;

        S17_GameplayData activeGameplayData = Object.FindFirstObjectByType<S17_GameplayData>(FindObjectsInactive.Include);
        if (activeGameplayData != null)
        {
            completionTime = activeGameplayData.completionTime;
            errorCount = activeGameplayData.errorCount;
            Destroy(activeGameplayData.gameObject);
        }

        float maxColor = colorSlider.maxValue;
        float maxContrast = contrastSlider.maxValue;
        float maxBrightness = brightnessSlider.maxValue;

        string dataRow = $"{Normalize(bg.color, maxColor)},{Normalize(bg.contrast, maxContrast)},{Normalize(bg.brightness, maxBrightness)}," +
                         $"{Normalize(ob.color, maxColor)},{Normalize(ob.contrast, maxContrast)},{Normalize(ob.brightness, maxBrightness)}," +
                         $"{Normalize(pt.color, maxColor)},{Normalize(pt.contrast, maxContrast)},{Normalize(pt.brightness, maxBrightness)}," +
                         $"{Normalize(pl.color, maxColor)},{Normalize(pl.contrast, maxContrast)},{Normalize(pl.brightness, maxBrightness)}," +
                         $"{completionTime.ToString("F2")},{errorCount}";

        string filePath = Path.Combine(Application.dataPath, "VisualAccessibility_Player.csv");
        
        if (!File.Exists(filePath))
        {
            File.WriteAllText(filePath, header + "\n" + dataRow + "\n");
        }
        else
        {
            File.AppendAllText(filePath, dataRow + "\n");
        }

        Debug.Log("Appended Player Record to VisualAccessibility_Player.csv: \n" + dataRow);

        PlayerPrefs.SetInt("v1Completed", 1);
        PlayerPrefs.Save();
        Debug.Log("Version 1 and Visual Assessment completed!");

        // SceneManager.LoadScene("GYM_MainMenu", LoadSceneMode.Single);
    }

    public void ExportGameplayToCSV()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        string fileName;

        if (sceneName == "GYM_V2Game")
        {
            fileName = "V2_Player.csv";
        }
        else if (sceneName == "GYM_V3Game")
        {
            fileName = "V3_Player.csv";
        }
        else
        {
            Debug.LogError("This is not a V2 or V3 game scene!");
            return;
        }

        if (settingsData == null)
        {
            Debug.LogError("Global VisualSettingsSO is not assigned!");
            return;
        }

        S17_GameplayData gameplayData = Object.FindFirstObjectByType<S17_GameplayData>(FindObjectsInactive.Include);

        if (gameplayData == null)
        {
            Debug.LogError("Gameplay data not found!");
            return;
        }

        var bg = settingsData.backgroundSettings;
        var ob = settingsData.obstacleSettings;
        var pt = settingsData.platformSettings;
        var pl = settingsData.playerSettings;

        string header = "background_color,background_contrast,background_brightness," +
                        "obstacle_color,obstacle_contrast,obstacle_brightness," +
                        "platform_color,platform_contrast,platform_brightness," +
                        "player_color,player_contrast,player_brightness," +
                        "completion_time,error_count";

        string dataRow = $"{bg.color:F2},{bg.contrast:F2},{bg.brightness:F2}," +
                        $"{ob.color:F2},{ob.contrast:F2},{ob.brightness:F2}," +
                        $"{pt.color:F2},{pt.contrast:F2},{pt.brightness:F2}," +
                        $"{pl.color:F2},{pl.contrast:F2},{pl.brightness:F2}," +
                        $"{gameplayData.completionTime:F2},{gameplayData.errorCount}";

        string filePath = Path.Combine(Application.dataPath, fileName); 

        if (!File.Exists(filePath))
        {
            File.WriteAllText(filePath, header + "\n" + dataRow + "\n");
        }
        else
        {
            File.AppendAllText(filePath, dataRow + "\n");
        }

        Debug.Log("Gameplay data exported to " + fileName);

        FindFirstObjectByType<SceneTransition>().MainMenuFromWin();
    }

    private string Normalize(float rawValue, float maxValue)
    {
        float normalizedValue = Mathf.Clamp01(rawValue / maxValue);
        return normalizedValue.ToString("0.00");
    }
}
