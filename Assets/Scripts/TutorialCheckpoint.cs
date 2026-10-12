using UnityEngine;

public class TutorialCheckpoint : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
            return;

        if (S6_Checkpoint.Instance == null)
        {
            Debug.LogError("Checkpoint Manager not found!");
            return;
        }

        S6_Checkpoint.Instance.SetCheckpoint(collision.transform.position);

        Debug.Log("Tutorial checkpoint saved: " + gameObject.name);
    }
}