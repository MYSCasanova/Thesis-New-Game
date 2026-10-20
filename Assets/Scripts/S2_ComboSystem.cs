using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class S2_ComboSystem : MonoBehaviour
{
    [Header("UI Elements")]
    public Slider comboBar;
    public TMP_Text scoreText;
    public Button boostButton;

    [Header("Combo Settings")]
    public int currentCombo = 0;
    public int totalScore = 0; // NEW: Tracks your points
    public int scoreMultiplier = 1; // NEW: Current multiplier; Set to public
    public int comboRequiredForBoost = 8;
    public float boostDuration = 5f;

    [Header("Player Reference")]
    public S1_PlayerController player;

    [Header("Combo Blocks")]
    public Image[] comboBlocks;

    

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
        
        // Multiplier Tiers
        if (currentCombo >= 10) scoreMultiplier = 3;
        else if (currentCombo >= 5) scoreMultiplier = 2;
        else scoreMultiplier = 1;

        UpdateUI();

        if (currentCombo >= comboRequiredForBoost && !boostButton.interactable)
        {
            boostButton.interactable = true;
        }
    }

    public void AddScore(int points)
    {
        Debug.Log("AddScore called: " + points);
        // Applies your chain multiplier to every score you get
        totalScore += (points * scoreMultiplier);
        UpdateUI();
    }

    public void ResetScore()
    {
        totalScore = 0;
        currentCombo = 0;
        scoreMultiplier = 1;

        UpdateUI();
    }

    private void UpdateUI()
    {        
        // Only update scoreText if it's assigned in the inspector
        if (scoreText != null) scoreText.text = "Score: " + totalScore;
        
        if (comboBar != null) comboBar.value = currentCombo;

        if (comboBlocks != null)
        {
            Debug.Log("Current Combo: " + currentCombo + " | Blocks: " + comboBlocks.Length);
            for (int i = 0; i < comboBlocks.Length; i++)
            {
                if (comboBlocks[i] != null)
                {
                    comboBlocks[i].gameObject.SetActive(i < currentCombo);
                }
                else
                {
                    Debug.LogWarning("Block " + (i + 1) + " is not assigned!");
                }
            }
        }
    }

    private void ActivateAutoJumpBoost()
    {
        // --- NEW BGM LOGIC ---
        // Play the Booster SFX when the button is pressed
        if (S16_AudioManager.instance != null)
        {
            S16_AudioManager.instance.PlaySFX("Booster");
        }

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