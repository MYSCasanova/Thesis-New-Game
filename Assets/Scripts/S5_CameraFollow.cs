using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform player;
    public float moveSpeed = 5f;

    private bool activated = false;

    void Update()
    {
        if (!activated)
            return;

        if (player.position.y > transform.position.y)
        {
            Vector3 targetPosition = new Vector3(
                transform.position.x,
                player.position.y,
                transform.position.z
            );

            transform.position = Vector3.Lerp(
                transform.position,
                targetPosition,
                moveSpeed * Time.deltaTime
            );
        }
    }

    public void Activate()
    {
        activated = true;
    }
}