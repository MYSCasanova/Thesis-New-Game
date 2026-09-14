using UnityEngine;

public class S6_Checkpoint : MonoBehaviour
{
    public static S6_Checkpoint Instance;
    private Vector3 checkpointPosition;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            checkpointPosition = player.transform.position;
        }
    }

    public void SetCheckpoint(Vector3 newPosition)
    {
        checkpointPosition = newPosition;
    }

    public void RespawnPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            player.transform.position = checkpointPosition;
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero; // Reset velocity to avoid falling through platforms
            }
        }
    }
}
