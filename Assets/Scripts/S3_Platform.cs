using UnityEngine;

public class S3_Platform : MonoBehaviour
{
    public enum MovementDirection
    {
        Static,
        Horizontal,
        Vertical
    }
    public int floorNumber;
    public MovementDirection direction;

    public float distance = 3f;
    public float speed = 2f;

    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {
        float movement = Mathf.Sin(Time.time * speed) * distance;

        if (direction == MovementDirection.Horizontal)
        {
            transform.position = startPosition + new Vector3(movement, 0, 0);
        }
        else if (direction == MovementDirection.Vertical)
        {
            transform.position = startPosition + new Vector3(0, movement, 0);
        }
    }
}