using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Vertical Camera Follow")]
    public Transform player;

    [Tooltip("Kept for compatibility with existing scene values. No longer used for the hard safety limit (see topSafetyMargin).")]
    public float topPadding = 5f;

    [Tooltip("Autonomous upward scroll speed. Only applied while the player is BELOW the follow threshold.")]
    public float moveSpeed = 8f;

    [Tooltip("Smoothing time used while following. Lower = snappier, higher = floatier. " +
             "Smoothing only tightens when the player gets close to the top edge.")]
    public float smoothTime = 0.2f;

    public int currentLevel;

    [Header("Follow Threshold (new)")]
    [Tooltip("Height of the follow line relative to the camera's vertical midpoint. 0 = exact middle of the screen.")]
    public float followThresholdOffset = 0f;

    [Tooltip("Minimum distance the player is allowed to get to the TOP edge of the view. " +
             "Only when the player gets this close does the camera stop smoothing and lock on.")]
    public float topSafetyMargin = 1.5f;

    private bool activated = false;
    private float smoothVelocity = 0f;
    private Vector3 initialCameraPosition;
    private Camera cam;

    void Start()
    {
        initialCameraPosition = transform.position;
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
    }

    // LateUpdate: runs after the player has moved this frame.
    void LateUpdate()
    {
        if (!activated || player == null || cam == null) return;

        float camY = transform.position.y;

        float ortho = cam.orthographicSize;
        float safety = Mathf.Clamp(topSafetyMargin, 0f, ortho - 0.1f);

        float threshold = camY + followThresholdOffset;
        float playerY = player.position.y;

        // 1) Autonomous scroll: always running, so the camera keeps climbing even if the player stands still.
        float scrollY = camY + moveSpeed * Time.deltaTime;

        // 2) Follow position: only when the player is above the follow line. Otherwise it asks for nothing.
        float followY = camY;
        if (playerY > threshold)
        {
            float targetY = playerY - followThresholdOffset;

            float allowedLead = Mathf.Max(0.1f, ortho - safety - followThresholdOffset);
            float t = Mathf.Clamp01((playerY - threshold) / allowedLead);

            // Smooth for most of the range; tightens only near the top edge.
            float effectiveSmooth = smoothTime * (1f - t * t);

            if (effectiveSmooth < 0.001f)
            {
                followY = targetY;
                smoothVelocity = 0f;
            }
            else
            {
                followY = Mathf.SmoothDamp(camY, targetY, ref smoothVelocity, effectiveSmooth);
            }

            // Hard guarantee: player never closer to the top edge than the safety margin.
            followY = Mathf.Max(followY, playerY - (ortho - safety));
        }
        else
        {
            smoothVelocity = moveSpeed; // keeps the hand-off into following continuous
        }

        // The camera takes whichever demands more height, so the two never fight.
        float newY = Mathf.Max(scrollY, followY);

        // Never move downward.
        if (newY < camY) newY = camY;

        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    public void Activate()
    {
        activated = true;
    }

    public Vector3 GetInitialPosition()
    {
        return initialCameraPosition;
    }

    public void ResetCamera(Vector3 cameraCheckpoint)
    {
        transform.position = cameraCheckpoint;
        activated = false;
        smoothVelocity = 0f;
    }
}