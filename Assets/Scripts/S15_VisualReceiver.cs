using UnityEngine;

public class S15_VisualReceiver : MonoBehaviour
{
    public VisualSettingsSO settingsData;
    
    public enum Category { Background, Hazard, Platform, Player }
    
    [Header("Object Category")]
    public Category myCategory;

    private SpriteRenderer mySprite;

    private void Awake()
    {
        mySprite = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        // 1. Subscribe to the event when this object is enabled or spawned in the scene
        settingsData.OnSettingsChanged += UpdateMyVisuals;
        
        // 2. Apply the correct visual adjustments immediately upon spawning
        UpdateMyVisuals(); 
    }

    private void OnDisable()
    {
        // Unsubscribe from the event when disabled or destroyed to prevent memory leaks and missing reference errors
        settingsData.OnSettingsChanged -= UpdateMyVisuals;
    }

    public void UpdateMyVisuals()
    {
        if (mySprite == null || mySprite.material == null) return;

        // Retrieve the correct settings data based on this object's specific category
        VisualSettingsSO.CategorySettings currentSettings = GetMySettings();

        // Apply the saved values directly to the shader material properties
        mySprite.material.SetFloat("_Brightness", currentSettings.brightness);
        mySprite.material.SetFloat("_Contrast", currentSettings.contrast);
        mySprite.material.SetFloat("_Saturation", currentSettings.color);
    }

    private VisualSettingsSO.CategorySettings GetMySettings()
    {
        // Return the corresponding settings from the ScriptableObject
        switch (myCategory)
        {
            case Category.Hazard: return settingsData.hazardSettings;
            case Category.Platform: return settingsData.platformSettings;
            case Category.Player: return settingsData.playerSettings;
            case Category.Background: 
            default: return settingsData.backgroundSettings;
        }
    }
}