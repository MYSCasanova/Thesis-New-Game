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

    public bool isTakingDamage = false;

    void Start()
    {
        currentLives = maxLives;
    }

    public int LoseLife(int damage)
    {

        if (isGameOver || isTakingDamage)
            return currentLives;

        isTakingDamage = true;

        // If the current scene is the tutorial, respawn the player without losing lives
        bool isTutorial = SceneManager.GetSceneByName("GYM_TutorialLevel").isLoaded;
        Debug.Log("Is Tutorial: " + isTutorial);      

        if (isTutorial)
        {
            RespawnPlayer();
            return currentLives;
        }
        
        // Beta Game: Lose lives normally
        currentLives -= damage;

        if (currentLives > 0 && animator != null)
        {
            animator.SetTrigger("Hurt");
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
        isTakingDamage = false;
        
        if (S6_Checkpoint.Instance == null)
        {
            Debug.LogError("Checkpoint Manager not found!");
            return;
        }

        Vector3 checkpointPosition = S6_Checkpoint.Instance.GetCheckpointPosition();
        transform.position = checkpointPosition;
        Rigidbody2D rb = GetComponent<Rigidbody2D>();

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
                cameraFollow.enabled = true;
                cameraFollow.ResetCamera(cameraCheckpoint);
            }
        }

        S1_PlayerController playerController = GetComponent<S1_PlayerController>();

        if (playerController != null)
        {
            playerController.StopRespawning();
            playerController.ResetMultiplier();
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
    }

    public void SetGameOverPanel(string sceneName, GameObject panel)
    {
        if (sceneName == "GYM_BetaGame")
        {
            betaGameOverPanel = panel;
        }
    }
    
    public void Die()
    {
        isGameOver = true; // Set the game over flag to true

        // Stop player movement
        S1_PlayerController playerController = GetComponent<S1_PlayerController>();

        if (playerController != null)
        {
            playerController.enabled = false;
        }

        Rigidbody2D rb = GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        Scene betaScene = SceneManager.GetSceneByName("GYM_BetaGame");

        // If the Beta Game is loaded, show its Game Over panel.
        if (betaScene.isLoaded)
        {
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

                if(comboSystem != null && scoreText != null)
                {
                    scoreText.text = "Score: " + comboSystem.totalScore;
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
        }
    }

    public int GetCurrentLives()
    {
        return currentLives;
    } 
}
