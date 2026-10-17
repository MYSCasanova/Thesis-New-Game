using UnityEngine;
using UnityEngine.UI;
using System.IO;
using UnityEngine.SceneManagement; 

public class DataExporter : MonoBehaviour
{
    [Header("Settings Data")]
    public VisualSettingsSO settingsData; 

    [Header("UI Slider References")]
    [Tooltip("Drag any Color, Contrast, and Brightness slider from your UI panels here to get their exact max values.")]
    public Slider colorSlider;
    public Slider contrastSlider;
    public Slider brightnessSlider;

    public void ExportToCSV()
    {
        if (settingsData == null || colorSlider == null || contrastSlider == null || brightnessSlider == null)
        {
            Debug.LogError("Missing references in the Inspector! Ensure Settings Data and all three Sliders are assigned.");
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

        // --- DYNAMICALLY FIND THE GAMEPLAY DATA ---
        float completionTime = 0f;
        int errorCount = 0;

        // Force Unity to find the script even if the GameObject has been disabled 
        S17_GameplayData activeGameplayData = Object.FindFirstObjectByType<S17_GameplayData>(FindObjectsInactive.Include);

        if (activeGameplayData != null)
        {
            completionTime = activeGameplayData.completionTime;
            errorCount = activeGameplayData.errorCount;
            
            // Optional: Destroy it after grabbing the data so it doesn't pile up if you replay
            Destroy(activeGameplayData.gameObject);
        }
        else
        {
            Debug.LogWarning("Gameplay Data was not found in the scene! Saving 0 for time and errors.");
        }
        // ------------------------------------------

        float maxColor = colorSlider.maxValue;
        float maxContrast = contrastSlider.maxValue;
        float maxBrightness = brightnessSlider.maxValue;

        string dataRow = $"{Normalize(bg.color, maxColor)},{Normalize(bg.contrast, maxContrast)},{Normalize(bg.brightness, maxBrightness)}," +
                         $"{Normalize(ob.color, maxColor)},{Normalize(ob.contrast, maxContrast)},{Normalize(ob.brightness, maxBrightness)}," +
                         $"{Normalize(pt.color, maxColor)},{Normalize(pt.contrast, maxContrast)},{Normalize(pt.brightness, maxBrightness)}," +
                         $"{Normalize(pl.color, maxColor)},{Normalize(pl.contrast, maxContrast)},{Normalize(pl.brightness, maxBrightness)}," +
                         $"{completionTime.ToString("F2")},{errorCount}";

        string filePath = Application.dataPath + "/SettingsExport.csv";
        
        if (!File.Exists(filePath))
        {
            File.WriteAllText(filePath, header + "\n" + dataRow + "\n");
        }
        else
        {
            File.AppendAllText(filePath, dataRow + "\n");
        }

        Debug.Log("Exported Normalized Row: \n" + dataRow);
        Debug.Log("Going back to Main Menu");
        SceneManager.LoadScene("GYM_MainMenu", LoadSceneMode.Single);
    }

    private string Normalize(float rawValue, float maxValue)
    {
        float normalizedValue = Mathf.Clamp01(rawValue / maxValue);
        return normalizedValue.ToString("0.00");
    }
}