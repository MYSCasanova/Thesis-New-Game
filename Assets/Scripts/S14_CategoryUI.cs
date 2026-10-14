using UnityEngine;
using UnityEngine.UI;
using TMPro; 

public class S14_CategoryUI : MonoBehaviour
{
    [Header("Toggle Elements")]
    public Button headerButton;
    public TextMeshProUGUI headerText;
    public GameObject slidersPanel;

    [Header("Mode Selection")]
    public TMP_Dropdown modeDropdown; // Reference to the Default/Custom dropdown

    [Header("Sliders")]
    public Slider brightnessSlider;
    public Slider contrastSlider;
    public Slider colorSlider;

    private void Start()
    {
        // Toggle the panel visibility when the header button is clicked
        headerButton.onClick.AddListener(TogglePanel);
        
        // Hide the sliders panel initially for a cleaner look
        slidersPanel.SetActive(false); 
    }

    private void TogglePanel()
    {
        slidersPanel.SetActive(!slidersPanel.activeSelf);
        
        // Forces the layout group to update spacing when expanding/collapsing
        LayoutRebuilder.ForceRebuildLayoutImmediate(transform.parent.GetComponent<RectTransform>());
    }
}