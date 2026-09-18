// UPDATED 9/18 5PM
// CHANGED health.TakeDamage(damage) to health.LoseLife(damage)

using UnityEngine;      

public class S4_Hazards : MonoBehaviour
{
    public enum HazardType
    {
        Spike,
        FallingObject,
        MovingBlade,
        FallingPlatform
    }

    public HazardType hazardType;
    public int damage;

    [Header("Moving Blade")]
    public float moveDistance;
    public float moveSpeed;

    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {
        if (hazardType == HazardType.MovingBlade)
        {
            float movement = Mathf.Sin(Time.time * moveSpeed) * moveDistance;

            transform.position = startPosition + Vector3.right * movement;
        }
    }

    public void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;

        S7_HealthSystem health = collision.gameObject.GetComponent<S7_HealthSystem>();

        if (health == null) return;

        Rigidbody2D playerRb = collision.gameObject.GetComponent<Rigidbody2D>();

        if (playerRb == null) return;

        if (playerRb.linearVelocity.y > 0.1f) return;

        switch (hazardType) 
        {
            //SPIKES
            case HazardType.Spike:
            health.LoseLife(damage);
            break;

            //FALLING OBJECT
            case HazardType.FallingObject:
            health.LoseLife(damage);
            break;

            //MOVING BLADE
            case HazardType.MovingBlade:
            health.LoseLife(damage);
            break;

            //FALLING PLATFORM
            case HazardType.FallingPlatform:
            health.LoseLife(damage);
            break;

        }
        
    }
}
