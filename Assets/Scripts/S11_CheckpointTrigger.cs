using UnityEngine;

public class S11_CheckpointTrigger : MonoBehaviour
{
    [Header("Level Audio")]
    [Tooltip("Leave this blank if the BGM should not change here. Otherwise, type the exact name of the BGM (e.g., 'Level 2').")]
    public string levelBGMName = "";

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

        if (other.bounds.min.y < ground.bounds.max.y - 0.1f)
            return;

        if (S6_Checkpoint.Instance == null)
        {
            Debug.LogError("Checkpoint Manager not found!");
            return;
        }

        S6_Checkpoint.Instance.SetCheckpoint(other.transform.position);
        checkpointSaved = true;

        // --- NEW BGM LOGIC ---
        // If a BGM name was typed into the inspector, tell the AudioManager to play it.
        // Because of your previous fix, it won't restart if it's already playing.
        if (!string.IsNullOrEmpty(levelBGMName))
        {
            if (S16_AudioManager.instance != null)
            {
                S16_AudioManager.instance.PlayBGM(levelBGMName);
            }
        }

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