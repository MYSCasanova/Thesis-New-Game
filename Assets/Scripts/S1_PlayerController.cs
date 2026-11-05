using UnityEngine;
using UnityEngine.SceneManagement;
using System;


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
    public bool isAutoBouncing = false; 
    public float jumpBufferTime = 0.2f; 
    private float jumpBufferCounter;
    
    // --- ADD THESE TWO LINES ---
    private float lastJumpTime;
    private float jumpCooldown = 0.1f;

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
    public float badLandingPenalty = 0.5f; // Cuts max speed and acceleration in half
    private float currentSpeedMultiplier = 1f; // 1 means normal speed

    [Header("Platform Types")]
    public bool isOnIcy = false; // Tracks if the player is currently on an icy platform
    public bool isOnBouncy ; // Tracks if the player is currently on a bouncy platform

    [Header("Player Animation")]
    public Animator animator;
    public SpriteRenderer spriteRenderer;
    public float animationMovementThreshold = 0.1f;

    [Header("Landing Feedback")]
    public S18_LandingFeedback landingFeedback;

    private Rigidbody2D rb;
    private float moveInput;
    private bool isGrounded;
    private bool groundLeft;
    private bool groundRight;
    private bool isRespawning;
    private float fallDetectionDelay;

    [Header("Tutorial Events")]
    public static Action OnShortJumpAchieved;
    public static Action OnLongJumpAchieved;
    public static Action OnMomentumJumpAchieved;
    public static Action OnEdgeJumpAchieved;
    public static Action OnDirectionChangeBoostAchieved;
    public static Action OnJumpBufferAchieved;

    private bool isCurrentJumpShort = false;
    private bool didJump = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        moveInput = Input.GetAxisRaw("Horizontal");

        // 1. Check if grounded (Left foot, Right foot, or both)
        Collider2D leftHit = Physics2D.OverlapCircle(leftFoot.position, groundCheckRadius, groundLayer);
        Collider2D rightHit = Physics2D.OverlapCircle(rightFoot.position, groundCheckRadius, groundLayer);

        groundLeft = leftHit != null;
        groundRight = rightHit != null;
        isGrounded = groundLeft || groundRight;

        // Activate the camera from the ground check, so it works even if we jump before a physical collision registers
        Collider2D groundHit = leftHit != null ? leftHit : rightHit;
        if (groundHit != null)
        {
            S3_Platform platform = groundHit.GetComponent<S3_Platform>();
            if (platform != null && platform.floorNumber >= 3)
            {
                CameraFollow cam = Camera.main.GetComponent<CameraFollow>();
                if (cam != null) cam.Activate();
            }
        }

        // 2. Jump Buffering / Auto Jump Queueing
        if (Input.GetButtonDown("Jump")) {
            jumpBufferCounter = jumpBufferTime;
        } else {
            jumpBufferCounter -= Time.deltaTime;
        }

                // 3. Execute Jump (Manual or Auto-bounce) with COOLDOWN
        if (isGrounded && (jumpBufferCounter > 0f || isAutoBouncing) && Time.time > lastJumpTime + jumpCooldown)
        {
            // NEW: If they buffered the jump (pressed before hitting the ground)
            if (jumpBufferCounter > 0f && jumpBufferCounter < jumpBufferTime) 
            {
                OnJumpBufferAchieved?.Invoke(); 
            }

            PerformJump();
            jumpBufferCounter = 0f;
            lastJumpTime = Time.time; 
        }

        // 4. Short Jump (Release Spacebar early)
        if (Input.GetButtonUp("Jump") && rb.linearVelocity.y > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * shortJumpMultiplier);
            
            // NEW: Broadcast short jump
            if (!isCurrentJumpShort)
            {
                isCurrentJumpShort = true;
                OnShortJumpAchieved?.Invoke();
            }
        }

        // NEW: Detect Long Jump (Reached apex, actually jumped, and didn't short jump)
        if (didJump && rb.linearVelocity.y <= 0 && !isCurrentJumpShort)
        {
            OnLongJumpAchieved?.Invoke();
            didJump = false; // Reset so it only fires once per jump
        }

        // NEW: Reset didJump if they land without jumping (e.g. falling off a ledge)
        if (isGrounded) didJump = false;

        // If the player is below the camera's bottom edge, trigger respawn logic
        float cameraBottomY = Camera.main.transform.position.y - (Camera.main.orthographicSize);
        if (fallDetectionDelay > 0f)
        {
            fallDetectionDelay -= Time.deltaTime;
        }
        else if (!isRespawning && transform.position.y < cameraBottomY)
        {
            isRespawning = true;

            // Increment error count in S17_GameplayData
            S17_GameplayData gameplayData = FindFirstObjectByType<S17_GameplayData>();

            if(gameplayData != null)
            {
                gameplayData.errorCount++;
                Debug.Log($"Error Count: {gameplayData.errorCount}");
            }

            // ---> MERGED FROM BETA: Safety null check for HealthSystem
            S7_HealthSystem healthSystem = GetComponent<S7_HealthSystem>();

            if (healthSystem != null)
            {
                healthSystem.LoseLife(1);
            }
            else
            {
                Debug.LogError("Player is missing S7_HealthSystem!");
                isRespawning = false;
            }
        }

        UpdateAnimation();
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
            accelRate = isOnIcy ? 0.2f : activeDecel;
        }
        float movement =Mathf.Pow(Mathf.Abs(speedDiff) * accelRate, 0.9f) * Mathf.Sign(speedDiff);
        rb.AddForce(movement * Vector2.right);

        // 6. Direction Change Boost in Air
        if (!isGrounded && moveInput != 0)
        {
            if ((moveInput > 0 && rb.linearVelocity.x < -0.1f) || (moveInput < 0 && rb.linearVelocity.x > 0.1f))
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y + directionChangeBoost * Time.fixedDeltaTime);
                
                // NEW: Broadcast direction change
                OnDirectionChangeBoostAchieved?.Invoke();
            }
        }
    }

    private void UpdateAnimation()
    {
        if (animator == null || spriteRenderer == null || rb == null)
            return;

        float horizontalSpeed = rb.linearVelocity.x;

        // Walk while actually moving; idle when nearly stopped
        bool isWalking = Mathf.Abs(horizontalSpeed) > animationMovementThreshold;
        animator.SetBool("IsWalking", isWalking);

        // Face the direction of movement
        if (horizontalSpeed > animationMovementThreshold)
        {
            spriteRenderer.flipX = false;
        }
        else if (horizontalSpeed < -animationMovementThreshold)
        {
            spriteRenderer.flipX = true;
        }
    }

        private void PerformJump()
    {   
        didJump = true;               // NEW: Mark that a jump started
        isCurrentJumpShort = false;   // NEW: Reset short jump status

        float currentJump = jumpForce;

        if (isOnBouncy && !isAutoBouncing) 
        {
            currentJump *= bounceMultiplier; 
            S16_AudioManager.instance.PlaySFX("Trampoline"); 
        }
        else 
        {
            S16_AudioManager.instance.PlaySFX("Jump"); 
        }

        // 7. Momentum Boost (Running fast = higher jump)
        float speedBoost = Mathf.Abs(rb.linearVelocity.x) * momentumJumpBoost;
        currentJump += speedBoost;
        
        // NEW: If speed boost is high enough, it counts as a momentum jump
        if (speedBoost > 1f) 
        {
            OnMomentumJumpAchieved?.Invoke();
        }

        // 8. Edge Jump Logic (Only one foot is on the ground)
        if (groundLeft != groundRight)
        {
            currentJump *= edgeJumpMultiplier; 
            
            // NEW: Broadcast edge jump
            OnEdgeJumpAchieved?.Invoke(); 
        }
        
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, currentJump);
        MovePlayerToPersistentScene();
    }



    void OnCollisionEnter2D(Collision2D collision)
    {
        // Check if the object we landed on has the "Platform" tag
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
                int landedFloor = landedPlatform.floorNumber;
                int floorsSkipped = landedFloor - currentFloor;
                isOnIcy = landedPlatform.isIcy; // Update icy state based on the platform we landed on
                isOnBouncy = landedPlatform.isBouncy; // Update bouncy state based on the platform we landed on

                // Start the completion timer when landing on floor 1
                if (landedFloor == 1)
                {
                    S17_GameplayData gameplayData = FindFirstObjectByType<S17_GameplayData>();

                    if (gameplayData != null)
                    {
                        gameplayData.StartGameTimer();
                    }
                }

                if (landedPlatform.direction != S3_Platform.MovementDirection.Static &&
                landedPlatform.direction != S3_Platform.MovementDirection.Falling) //Attach player to platform ONLY if it's moving
                {
                    transform.SetParent(collision.transform);
                }

                if (landedFloor >= 3) //camera scrolls up when player jumps on platform 3
                {
                    Camera.main.GetComponent<CameraFollow>().Activate();
                }

                // Update our current floor so we can't farm combos by jumping in place
                if (landedFloor > currentFloor)
                {
                    // 1. Precision Landing Math
                float playerX = transform.position.x;
                float platformX = collision.collider.bounds.center.x;
                float halfWidth = collision.collider.bounds.extents.x;
                
                // Gives a value from 0 (dead center) to 1 (extreme edge)
                float landingAccuracy = Mathf.Abs(playerX - platformX) / halfWidth;
                
                if (comboSystem != null)
                    {
                        comboSystem.AddScore(2500);
                    }
                    
                if (landingAccuracy <= 0.35f)
                    {
                        Debug.Log("PERFECT Landing! Momentum restored.");
                        currentSpeedMultiplier = 1f; // REMOVES THE PENALTY

                        if (landingFeedback != null)
                        {
                            landingFeedback.ShowPerfect();
                        }

                        if (comboSystem != null)
                        {
                            comboSystem.AddScore(800);
                        }
                    }
                    else if (landingAccuracy <= 0.75f)
                    {
                        Debug.Log("GOOD Landing.");
                        // Good landings do nothing to the penalty. You must get a Perfect to cure it!

                        if (landingFeedback != null)
                        {
                            landingFeedback.ShowGood();
                        }
                    }
                    else
                    {
                        Debug.Log("BAD Landing (Edge)! Speed halved until Perfect.");
                        currentSpeedMultiplier = badLandingPenalty; // APPLIES THE PENALTY

                        if (landingFeedback != null)
                        {
                            landingFeedback.ShowBad();
                        }

                        // Increment error count in S17_GameplayData
                        S17_GameplayData gameplayData = FindFirstObjectByType<S17_GameplayData>();

                        if(gameplayData != null)
                        {
                            gameplayData.errorCount++;
                            Debug.Log($"Error Count: {gameplayData.errorCount}");
                        }
                    }

                    // 3. Combo System Logic
                    if (floorsSkipped > 1)
                    {
                        int comboPoints = floorsSkipped - 1;

                        if (comboSystem != null)
                        {
                            comboSystem.AddCombo(comboPoints);
                        comboSystem.AddScore(1000 * comboPoints); // Combo Score
                        }
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
            // Safety check: Do not execute if the player or the platform is being deactivated
            if (!gameObject.activeInHierarchy || !collision.gameObject.activeInHierarchy)
            {
                return;
            }

            MovePlayerToPersistentScene();
            isOnIcy = false; // Reset icy state when leaving the platform
            isOnBouncy = false;
        }
    }


    public void ResetMultiplier()
    {
        currentSpeedMultiplier = 1f;
    }

    public void ResetRun()
    {
        currentFloor = 0;
        currentSpeedMultiplier = 1f;
        isRespawning = false;
        jumpBufferCounter = 0f;
        isOnIcy = false;
        isOnBouncy = false;
        fallDetectionDelay = 1f;

        if (comboSystem != null)
        {
            comboSystem.currentCombo = 0;
            comboSystem.totalScore = 0;
            comboSystem.scoreMultiplier = 1;
        }
    }

    public void StopRespawning()
    {
        isRespawning = false;
    }

    private void MovePlayerToPersistentScene()
    {
        // Extra safety check before changing hierarchy
        if (!gameObject.activeInHierarchy) return;

        if (transform.parent != null)
        {
            transform.SetParent(null);
        }
        
        Scene persistentScene = SceneManager.GetSceneByName("PersistentGameplay");

        if (persistentScene.IsValid() && persistentScene.isLoaded)
        {
            SceneManager.MoveGameObjectToScene(gameObject, persistentScene);
        }
    }
}