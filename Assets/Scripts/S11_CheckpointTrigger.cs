
using UnityEngine;

public class S11_CheckpointTrigger : MonoBehaviour
{
    private bool playerInsideTrigger;
    private bool checkpointSaved;
    private Collider2D ground;

    private void Awake()
    {
        Transform groundTransform = transform.parent != null ? transform.parent.Find("Ground") : null;

        if (groundTransform != null)
        {
            ground = groundTransform.GetComponent<Collider2D>();
        }
        else
        {
            Debug.LogError(name + ": Ground child not found!");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInsideTrigger = true;
        checkpointSaved = false;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || !playerInsideTrigger || checkpointSaved || ground == null)
            return;

        if (!other.IsTouching(ground))
            return;

        // Confirm the player is standing above the ground.
        if (other.bounds.min.y < ground.bounds.max.y - 0.1f)
            return;

        if (S6_Checkpoint.Instance == null)
        {
            Debug.LogError("Checkpoint Manager not found!");
            return;
        }

        S6_Checkpoint.Instance.SetCheckpoint(other.transform.position);
        checkpointSaved = true;

        Debug.Log(name + ": CHECKPOINT SAVED!");
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInsideTrigger = false;
            checkpointSaved = false;
        }
    }
}
