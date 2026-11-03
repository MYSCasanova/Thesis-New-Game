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
    public int totalScore = 0;
    public int scoreMultiplier = 1;
    public int comboRequiredForBoost = 8;
    public float boostDuration = 5f;

    [Header("Player Reference")]
    public S1_PlayerController player;
    
    [Header("Obstacle Immunity Settings")]
    public string obstacleLayerName = "Hazard"; // Set this to your obstacle layer name in the Inspector

    [Header("Combo Blocks")]
    public Image[] comboBlocks;

    private bool justFinishedBoost = false;
    private Coroutine blinkCoroutine;

    void Start()
    {
        comboBar.maxValue = comboRequiredForBoost;
        comboBar.value = 0;
        UpdateUI();

        boostButton.interactable = false;
        boostButton.onClick.AddListener(ActivateAutoJumpBoost);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.B) && boostButton != null && boostButton.interactable)
        {
            ActivateAutoJumpBoost();
        }
    }

    public void AddCombo(int amount = 1)
    {
        if (player != null && player.isAutoBouncing) return;

        if (justFinishedBoost)
        {
            justFinishedBoost = false; 
            return; 
        }

        currentCombo += amount;
        
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
        totalScore += (points * scoreMultiplier);
        UpdateUI();
    }

    public void ResetScore()
    {
        totalScore = 0;
        currentCombo = 0;
        scoreMultiplier = 1;

        if (boostButton != null) boostButton.interactable = false;

        UpdateUI();
    }

    private void UpdateUI()
    {        
        if (scoreText != null) scoreText.text = "" + totalScore;
        if (comboBar != null) comboBar.value = currentCombo;

        if (comboBlocks != null)
        {
            for (int i = 0; i < comboBlocks.Length; i++)
            {
                if (comboBlocks[i] != null)
                {
                    comboBlocks[i].gameObject.SetActive(i < currentCombo);
                }
            }
        }

        if (boostButton != null)
        {
            boostButton.interactable = currentCombo >= comboRequiredForBoost && player != null && !player.isAutoBouncing; 
        }
    }

    private void ActivateAutoJumpBoost()
    {
        // Play the Booster SFX when the button is pressed (Assuming S16 exists in context)
        // If S16_AudioManager is throwing errors, ensure you add its script to your project or remove this block
        var audioManagerType = System.Type.GetType("S16_AudioManager");
        if (audioManagerType != null)
        {
            // Reflection fallback if S16_AudioManager is not compiled in the same assembly yet, 
            // otherwise use S16_AudioManager.instance.PlaySFX("Booster"); directly if it exists.
            SendMessageUpwards("PlaySFX", "Booster", SendMessageOptions.DontRequireReceiver);
        }

        StartCoroutine(BoostRoutine());
    }

    private IEnumerator BoostRoutine()
    {
        // 1. Activate Auto-Jump & Lock UI
        player.isAutoBouncing = true;
        boostButton.interactable = false; 
        currentCombo = 0;
        UpdateUI();

        // 2. Activate Obstacle Immunity
        int playerLayer = player.gameObject.layer;
        int obsLayer = LayerMask.NameToLayer(obstacleLayerName);
        if (obsLayer != -1) 
        {
            Physics2D.IgnoreLayerCollision(playerLayer, obsLayer, true);
        }
        else
        {
            Debug.LogWarning("Obstacle layer not found. Make sure 'obstacleLayerName' matches your layer string exactly.");
        }

        // 3. Start Visual Blinking via S15's SpriteRenderer
        blinkCoroutine = StartCoroutine(BlinkEffectRoutine());

        // 4. Wait for duration
        yield return new WaitForSeconds(boostDuration);

        // 5. Disable Auto-Jump & Remove Immunity
        player.isAutoBouncing = false;
        if (obsLayer != -1) 
        {
            Physics2D.IgnoreLayerCollision(playerLayer, obsLayer, false);
        }

        // 6. Cleanup Visuals
        if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
        ResetPlayerColor();

        justFinishedBoost = true; 
    }

    private IEnumerator BlinkEffectRoutine()
    {
        S15_VisualReceiver s15 = player.GetComponentInChildren<S15_VisualReceiver>();
        if (s15 != null)
        {
            SpriteRenderer sr = s15.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                while (true)
                {
                    // Flash semi-transparent green
                    sr.color = new Color(0.5f, 1f, 0.5f, 0.6f);
                    yield return new WaitForSeconds(0.15f);
                    // Flash standard white
                    sr.color = Color.white;
                    yield return new WaitForSeconds(0.15f);
                }
            }
        }
    }

    private void ResetPlayerColor()
    {
        S15_VisualReceiver s15 = player.GetComponentInChildren<S15_VisualReceiver>();
        if (s15 != null)
        {
            SpriteRenderer sr = s15.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = Color.white;
        }
    }

    public void CancelBoost()
    {
        if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
        
        if (player != null)
        {
            player.isAutoBouncing = false;
            
            // Turn collision back on
            int playerLayer = player.gameObject.layer;
            int obsLayer = LayerMask.NameToLayer(obstacleLayerName);
            if (obsLayer != -1) Physics2D.IgnoreLayerCollision(playerLayer, obsLayer, false);
        }
        
        ResetPlayerColor();
        justFinishedBoost = false;
        
        UpdateUI();
    }
}

