using UnityEngine;

public class S1_PlayerController : MonoBehaviour
{
    [Header("Icy Tower Movement")]
    public float acceleration = 15f;
    public float deceleration = 10f;
    public float maxSpeed = 10f;

    [Header("Jumping Mechanics")]
    public float jumpForce = 20f; // High force to fight heavy gravity
    public float shortJumpMultiplier = 0.3f; // Cuts height aggressively when releasing space
    public float edgeJumpMultiplier = 0.75f; // Lower jump on edges
    public float directionChangeBoost = 3f;  // Upward boost when switching directions in air
    public float momentumJumpBoost = 0.5f;   // Running faster makes you jump slightly higher
    
    [Header("Auto-Bounce (Jump Chaining)")]
    public bool isAutoBouncing = false; // Toggle this in inspector for continuous bouncing
    public float jumpBufferTime = 0.2f; // Allows jumping just before hitting the ground
    private float jumpBufferCounter;

    [Header("Collision")]
    public Transform leftFoot;
    public Transform rightFoot;
    public float groundCheckRadius = 0.1f;
    public LayerMask groundLayer;

    [Header("Combo Tracking")]
    public S2_ComboSystem comboSystem; // Drag your Canvas (with the ComboSystem script) here
    private int currentFloor = 0;

    private Rigidbody2D rb;
    private float moveInput;
    private bool isGrounded;
    private bool groundLeft;
    private bool groundRight;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        moveInput = Input.GetAxisRaw("Horizontal");

        // 1. Check if grounded (Left foot, Right foot, or both)
        groundLeft = Physics2D.OverlapCircle(leftFoot.position, groundCheckRadius, groundLayer);
        groundRight = Physics2D.OverlapCircle(rightFoot.position, groundCheckRadius, groundLayer);
        isGrounded = groundLeft || groundRight;

        // 2. Jump Buffering / Auto Jump Queueing
        if (Input.GetButtonDown("Jump")) {
            jumpBufferCounter = jumpBufferTime;
        } else {
            jumpBufferCounter -= Time.deltaTime;
        }

        // 3. Execute Jump (Manual or Auto-bounce)
        if (isGrounded && (jumpBufferCounter > 0f || isAutoBouncing))
        {
            PerformJump();
            jumpBufferCounter = 0f;
        }

        // 4. Short Jump (Release Spacebar early)
        if (Input.GetButtonUp("Jump") && rb.linearVelocity.y > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * shortJumpMultiplier);
        }
    }

    void FixedUpdate()
    {
        // 5. Icy Tower Acceleration / Slippery Movement
        float targetSpeed = moveInput * maxSpeed;
        float speedDiff = targetSpeed - rb.linearVelocity.x;
        float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;
        
        float movement = Mathf.Pow(Mathf.Abs(speedDiff) * accelRate, 0.9f) * Mathf.Sign(speedDiff);
        rb.AddForce(movement * Vector2.right);

        // 6. Direction Change Boost in Air
        if (!isGrounded && moveInput != 0)
        {
            // If pressing a direction opposite to current momentum
            if ((moveInput > 0 && rb.linearVelocity.x < -0.1f) || (moveInput < 0 && rb.linearVelocity.x > 0.1f))
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y + directionChangeBoost * Time.fixedDeltaTime);
            }
        }
    }

    private void PerformJump()
    {
        float currentJump = jumpForce;

        // 7. Momentum Boost (Running fast = higher jump)
        float speedBoost = Mathf.Abs(rb.linearVelocity.x) * momentumJumpBoost;
        currentJump += speedBoost;

        // 8. Edge Jump Logic (Only one foot is on the ground)
        if (groundLeft != groundRight)
        {
            currentJump *= edgeJumpMultiplier; // Lower jump height
        }

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, currentJump);
    }
    void OnCollisionEnter2D(Collision2D collision)
    {
        // Check if the object we landed on has the "Platform" tag
        if (collision.gameObject.CompareTag("Platform"))
        {
            S3_Platform landedPlatform = collision.gameObject.GetComponent<S3_Platform>();
            
            if (landedPlatform != null)
            {
                int landedFloor = landedPlatform.floorNumber;
                int floorsSkipped = landedFloor - currentFloor;

                transform.SetParent(collision.transform); // Make the player a child of the platform TEMPORARILY so it moves with the platform

                // If we skipped at least 1 floor (e.g., Jumped from Floor 1 to 3)
                if (floorsSkipped > 1)  //For Debug use >= 1
                {
                    // Trigger the combo in our UI!
                    comboSystem.AddCombo(floorsSkipped);
                }

                if (landedFloor >= 3) //camera scrolls up when player jumps on platform 3
                {
                    Camera.main.GetComponent<CameraFollow>().Activate();
                }

                // Update our current floor so we can't farm combos by jumping in place
                if (landedFloor > currentFloor)
                {
                    currentFloor = landedFloor;
                }
            }
        }
    }

    void OnCollisionExit2D(Collision2D collision) // Detach player from platform when leaving
    {
        if (collision.gameObject.CompareTag("Platform"))
        {
            transform.SetParent(null);
        }
    }
}