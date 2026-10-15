using UnityEngine;
using UnityEngine.UI;

public class S9_VisualSettingsUI : MonoBehaviour
{
    public VisualSettingsSO settingsData;
    
    public enum Category { Background, Obstacle, Platform, Player }
    
    [Header("Panel Category")]
    [Tooltip("Select which category this panel is for.")]
    public Category panelCategory; //[cite: 9]

    [Header("UI Sliders")]
    public Slider brightnessSlider;
    public Slider contrastSlider;
    public Slider colorSlider;

    [Header("Default Settings")]
    [Tooltip("Set the default values for the sliders here.")]
    public float defaultBrightness = 1f;
    public float defaultContrast = 1f;
    public float defaultColor = 1f;
    
    [Header("Target Object")]
    [Tooltip("Drop the target object (e.g., player, platform) for this panel here.")]
    public GameObject targetObject; //[cite: 9]

    private void Start()
    {
        // Get the saved settings from the ScriptableObject based on the category[cite: 9]
        VisualSettingsSO.CategorySettings currentSettings = GetSettingsForCategory(); //[cite: 9]
        
        // Set the sliders to the saved values[cite: 9]
        brightnessSlider.value = currentSettings.brightness; //[cite: 9]
        contrastSlider.value = currentSettings.contrast; //[cite: 9]
        colorSlider.value = currentSettings.color; //[cite: 9]

        // Add listeners to update settings when sliders are moved[cite: 9]
        brightnessSlider.onValueChanged.AddListener(delegate { UpdateSettings(); }); //[cite: 9]
        contrastSlider.onValueChanged.AddListener(delegate { UpdateSettings(); }); //[cite: 9]
        colorSlider.onValueChanged.AddListener(delegate { UpdateSettings(); }); //[cite: 9]

        // Apply the visual adjustments immediately upon loading[cite: 9]
        ApplyAdjustments(currentSettings); //[cite: 9]
    }

    private void UpdateSettings()
    {
        VisualSettingsSO.CategorySettings currentSettings = GetSettingsForCategory(); //[cite: 9]
        
        // Update the values in the ScriptableObject[cite: 9]
        currentSettings.brightness = brightnessSlider.value; //[cite: 9]
        currentSettings.contrast = contrastSlider.value; //[cite: 9]
        currentSettings.color = colorSlider.value; //[cite: 9]

        // Apply the new values to the target object[cite: 9]
        ApplyAdjustments(currentSettings); //[cite: 9]

        // Save the changes (so they can be exported to CSV later)[cite: 9]
        settingsData.ApplyChanges(); //[cite: 9]
    }

    private VisualSettingsSO.CategorySettings GetSettingsForCategory()
    {
        // Return the correct data set based on the selected category in the Inspector[cite: 9]
        switch (panelCategory) //[cite: 9]
        {
            case Category.Obstacle: return settingsData.obstacleSettings; //[cite: 9]
            case Category.Platform: return settingsData.platformSettings; //[cite: 9]
            case Category.Player: return settingsData.playerSettings; //[cite: 9]
            case Category.Background: //[cite: 9]
            default: return settingsData.backgroundSettings; //[cite: 9]
        }
    }

    private void ApplyAdjustments(VisualSettingsSO.CategorySettings activeSettings)
    {
        if (targetObject != null) //[cite: 9]
        {
            SpriteRenderer targetSprite = targetObject.GetComponent<SpriteRenderer>(); //[cite: 9]
            
            if (targetSprite != null && targetSprite.material != null) //[cite: 9]
            {
                targetSprite.material.SetFloat("_Brightness", activeSettings.brightness); //[cite: 9]
                targetSprite.material.SetFloat("_Contrast", activeSettings.contrast); //[cite: 9]
                
                // Pass the color slider value to the Shader Graph (Saturation)[cite: 9]
                targetSprite.material.SetFloat("_Saturation", activeSettings.color); //[cite: 9]
            }
        }
    }

    // --- NEW FUNCTION ---
    public void ResetToDefault()
    {
        // Changing the slider values via code automatically triggers the onValueChanged listeners.
        // This will seamlessly run UpdateSettings() and apply the changes.
        brightnessSlider.value = defaultBrightness;
        contrastSlider.value = defaultContrast;
        colorSlider.value = defaultColor;
    }
}