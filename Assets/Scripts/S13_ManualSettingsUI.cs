using UnityEngine;
using UnityEngine.UI;

public class S13_ManualSettingsUI : MonoBehaviour
{
    public VisualSettingsSO settingsData;

    [Header("UI Generation")]
    public Transform graphicsContentParent; // Dito i-d-drop yung "Content" object ng ScrollView
    public GameObject categoryPrefab;       // Dito i-d-drop yung nagawa nating Prefab

    private void Start()
    {
        // Linisin muna kung may laman ang Content panel (optional safeguard)
        foreach (Transform child in graphicsContentParent) {
            Destroy(child.gameObject);
        }

        // I-generate ang mga UI panels via script!
        GenerateCategory("Adjust Background Visual", "Background", settingsData.backgroundSettings);
        GenerateCategory("Adjust Hazard Visual", "Hazard", settingsData.hazardSettings);
        GenerateCategory("Adjust Platform Visual", "Platform", settingsData.platformSettings);
        GenerateCategory("Adjust Player Visual", "Player", settingsData.playerSettings);
    }

    private void GenerateCategory(string title, string tagToFind, VisualSettingsSO.CategorySettings currentSettings)
    {
        // 1. Spawn a new Prefab inside the Content Parent
        GameObject newCategory = Instantiate(categoryPrefab, graphicsContentParent);
        S14_CategoryUI uiElements = newCategory.GetComponent<S14_CategoryUI>();

        // 2. Change the title text
        if (uiElements.headerText != null) uiElements.headerText.text = title;

        // 3. Load the saved values into the sliders
        uiElements.brightnessSlider.value = currentSettings.brightness;
        uiElements.contrastSlider.value = currentSettings.contrast;
        uiElements.colorSlider.value = currentSettings.color;

        // 4. Add listeners to update settings when sliders are moved (REMOVED tagToFind HERE)
        uiElements.brightnessSlider.onValueChanged.AddListener(val => UpdateVisuals(uiElements, currentSettings));
        uiElements.contrastSlider.onValueChanged.AddListener(val => UpdateVisuals(uiElements, currentSettings));
        uiElements.colorSlider.onValueChanged.AddListener(val => UpdateVisuals(uiElements, currentSettings));
    }

    private void UpdateVisuals(S14_CategoryUI ui, VisualSettingsSO.CategorySettings currentSettings)
    {
        // Save the new slider values into the respective category in the ScriptableObject
        currentSettings.brightness = ui.brightnessSlider.value;
        currentSettings.contrast = ui.contrastSlider.value;
        currentSettings.color = ui.colorSlider.value;

        // Calling ApplyChanges() on the ScriptableObject will trigger the OnSettingsChanged event.
        // This automatically notifies all active S15_VisualReceiver scripts to update their materials instantly.
        settingsData.ApplyChanges(); 
    }

    private void ApplyVisualsToTag(string tagToFind, VisualSettingsSO.CategorySettings activeSettings)
    {
        GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag(tagToFind);
        foreach (GameObject obj in taggedObjects)
        {
            SpriteRenderer targetSprite = obj.GetComponent<SpriteRenderer>();
            if (targetSprite != null && targetSprite.material != null)
            {
                targetSprite.material.SetFloat("_Brightness", activeSettings.brightness);
                targetSprite.material.SetFloat("_Contrast", activeSettings.contrast);
                targetSprite.material.SetFloat("_Saturation", activeSettings.color);
            }
        }
    }
}