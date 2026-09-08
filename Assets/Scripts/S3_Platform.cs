using UnityEngine;

public class S3_Platform : MonoBehaviour
{    
    [Header("Platform Settings")]
    public int floorNumber;
    public float distance = 3f;
    public float speed = 2f;
    public enum MovementDirection
    {
        Static,
        Horizontal,
        Vertical
    }
    public MovementDirection direction;
    public bool isIcy; //If platform is icy, it will affect deceleration of player when on it
    public bool isBreaking; //If platform is breaking, it will break after the player stands on it
    public float breakDelay = 2f; // Delay before the platform breaks after the player stands on it
    private bool isPlayerOnPlatform; // Tracks if the player is currently on the platform
    private float breakTimer; // Timer to track how long the player has been on the platform
    public bool isBouncy; // If platform is bouncy, it will affect the jump height of the player when on it

    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {
        MovePlatform();

        if(isBreaking && isPlayerOnPlatform) // Only start the timer if the platform is a breaking type and the player is on it
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

        if (direction == MovementDirection.Horizontal)
        {
            transform.position = startPosition + new Vector3(movement, 0, 0);
        }
        else if (direction == MovementDirection.Vertical)
        {
            transform.position = startPosition + new Vector3(0, movement, 0);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && isBreaking)
        {
            isPlayerOnPlatform = true; // Start the timer when the player lands on the platform
            breakTimer = 0f; // Reset the timer when the player lands on the platform
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && isBreaking)
        {
            isPlayerOnPlatform = false; // Stop the timer when the player leaves the platform
            breakTimer = 0f; // Reset the timer when the player leaves the platform
        }
    }

    private void BreakPlatform()
    {
        // Disable the platform's collider and renderer to simulate breaking
        GetComponent<Collider2D>().enabled = false;
        GetComponent<SpriteRenderer>().enabled = false;

        // Optionally, you can add a particle effect or sound effect here

        // Set platform to inactive
        gameObject.SetActive(false);
    }
}