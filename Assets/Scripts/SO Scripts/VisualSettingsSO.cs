using UnityEngine;
using System;

[CreateAssetMenu(fileName = "VisualSettings", menuName = "Accessibility/Visual Settings")]
public class VisualSettingsSO : ScriptableObject
{
    [System.Serializable]
    public class CategorySettings
    {
        [Range(0f, 2f)] public float brightness = 1f;
        [Range(0f, 2f)] public float contrast = 1f;
        [Range(0f, 1f)] public float color = 0.5f; // Assuming a 0-1 slider for hue or color tint
    }

    // Explicit fields match your future CSV export format perfectly
    public CategorySettings backgroundSettings = new CategorySettings();
    public CategorySettings hazardSettings = new CategorySettings();
    public CategorySettings platformSettings = new CategorySettings();
    public CategorySettings playerSettings = new CategorySettings();

    // Event triggered whenever any setting changes
    public event Action OnSettingsChanged;

    public void ApplyChanges()
    {
        OnSettingsChanged?.Invoke();
    }
}