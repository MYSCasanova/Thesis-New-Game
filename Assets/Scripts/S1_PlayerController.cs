using UnityEngine;

public class S1_PlayerController : MonoBehaviour
{
    [Header("Icy Tower Movement")]
    public float acceleration = 15f;
    public float deceleration = 10f; // how long before player stops
    public float maxSpeed = 10f;

    [Header("Jumping Mechanics")]
    public float jumpForce = 20f; // High force to fight heavy gravity
    public float shortJumpMultiplier = 0.3f; // Cuts height aggressively when releasing space
    public float edgeJumpMultiplier = 0.75f; // Lower jump on edges
    public float directionChangeBoost = 3f;  // Upward boost when switching directions in air
    public float momentumJumpBoost = 0.5f;   // Running faster makes you jump slightly higher
    public float bounceMultiplier = 1.5f; // Multiplier for jump height when on a bouncy platform
    
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

    [Header("Landing Penalties")]
    // DEBUGGING PUT BADLANDING PENALTY TO 0.5 AFTER
    public float badLandingPenalty = 1f; // Cuts max speed and acceleration in half 
    private float currentSpeedMultiplier = 1f; // 1 means normal speed

    private Rigidbody2D rb;
    private float moveInput;
    private bool isGrounded;
    private bool groundLeft;
    private bool groundRight;
    public bool isOnIcy = false; // Tracks if the player is currently on an icy platform
    public bool isOnBouncy = false; // Tracks if the player is currently on a bouncy platform

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
        float targetSpeed = moveInput * (maxSpeed * currentSpeedMultiplier);
        float speedDiff = targetSpeed - rb.linearVelocity.x;

        // Apply the multiplier to acceleration as well so they feel sluggish
        float activeAccel = acceleration * currentSpeedMultiplier;
        float activeDecel = deceleration * currentSpeedMultiplier;
        
        float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? activeAccel : activeDecel;
        
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

        if (isOnBouncy) // If the player is on a bouncy platform, increase jump height
        {
            currentJump *= bounceMultiplier; // Increase jump height by bounce multiplier if on a bouncy platform
        }

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
        if (collision.gameObject.CompareTag("Platform"))
        {
            S3_Platform landedPlatform = collision.gameObject.GetComponent<S3_Platform>();
            
            if (landedPlatform != null)
            {
                // 1. Precision Landing Math
                float playerX = transform.position.x;
                float platformX = collision.collider.bounds.center.x;
                float halfWidth = collision.collider.bounds.extents.x;
                
                // Gives a value from 0 (dead center) to 1 (extreme edge)
                float landingAccuracy = Mathf.Abs(playerX - platformX) / halfWidth;
                int landedFloor = landedPlatform.floorNumber;

                // Only score/judge the landing if it's a new, higher platform
                if (landedFloor > currentFloor)
                {
                    comboSystem.AddScore(2500); // Base Height Score

                    // 2. Evaluate Landing Quality
                    if (landingAccuracy <= 0.35f) 
                    {
                        Debug.Log("PERFECT Landing! Momentum restored.");
                        currentSpeedMultiplier = 1f; // REMOVES THE PENALTY
                        comboSystem.AddScore(800); 
                    } 
                    else if (landingAccuracy <= 0.75f) 
                    {
                        Debug.Log("GOOD Landing.");
                        // Good landings do nothing to the penalty. You must get a Perfect to cure it!
                    } 
                    else 
                    {
                        Debug.Log("BAD Landing (Edge)! Speed halved until Perfect.");
                        currentSpeedMultiplier = badLandingPenalty; // APPLIES THE PENALTY
                    }

                    // 3. Combo System Logic
                    int floorsSkipped = landedFloor - currentFloor;
                    if (floorsSkipped > 1) 
                    {
                        int comboPoints = floorsSkipped - 1;
                        comboSystem.AddCombo(comboPoints);
                        comboSystem.AddScore(1000 * comboPoints); // Combo Score
                    }

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
            isOnIcy = false; // Reset icy state when leaving the platform
        }
    }
}