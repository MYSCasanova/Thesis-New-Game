using UnityEngine;

public class S7_HealthSystem : MonoBehaviour
{
    public int maxLives;
    public int maxHealth;
    private int currentLives;
    private int currentHealth;

    void Start()
    {
        currentLives = maxLives;
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        Debug.Log("Player took damage. Current health: " + currentHealth);

        if(currentHealth <= 0)
        {
            LoseLife();
        }
    }

    public void LoseLife()
    {
        currentLives--;

        Debug.Log("Lost a life. Current lives: " + currentLives);

        if(currentLives <= 0)
        {
            Die();
        }
        else
        {
            currentHealth = maxHealth;
            RespawnPlayer();
        }
    }

    public void RespawnPlayer()
    {
        Vector3 checkpointPosition = S6_Checkpoint.Instance.GetCheckpointPosition();
        transform.position = checkpointPosition;
        Rigidbody2D rb = GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero; // Reset velocity to avoid falling through platforms
        }

        if (Camera.main != null)
        {
            Vector3 cameraCheckpoint = S6_Checkpoint.Instance.GetCameraCheckpoint();
            Camera.main.GetComponent<CameraFollow>().ResetCamera(cameraCheckpoint);
        }

        GetComponent<S1_PlayerController>().StopRespawning();

        GetComponent<S1_PlayerController>().ResetMultiplier();

    }
    
    public void Die()
    {
        //make sure to add death logic
        Debug.Log("Game Over");
    }

    public int GetCurrentLives()
    {
        return currentLives;
    }
}
