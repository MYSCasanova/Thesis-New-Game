using UnityEngine;

public class S15_VisualReceiver : MonoBehaviour
{
    public VisualSettingsSO settingsData;
    
    public enum Category { Background, Obstacle, Platform, Player }
    
    [Header("Object Category")]
    public Category myCategory;

    private SpriteRenderer mySprite;

    private void Awake()
    {
        mySprite = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        // 0. Ensure the ScriptableObject has the correct values loaded BEFORE applying visuals
        LoadMySettings();

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

    private void LoadMySettings()
    {
        // Determine the tag string to match the PlayerPrefs keys saved by the UI
        string tagToFind = myCategory.ToString();
        VisualSettingsSO.CategorySettings currentSettings = GetMySettings();

        // Check what mode is saved (0 = Default, 1 = Custom)
        int savedMode = PlayerPrefs.GetInt(tagToFind + "_GraphicsMode", 0);

        if (savedMode == 0) // Default Mode
        {
            switch (tagToFind)
            {
                case "Background":
                    currentSettings.brightness = 2f;
                    currentSettings.contrast = 0.8960935f;
                    currentSettings.color = 0f;
                    break;
                case "Obstacle":
                    currentSettings.brightness = 1.290718f;
                    currentSettings.contrast = 0.8276349f;
                    currentSettings.color = 0f;
                    break;
                case "Platform":
                    currentSettings.brightness = 0.5678623f;
                    currentSettings.contrast = 0.8216149f;
                    currentSettings.color = 0f;
                    break;
                case "Player":
                    currentSettings.brightness = 0.9364931f;
                    currentSettings.contrast = 0.9023179f;
                    currentSettings.color = 0f;
                    break;
            }
        }
        else if (savedMode == 1) // Custom Mode
        {
            currentSettings.brightness = PlayerPrefs.GetFloat(tagToFind + "_Brightness", 1f);
            currentSettings.contrast = PlayerPrefs.GetFloat(tagToFind + "_Contrast", 1f);
            currentSettings.color = PlayerPrefs.GetFloat(tagToFind + "_Color", 1f);
        }
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
            case Category.Obstacle: return settingsData.obstacleSettings;
            case Category.Platform: return settingsData.platformSettings;
            case Category.Player: return settingsData.playerSettings;
            case Category.Background: 
            default: return settingsData.backgroundSettings;
        }
    }
}