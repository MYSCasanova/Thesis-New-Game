using UnityEngine;
using UnityEngine.UI; // Or SpriteRenderer if adjusting in-game sprites

public class S10_PlayerVisualReceiver : MonoBehaviour
{
    public VisualSettingsSO settingsData;
    private Image previewImage; // Change to SpriteRenderer for actual gameplay objects

    private void Awake()
    {
        previewImage = GetComponent<Image>();
    }

    private void OnEnable()
    {
        settingsData.OnSettingsChanged += UpdateVisuals;
        UpdateVisuals(); // Sync on startup
    }

    private void OnDisable()
    {
        settingsData.OnSettingsChanged -= UpdateVisuals;
    }

    private void UpdateVisuals()
    {
        // Example logic: You will likely pass these values to a custom shader/material
        float b = settingsData.playerSettings.brightness;
        // previewImage.material.SetFloat("_Brightness", b); 
    }
}