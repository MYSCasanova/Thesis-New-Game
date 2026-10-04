using UnityEngine;

public class TutorialCheckpoint : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.gameObject.CompareTag("Player"))
            return;

        S6_Checkpoint checkpoint = FindFirstObjectByType<S6_Checkpoint>();

        if (checkpoint != null)
        {
            checkpoint.SetCheckpoint(collision.transform.position);

            Debug.Log("Tutorial checkpoint saved at: " + gameObject.name);
        }
    }
}
