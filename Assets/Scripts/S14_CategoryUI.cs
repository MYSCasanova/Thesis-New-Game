using UnityEngine;
using UnityEngine.UI;
using TMPro; // Make sure using TMP for your text

public class S14_CategoryUI : MonoBehaviour
{
    [Header("Toggle Elements")]
    public Button headerButton;
    public TextMeshProUGUI headerText;
    public GameObject slidersPanel;

    [Header("Sliders")]
    public Slider brightnessSlider;
    public Slider contrastSlider;
    public Slider colorSlider;

    private void Start()
    {
        // Kapag pinindot ang header, magto-toggle ang panel
        headerButton.onClick.AddListener(TogglePanel);
        
        // I-hide agad pagka-start para malinis
        slidersPanel.SetActive(false); 
    }

    private void TogglePanel()
    {
        slidersPanel.SetActive(!slidersPanel.activeSelf);
        
        // Ito yung nagfi-fix ng spacing sa layout group kapag nag-expand
        LayoutRebuilder.ForceRebuildLayoutImmediate(transform.parent.GetComponent<RectTransform>());
    }
}