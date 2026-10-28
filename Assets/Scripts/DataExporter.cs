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
        SceneManager.LoadScene("GYM_MainMenu", LoadSceneMode.Single);
    }

    private string Normalize(float rawValue, float maxValue)
    {
        float normalizedValue = Mathf.Clamp01(rawValue / maxValue);
        return normalizedValue.ToString("0.00");
    }
}
