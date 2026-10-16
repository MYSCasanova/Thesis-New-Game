using UnityEngine;
using System.Collections.Generic;

public class S3_Platform : MonoBehaviour
{    
    [Header("Platform Settings")]
    public int floorNumber;

    public enum MovementDirection
    {
        Static,
        Horizontal,
        Vertical,
        Falling
    }

    [Header("Movement")]
    public MovementDirection direction;
    public float distance = 3f;
    public float speed = 2f;

    [Header("Platform Properties")]
    public bool isIcy; //If platform is icy, it will affect deceleration of player when on it
    public bool isBouncy; // If platform is bouncy, it will affect the jump height of the player when on it

    [Header("Breaking Platform")]
    public bool isBreaking; //If platform is breaking, it will break after the player stands on it
    public float breakDelay = 2f; // Delay before the platform breaks after the player stands on it
    private bool isPlayerOnPlatform; // Tracks if the player is currently on the platform
    private float breakTimer; // Timer to track how long the player has been on the platform

    [Header("Falling Platform")]
    private bool isFalling = false;
    public float fallSpeed = 5f;
    public float destroyDelay = 3f;

    private Rigidbody2D rb;
    private Vector3 startPosition;
    private Animator animator;
    private bool hasBroken = false;

    private static List<S3_Platform> allPlatforms = new List<S3_Platform>();

    private void Awake()
    {
        if (!allPlatforms.Contains(this))
        {
            allPlatforms.Add(this);
        }
    }

    void Start()
    {
        startPosition = transform.position;
        animator = GetComponentInChildren<Animator>();
        //Used for falling platforms
        rb = GetComponent<Rigidbody2D>();
        if (direction == MovementDirection.Falling)
        {
            rb.gravityScale = 0f;
        }
    }

    void Update()
    {
        MovePlatform();

        if(isBreaking && isPlayerOnPlatform && !hasBroken) // Only start the timer if the platform is a breaking type and the player is on it and it hasnt broken yet
        {
            breakTimer += Time.deltaTime;
            if(breakTimer >= breakDelay)
            {
                BreakPlatform();
            }
        }
    }

    private void MovePlatform()
    {
        float movement = Mathf.Sin(Time.time * speed) * distance;

        switch (direction)
        {
            case MovementDirection.Horizontal:
                transform.position = startPosition + new Vector3(movement, 0, 0);
                break;

            case MovementDirection.Vertical:
                transform.position = startPosition + new Vector3(0, movement, 0);
                break;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;

        Rigidbody2D playerRb = collision.gameObject.GetComponent<Rigidbody2D>();

        if (playerRb == null) return;

        if (playerRb.linearVelocity.y > 0.1f) return;

        S9_TutorialManager tutorialManager = FindFirstObjectByType<S9_TutorialManager>();
        
        if(tutorialManager != null)
        {
            tutorialManager.PlatformLanded(floorNumber);
        }

        if (isBreaking)
        {
            isPlayerOnPlatform = true; // Start the timer when the player lands on the platform
            // breakTimer = 0f; // Reset the timer when the player lands on the platform
        }
        if (direction == MovementDirection.Falling && !isFalling)
        {
            isFalling = true;
            rb.gravityScale = 1f; // Apply ggravity to falling platform when player steps on it
            Invoke(nameof(DestroyPlatform), destroyDelay);
        }
    }

    private void BreakPlatform()
    {
        if (hasBroken) return;

        hasBroken = true;
        isPlayerOnPlatform = false;

        // Play the breaking animation
        if (animator != null)
        {
            animator.SetTrigger("Break");
        }
        else
        {
            Debug.LogWarning("No Animator found on the platform or its children!");
        }

        // Stop the platform from being used
        GetComponent<Collider2D>().enabled = false;

        Invoke(nameof(DestroyPlatform), 0.6f);
    }

    private void DestroyPlatform()
    {
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        allPlatforms.Remove(this);
    }

    public static void ResetAllPlatforms()
    {
        foreach (S3_Platform platform in allPlatforms)
        {
            if (platform != null)
            {
                platform.ResetPlatform();
            }
        }
    }

    public void ResetPlatform()
    {
        CancelInvoke();

        // Reactivate the platform
        gameObject.SetActive(true);

        // Reset breaking-platform variables
        hasBroken = false;
        isPlayerOnPlatform = false;
        breakTimer = 0f;

        // Re-enable the platform collider
        GetComponent<Collider2D>().enabled = true;

        // Reset the breaking animation
        if (animator != null)
        {
            animator.ResetTrigger("Break");
            animator.Play("IdleBreakingPlatform", 0, 0f);
        }

        // Reset falling-platform variables
        isFalling = false;

        if (rb != null && direction == MovementDirection.Falling)
        {
            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.zero;
            transform.position = startPosition;
        }
    }
}