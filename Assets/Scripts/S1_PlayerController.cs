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

    [Header("Platform Types")]
    public bool isOnIcy = false; // Tracks if the player is currently on an icy platform
    public bool isOnBouncy ; // Tracks if the player is currently on a bouncy platform

    private Rigidbody2D rb;
    private float moveInput;
    private bool isGrounded;
    private bool groundLeft;
    private bool groundRight;
    public bool isRespawning;

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

        // Checkpoint - checks if the player is below the screen
        float cameraBottomY = Camera.main.transform.position.y - (Camera.main.orthographicSize);
        if (!isRespawning && rb.linearVelocity.y < 0 && transform.position.y < cameraBottomY)
        {
            isRespawning = true;
            GetComponent<S7_HealthSystem>().LoseLife();
        }
    }

    void FixedUpdate()
    {
        // 5. Icy Tower Acceleration / Slippery Movement
        float targetSpeed = moveInput * (maxSpeed * currentSpeedMultiplier);
        float speedDiff = targetSpeed - rb.linearVelocity.x;

        //Icy Platforms have very low deceleration
        float activeAccel = acceleration * currentSpeedMultiplier;
        float activeDecel = deceleration * currentSpeedMultiplier;

        float accelRate; 

        if (Mathf.Abs(targetSpeed) > 0.01f) 
        { 
            accelRate = activeAccel; 
        } 
        else 
        { 
            accelRate = isOnIcy ? 1f : activeDecel; 
        
        }
        float movement =Mathf.Pow(Mathf.Abs(speedDiff) * accelRate, 0.9f) * Mathf.Sign(speedDiff);
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
        transform.SetParent(null);
    }
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Platform"))
        {
            // Only count it as a landing if the player hits the TOP of the platform
            bool landedOnTop = false;

            foreach (ContactPoint2D contact in collision.contacts)
            {
                if (contact.normal.y > 0.5f)
                {
                    landedOnTop = true;
                    break;
                }
            }

            if (!landedOnTop)
            {
                return;
            }

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
                int floorsSkipped = landedFloor - currentFloor;
                isOnIcy = landedPlatform.isIcy; // Update icy state based on the platform we landed on
                isOnBouncy = landedPlatform.isBouncy; // Update bouncy state based on the platform we landed on

                if (landedPlatform.direction != S3_Platform.MovementDirection.Static &&
                landedPlatform.direction != S3_Platform.MovementDirection.Falling) //Attach player to platform ONLY if it's moving
                {
                    transform.SetParent(collision.transform);
                }

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

                // Only score/judge the landing if it's a new, higher platform
                if (landedFloor > currentFloor)
                {
                    // 1. Precision Landing Math
                float playerX = transform.position.x;
                float platformX = collision.collider.bounds.center.x;
                float halfWidth = collision.collider.bounds.extents.x;
                
                // Gives a value from 0 (dead center) to 1 (extreme edge)
                float landingAccuracy = Mathf.Abs(playerX - platformX) / halfWidth;
                
                comboSystem.AddScore(2500);

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

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Checkpoint"))
        {
            S6_Checkpoint checkpoint = collision.gameObject.GetComponent<S6_Checkpoint>();
            if (checkpoint != null)
            {
                S6_Checkpoint.Instance.SetCheckpoint(checkpoint.transform.position);
            }
        }
    }
}