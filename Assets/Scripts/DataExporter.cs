using UnityEngine;
using UnityEngine.UI; // Importante ito para ma-basa ang Slider component
using System.IO;

public class DataExporter : MonoBehaviour
{
    [Header("Settings Data")]
    public VisualSettingsSO settingsData; 

    [Header("UI Slider References")]
    [Tooltip("I-drag dito ang kahit anong isang Color, Contrast, at Brightness slider mula sa UI panels mo para makuha ang exact max values nila.")]
    public Slider colorSlider;
    public Slider contrastSlider;
    public Slider brightnessSlider;

    public void ExportToCSV()
    {
        // Checker para iwas error kung may nakalimutan i-assign sa Inspector
        if (settingsData == null || colorSlider == null || contrastSlider == null || brightnessSlider == null)
        {
            Debug.LogError("May kulang sa Inspector! Siguraduhing naka-assign ang Settings Data at ang tatlong Sliders.");
            return;
        }

        // Headers
        string header = "background_color,background_contrast,background_brightness," +
                        "hazard_color,hazard_contrast,hazard_brightness," +
                        "platform_color,platform_contrast,platform_brightness," +
                        "player_color,player_contrast,player_brightness," +
                        "reaction_time,error_count";

        // Kunin yung raw values mula sa nakasave na settings
        var bg = settingsData.backgroundSettings;
        var hz = settingsData.hazardSettings;
        var pt = settingsData.platformSettings;
        var pl = settingsData.playerSettings;

        // Temporary 0 values muna
        float reactionTime = 0f;
        int errorCount = 0;

        // Kunin ang Max Values dynamically galing mismo sa Inspector properties ng sliders mo
        float maxColor = colorSlider.maxValue;
        float maxContrast = contrastSlider.maxValue;
        float maxBrightness = brightnessSlider.maxValue;

        // I-convert sa decimal percentage (Value / MaxValue) na hanggang dalawang decimal places
        string dataRow = $"{Normalize(bg.color, maxColor)},{Normalize(bg.contrast, maxContrast)},{Normalize(bg.brightness, maxBrightness)}," +
                         $"{Normalize(hz.color, maxColor)},{Normalize(hz.contrast, maxContrast)},{Normalize(hz.brightness, maxBrightness)}," +
                         $"{Normalize(pt.color, maxColor)},{Normalize(pt.contrast, maxContrast)},{Normalize(pt.brightness, maxBrightness)}," +
                         $"{Normalize(pl.color, maxColor)},{Normalize(pl.contrast, maxContrast)},{Normalize(pl.brightness, maxBrightness)}," +
                         $"{reactionTime},{errorCount}";

        // I-save ang file
        string csvContent = header + "\n" + dataRow;
        string filePath = Application.dataPath + "/SettingsExport.csv";
        
        File.WriteAllText(filePath, csvContent);
        Debug.Log("Exported Normalized Row: \n" + dataRow);
    }

    // Helper function para mag-compute ng percentage at maging 0.00 hanggang 1.00 lang
    private string Normalize(float rawValue, float maxValue)
    {
        float normalizedValue = Mathf.Clamp01(rawValue / maxValue);
        return normalizedValue.ToString("0.00");
    }
}