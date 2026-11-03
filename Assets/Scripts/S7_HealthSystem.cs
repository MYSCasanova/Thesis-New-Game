using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class S7_HealthSystem : MonoBehaviour
{
    public int maxLives;
    public int currentLives; //Set to public so that it can be accessed by other scripts

    [Header("Game Over Panel")]
    public GameObject betaGameOverPanel;
    public TMP_Text scoreText; // Reference to the TMP_Text component for displaying lives

    [Header("Score Reference")]
    public S2_ComboSystem comboSystem; // Reference to the ComboSystem script

    [Header("Hurt Animation")]
    public Animator animator;   

    [Header("Respawn Settings")]
    public float respawnDelay = 0.3f;

    [Header("GameOver check")]
    public bool isGameOver = false; // Flag to indicate if the game is in progress

    [Header("Lives")]
    public LivesUI livesUI;
    public bool isTakingDamage = false;

    [Header("Dev Mode")]
    public bool devMode = false;


    void Start()
    {
        currentLives = maxLives;
        
        if (livesUI != null)
        {
            livesUI.UpdateHearts(currentLives);
        }
    }

    // Ensures Dev Mode checkbox works in both Editor and Build
    private bool IsDevModeActive
    {
        get
        {
    #if UNITY_EDITOR
            return devMode;
    #else
            return false;
    #endif
        }
    }

    public int LoseLife(int damage)
    {

        if (isGameOver || isTakingDamage || IsDevModeActive)
            return currentLives;

        // ---> ADD THIS BLOCK START <---
        // Completely bypass taking damage if the player is currently in the Boost phase
        S1_PlayerController pc = GetComponent<S1_PlayerController>();
        if (pc != null && pc.isAutoBouncing && !FellBelowCamera())
        {
            return currentLives;
        }
        // ---> ADD THIS BLOCK END <---

        isTakingDamage = true;

        // Check if the ACTIVE scene is the tutorial, rather than just loaded in memory
        bool isTutorial = SceneManager.GetActiveScene().name == "GYM_TutorialLevel";
        Debug.Log("Is Tutorial: " + isTutorial);      

        if (isTutorial)
        {
            RespawnPlayer();
            return currentLives;
        }
        
        // Beta Game: Lose lives normally
        currentLives -= damage;

        // Update hearts safely
        if (livesUI != null)
        {
            livesUI.UpdateHearts(currentLives);
        }
        else
        {
            Debug.LogError("LivesUI reference is missing on Player!");
        }

        // Play hurt animation
        if (currentLives > 0 && animator != null)
        {
            animator.SetTrigger("Hurt");
        }

        // Play hurt sound safely
        if (currentLives > 0 && S16_AudioManager.instance != null)
        {
            S16_AudioManager.instance.PlaySFX("Hurt");
        }

        Debug.Log("Lost a life. Current lives: " + currentLives);

        if(currentLives <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(RespawnAfterDelay());
        }

        return currentLives;
    }

    public void RespawnPlayer()
    {
        if (S6_Checkpoint.Instance == null)
        {
            Debug.LogError("Checkpoint Manager not found!");
            return;
        }

        // 1. Get the CURRENT active checkpoint (fixes the issue of not progressing)
        Vector3 checkpointPosition = S6_Checkpoint.Instance.GetCheckpointPosition();
        transform.position = checkpointPosition;
        Rigidbody2D rb = GetComponent<Rigidbody2D>();

        // Reset all platforms when the player respawns
        S3_Platform.ResetAllPlatforms();

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero; // Reset velocity to avoid falling through platforms
        }

        if (Camera.main != null)
        {
            CameraFollow cameraFollow = Camera.main.GetComponent<CameraFollow>();

            if (cameraFollow != null)
            {
                Vector3 cameraCheckpoint = S6_Checkpoint.Instance.GetCameraCheckpoint();
                Vector3 initialPlayerPos = S6_Checkpoint.Instance.GetInitialPlayerCheckpoint();
                
                // 2. Are we at the Level 1 Start?
                if (Vector3.Distance(checkpointPosition, initialPlayerPos) < 0.1f)
                {
                    // Use the exact initial camera framing so we don't see the blue void
                    cameraCheckpoint = S6_Checkpoint.Instance.GetInitialCameraCheckpoint();
                }
                else
                {
                    // 3. We are at a NEW checkpoint. Apply a tiny safety net to prevent instant-death loops.
                    float orthoSize = Camera.main.orthographicSize;
                    float cameraBottom = cameraCheckpoint.y - orthoSize;
                    
                    // If the saved camera is so high that the player spawns off-screen, pull it down
                    if (checkpointPosition.y < cameraBottom + 0.1f)
                    {
                        cameraCheckpoint.y = checkpointPosition.y + orthoSize - 0.1f;
                    }
                }
                
                cameraFollow.enabled = true;
                cameraFollow.ResetCamera(cameraCheckpoint);
            }
        }

        S1_PlayerController playerController = GetComponent<S1_PlayerController>();

        if (playerController != null)
        {
            playerController.enabled = true;
            playerController.StopRespawning();
            playerController.ResetMultiplier();
        }

        isTakingDamage = false;

        // Stop the boost and visuals if revived/returned to checkpoint
        if (comboSystem != null)
        {
            comboSystem.CancelBoost();
        }
    }

    private IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelay);
        RespawnPlayer();
    }

    public void ResetLives()
    {
        currentLives = maxLives;
        isGameOver = false;
        isTakingDamage = false;

        if (livesUI != null)
        {
            livesUI.UpdateHearts(currentLives);
        }
    }

    public void SetGameOverPanel(string sceneName, GameObject panel)
    {
        switch (sceneName)
        {
            case "GYM_BetaGame":
            case "GYM_V2Game":
            case "GYM_V3Game":
            
                betaGameOverPanel = panel;
                break;
        }
    }
    
    public void Die()
    {
        // 1. Get the player controller once at the top
        S1_PlayerController playerController = GetComponent<S1_PlayerController>();

        // 2. Prevent instant-death while boosting, EXCEPT if falling off-screen
        if (playerController != null && playerController.isAutoBouncing && !FellBelowCamera())
        {
            return; 
        }
        
        isGameOver = true; // Set the game over flag to true

        // 3. Stop player movement
        if (playerController != null)
        {
            playerController.enabled = false;
        }

        Rigidbody2D rb = GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        Scene activeScene = SceneManager.GetActiveScene();

        // If the Game is loaded, show its Game Over panel.
        switch (activeScene.name)
        {
            case "GYM_BetaGame":
            case "GYM_V2Game":
            case "GYM_V3Game":

                // Disable the CameraFollow script to stop camera movement when the panel appears
                if (Camera.main != null)
                {
                    CameraFollow cameraFollow = Camera.main.GetComponent<CameraFollow>();
                    if (cameraFollow != null)
                    {
                        cameraFollow.enabled = false;
                    }
                }

                // Show the Beta Game Over panel and display the final score
                if (betaGameOverPanel != null)
                {
                    betaGameOverPanel.SetActive(true);

                    // --- NEW BGM LOGIC ---
                    // Trigger the Game Over BGM right as the panel appears
                    if (S16_AudioManager.instance != null)
                    {
                        S16_AudioManager.instance.PlayBGM("Game Over");
                        S16_AudioManager.instance.PlaySFX("Death");
                    }

                    if(comboSystem != null && scoreText != null)
                    {
                        scoreText.text = comboSystem.totalScore.ToString();
                    }
                    else
                    {
                        Debug.LogError("ComboSystem or ScoreText is not assigned!");
                    }
                }
                else
                {
                    Debug.LogError("Beta Game Over panel has not been registered!");
                }

                break;
        }
    }

    public int GetCurrentLives()
    {
        return currentLives;
    } 

    private bool FellBelowCamera()
    {
        if (Camera.main != null)
        {
            float camBottom = Camera.main.transform.position.y - Camera.main.orthographicSize;
            return transform.position.y < camBottom;
        }
        return false;
    }
}