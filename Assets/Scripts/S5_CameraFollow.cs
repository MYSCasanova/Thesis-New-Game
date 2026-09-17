using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform player;
    public float moveSpeed = 5f;
    private bool activated = false;

    void Update()
    {
        if (!activated) return;

        transform.position += Vector3.up * moveSpeed * Time.deltaTime;

    }

    public void Activate()
    {
        activated = true;
    }

    public void ResetCamera(Vector3 checkpointPosition)
    {
        transform.position = checkpointPosition;
        activated = false;
    }
}