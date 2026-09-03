using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class S2_ComboSystem : MonoBehaviour
{
    [Header("UI Elements")]
    public Slider comboBar;
    public TMP_Text comboText;
    public Button boostButton;

    [Header("Combo Settings")]
    public int currentCombo = 0;
    public int comboRequiredForBoost = 10;
    public float boostDuration = 5f;

    [Header("Player Reference")]
    public S1_PlayerController player;

    void Start()
    {
        comboBar.maxValue = comboRequiredForBoost;
        comboBar.value = 0;
        UpdateUI();

        // Make button disabled by default
        boostButton.interactable = false;
        
        // Connect the button click to our Boost function
        boostButton.onClick.AddListener(ActivateAutoJumpBoost);
    }

    // Call this whenever the player successfully jumps to a new platform
    public void AddCombo(int amount = 1)
    {
        currentCombo += amount;
        UpdateUI();

        if (currentCombo >= comboRequiredForBoost && !boostButton.interactable)
        {
            boostButton.interactable = true; // Enables the button (removes gray shade)
        }
    }

    private void UpdateUI()
    {
        comboText.text = "Combo: " + currentCombo;
        comboBar.value = currentCombo;
    }

    private void ActivateAutoJumpBoost()
    {
        StartCoroutine(BoostRoutine());
    }

    private IEnumerator BoostRoutine()
    {
        // 1. Activate Auto-Jump
        player.isAutoBouncing = true;
        boostButton.interactable = false; // Disable button while boosting
        
        // Reset combo count visually if you want
        currentCombo = 0;
        UpdateUI();

        // 2. Wait for 5 seconds
        yield return new WaitForSeconds(boostDuration);

        // 3. Disable Auto-Jump
        player.isAutoBouncing = false;
    }
}