using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Vertical Camera Follow")]
    public Transform player;
    public float topPadding = 5f;
    public float moveSpeed = 8f;
    public float smoothTime = 0.2f;
    private bool activated = false;
    private float smoothVelocity = 0f;

    void Update()
    {
        if (!activated) return;

         // Normal continuous upward scrolling
        float newY = transform.position.y + moveSpeed * Time.deltaTime;

        // Check the top edge of the camera
        float cameraTopY = transform.position.y + Camera.main.orthographicSize;

        // If player is above the allowed area, move camera upward
        if (player.position.y > cameraTopY - topPadding)
        {
            float targetY = player.position.y
                + topPadding;

            newY = Mathf.SmoothDamp(transform.position.y, targetY, ref smoothVelocity, smoothTime);
        }

        else
        {
            smoothVelocity = 0f;
        }

        transform.position = new Vector3(transform.position.x, newY, transform.position.z);

    }

    public void Activate()
    {
        activated = true;
    }

    public void ResetCamera(Vector3 checkpointPosition)
    {
        transform.position = checkpointPosition;
        activated = false;
        smoothVelocity = 0f;
    }
}