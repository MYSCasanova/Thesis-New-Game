using UnityEngine;
using UnityEngine.UI;

public class S9_VisualSettingsUI : MonoBehaviour
{
    public VisualSettingsSO settingsData;
    
    public enum Category { Background, Hazard, Platform, Player }
    
    [Header("Panel Category")]
    public Category panelCategory; // Dito mo pipiliin kung para saan ang panel na ito

    [Header("UI Sliders")]
    public Slider brightnessSlider;
    public Slider contrastSlider;
    public Slider colorSlider;
    
    [Header("Target Object")]
    public GameObject targetObject; // I-drop mo dito yung "--- PLAYER ---" o kahit anong target ng panel na ito

    private void Start()
    {
        // Kunin ang nakasave na settings mula sa ScriptableObject base sa category
        VisualSettingsSO.CategorySettings currentSettings = GetSettingsForCategory();
        
        // I-set ang sliders sa nakasave na value
        brightnessSlider.value = currentSettings.brightness;
        contrastSlider.value = currentSettings.contrast;
        colorSlider.value = currentSettings.color;

        // Mag-add ng listeners para mag-update kapag ginagalaw ang slider
        brightnessSlider.onValueChanged.AddListener(delegate { UpdateSettings(); });
        contrastSlider.onValueChanged.AddListener(delegate { UpdateSettings(); });
        colorSlider.onValueChanged.AddListener(delegate { UpdateSettings(); });

        // I-apply agad ang visual adjustments pagka-load
        ApplyAdjustments(currentSettings);
    }

    private void UpdateSettings()
    {
        VisualSettingsSO.CategorySettings currentSettings = GetSettingsForCategory();
        
        // I-update ang values sa ScriptableObject
        currentSettings.brightness = brightnessSlider.value;
        currentSettings.contrast = contrastSlider.value;
        currentSettings.color = colorSlider.value;

        // I-apply ang bagong values sa target object
        ApplyAdjustments(currentSettings);

        // I-save ang changes (para pwede ma-export sa CSV mamaya)
        settingsData.ApplyChanges();
    }

    private VisualSettingsSO.CategorySettings GetSettingsForCategory()
    {
        // I-return ang tamang data set base sa piniling category sa Inspector
        switch (panelCategory)
        {
            case Category.Hazard: return settingsData.hazardSettings;
            case Category.Platform: return settingsData.platformSettings;
            case Category.Player: return settingsData.playerSettings;
            case Category.Background: 
            default: return settingsData.backgroundSettings;
        }
    }

    private void ApplyAdjustments(VisualSettingsSO.CategorySettings activeSettings)
    {
        if (targetObject != null)
        {
            SpriteRenderer targetSprite = targetObject.GetComponent<SpriteRenderer>();
            
            if (targetSprite != null && targetSprite.material != null)
            {
                targetSprite.material.SetFloat("_Brightness", activeSettings.brightness);
                targetSprite.material.SetFloat("_Contrast", activeSettings.contrast);
                
                // Ipapasa ang color slider value sa Shader Graph
                targetSprite.material.SetFloat("_Saturation", activeSettings.color); 
            }
        }
    }
}