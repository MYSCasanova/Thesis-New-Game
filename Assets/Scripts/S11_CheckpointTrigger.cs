using UnityEngine;
using System.Collections;

public class S11_CheckpointTrigger : MonoBehaviour
{
    [Header("Checkpoint Feedback")]
    public GameObject checkpointFeedback;
    public float feedbackDuration = 2f;
    private bool feedbackPlayed;

    [Header("Level Audio")]
    [Tooltip("Leave this blank if the BGM should not change here. Otherwise, type the exact name of the BGM (e.g., 'Level 2').")]
    public string levelBGMName = "";

    [Header("Ground")]
    [Tooltip("Optional. The ground collider of this checkpoint. If left empty, a collider named 'Ground' is searched for under the parent (or under this object if there is no parent).")]
    public Collider2D ground;

    [Tooltip("Extra distance added to the player's groundCheckRadius when testing if the feet are on the ground.")]
    public float footMargin = 0.05f;

    private int playerOverlapCount;
    private bool checkpointSaved;
    private bool reportedMissingManager;
    private S1_PlayerController player;
    private Collider2D playerCollider;

    private void Awake()
    {
        if (ground == null)
        {
            Transform searchRoot = transform.parent != null ? transform.parent : transform;

            foreach (Collider2D c in searchRoot.GetComponentsInChildren<Collider2D>(true))
            {
                if (c.gameObject != gameObject &&
                    string.Equals(c.gameObject.name, "Ground", System.StringComparison.OrdinalIgnoreCase))
                {
                    ground = c;
                    break;
                }
            }
        }

        if (ground == null)
        {
            Debug.LogError(name + ": Ground collider not found! Assign it in the Inspector or name a child 'Ground'.", this);
        }
    }

    private static S1_PlayerController GetPlayer(Collider2D other)
    {
        if (other.attachedRigidbody != null)
            return other.attachedRigidbody.GetComponent<S1_PlayerController>();

        return other.GetComponentInParent<S1_PlayerController>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        S1_PlayerController pc = GetPlayer(other);
        if (pc == null)
            return;

        if (playerOverlapCount == 0)
        {
            checkpointSaved = false;
            reportedMissingManager = false;
        }

        playerOverlapCount++;
        player = pc;
        playerCollider = pc.GetComponent<Collider2D>();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (GetPlayer(other) == null)
            return;

        playerOverlapCount = Mathf.Max(0, playerOverlapCount - 1);

        if (playerOverlapCount == 0)
        {
            checkpointSaved = false;
            player = null;
            playerCollider = null;
        }
    }

    // LateUpdate runs after the player's Update, so the player's position is still where it was
    // when its ground check fired, even if a chained jump launched on this same frame.
    private void LateUpdate()
    {
        if (playerOverlapCount <= 0 || checkpointSaved || ground == null || player == null)
            return;

        if (!IsPlayerOnGround())
            return;

        // Feet must be on top of the ground, not touching its side
        if (playerCollider != null && playerCollider.bounds.min.y < ground.bounds.max.y - 0.1f)
            return;

        if (S6_Checkpoint.Instance == null)
        {
            if (!reportedMissingManager)
            {
                Debug.LogError("Checkpoint Manager not found!", this);
                reportedMissingManager = true;
            }
            return;
        }

        S6_Checkpoint.Instance.SetCheckpoint(player.transform.position);
        checkpointSaved = true;

        if(checkpointFeedback != null)
        {
            if (!feedbackPlayed)
            {
                StartCoroutine(ShowCheckpointFeedback());
                feedbackPlayed = true;
            }
        }

        // --- NEW BGM LOGIC ---
        // If a BGM name was typed into the inspector, tell the AudioManager to play it.
        // It won't restart if it's already playing.
        if (!string.IsNullOrEmpty(levelBGMName))
        {
            if (S16_AudioManager.instance != null)
            {
                S16_AudioManager.instance.PlayBGM(levelBGMName);
            }
        }

        Debug.Log(name + ": CHECKPOINT SAVED!", this);
    }

    // Same idea as the player's own ground check (feet circles), so it matches the moment a jump launches,
    // plus the old physics-contact test as a fallback.
    private bool IsPlayerOnGround()
    {
        if (playerCollider != null && playerCollider.IsTouching(ground))
            return true;

        return FootOnGround(player.leftFoot) || FootOnGround(player.rightFoot);
    }

    private bool FootOnGround(Transform foot)
    {
        if (foot == null)
            return false;

        Vector2 footPos = foot.position;
        Vector2 closest = ground.ClosestPoint(footPos);

        return Vector2.Distance(footPos, closest) <= player.groundCheckRadius + footMargin;
    }

    private IEnumerator ShowCheckpointFeedback()
    {
        checkpointFeedback.SetActive(true);
        yield return new WaitForSeconds(feedbackDuration);
        checkpointFeedback.SetActive(false);
    }
}