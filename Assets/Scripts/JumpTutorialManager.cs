using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.EventSystems;
using TMPro;

public class JumpTutorialManager : MonoBehaviour
{
    public enum JumpType { Short, Long, Momentum, Edge, DirectionChange, Buffer, None }
    
    [Header("Tutorial State")]
    public JumpType currentTask = JumpType.None;
    private bool[] completedJumps = new bool[6];
    private bool isWaitingForLanding = false; 

    [Header("Safety Checks")]
    private Vector3 lastSafePosition;
    private bool hasSafePosition = false;

    [Header("UI Panels")]
    public RectTransform leftMenuPanel;
    public Vector2 menuOnScreenPos = new Vector2(0, 0);       
    public Vector2 menuOffScreenPos = new Vector2(-800, 0);   

    public float slideDuration = 0.25f; 
    
    [Header("UI Animation Elements")]
    [Tooltip("The 'Jump Tutorial' Title Text")]
    public RectTransform menuTitle; 
    public float buttonCascadeDelay = 0.08f; 
    public float slideSpeed = 0.2f;          
    public float offScreenX = -1500f;        

    private RectTransform[] sequenceElements;
    private Dictionary<RectTransform, float> targetXPositions = new Dictionary<RectTransform, float>();

    private bool isMenuOpen = false;
    private Coroutine slideCoroutine;

    [Header("Jump Type UI Button")]
    [SerializeField] private Button jumpPanel;

    [Header("Menu Buttons")]
    public Button[] jumpTypeButtons; 
    public GameObject[] buttonCheckmarks; 
    public Button continueBtn;
    public Button returnToGalleryBtn;

    [Header("Gameplay UI")]
    public TextMeshProUGUI descriptionText;
    
    [Header("Video Setup")]
    public VideoPlayer videoPlayer;
    public CanvasGroup videoCanvasGroup; // Add this line
    public float crossfadeDuration = 0.5f; // Add this line
    public VideoClip[] tutorialVideos;   
    public GameObject completionPanel;   

    [Header("Safety Checks")]
    private Vector3 initialSpawnPosition; // Add this variable to store the absolute starting position

    void OnEnable()
    {
        S1_PlayerController.OnShortJumpAchieved += HandleShortJump;
        S1_PlayerController.OnLongJumpAchieved += HandleLongJump;
        S1_PlayerController.OnMomentumJumpAchieved += HandleMomentumJump;
        S1_PlayerController.OnEdgeJumpAchieved += HandleEdgeJump;
        S1_PlayerController.OnDirectionChangeBoostAchieved += HandleDirectionBoost;
        S1_PlayerController.OnJumpBufferAchieved += HandleJumpBuffer;

        // Add this check
        if (videoPlayer != null)
            videoPlayer.loopPointReached += OnVideoEnd;
    }

    void OnDisable()
    {
        S1_PlayerController.OnShortJumpAchieved -= HandleShortJump;
        S1_PlayerController.OnLongJumpAchieved -= HandleLongJump;
        S1_PlayerController.OnMomentumJumpAchieved -= HandleMomentumJump;
        S1_PlayerController.OnEdgeJumpAchieved -= HandleEdgeJump;
        S1_PlayerController.OnDirectionChangeBoostAchieved -= HandleDirectionBoost;
        S1_PlayerController.OnJumpBufferAchieved -= HandleJumpBuffer;

        // Add this check
        if (videoPlayer != null)
            videoPlayer.loopPointReached -= OnVideoEnd;

        StopAllCoroutines();
    }

    // --- NAMED EVENT HANDLERS ---
    void HandleShortJump() { StartCoroutine(WaitToCompleteTutorial(JumpType.Short)); }
    void HandleLongJump() { StartCoroutine(WaitToCompleteTutorial(JumpType.Long)); }
    
    // For mid-air jumps, lock in the win immediately so they don't break if the player pauses mid-air
    void HandleMomentumJump() { 
        if (currentTask == JumpType.Momentum) InstantCompleteTask(JumpType.Momentum); 
        StartCoroutine(WaitToCompleteTutorial(JumpType.Momentum)); 
    }
    void HandleEdgeJump() { 
        if (currentTask == JumpType.Edge) InstantCompleteTask(JumpType.Edge);
        StartCoroutine(WaitToCompleteTutorial(JumpType.Edge)); 
    }
    void HandleDirectionBoost() { 
        if (currentTask == JumpType.DirectionChange) InstantCompleteTask(JumpType.DirectionChange);
        StartCoroutine(WaitToCompleteTutorial(JumpType.DirectionChange)); 
    }
    void HandleJumpBuffer() { 
        if (currentTask == JumpType.Buffer) InstantCompleteTask(JumpType.Buffer);
        StartCoroutine(WaitToCompleteTutorial(JumpType.Buffer)); 
    }

    void Start()
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(leftMenuPanel);

        List<RectTransform> elementsList = new List<RectTransform>();
        if (menuTitle != null) elementsList.Add(menuTitle);
        
        for (int i = 0; i < jumpTypeButtons.Length; i++)
        {
            if (jumpTypeButtons[i] != null)
            {
                elementsList.Add(jumpTypeButtons[i].GetComponent<RectTransform>());
                
                int index = i; 
                jumpTypeButtons[i].onClick.AddListener(() => SelectTutorial((JumpType)index));
            }
            if (i < buttonCheckmarks.Length && buttonCheckmarks[i] != null)
            {
                buttonCheckmarks[i].SetActive(false);
            }
        }

        if (continueBtn != null) 
        {
            elementsList.Add(continueBtn.GetComponent<RectTransform>());
            continueBtn.onClick.AddListener(CloseMenu);
        }
        
        if (returnToGalleryBtn != null) 
        {
            elementsList.Add(returnToGalleryBtn.GetComponent<RectTransform>());
            returnToGalleryBtn.onClick.AddListener(FinishTutorial);
        }

        sequenceElements = elementsList.ToArray();

        foreach (var el in sequenceElements)
        {
            targetXPositions[el] = el.localPosition.x;
        }

        VerticalLayoutGroup vlg = leftMenuPanel.GetComponentInChildren<VerticalLayoutGroup>();
        if (vlg != null) vlg.enabled = false;

        if (completionPanel != null) completionPanel.SetActive(false);

        // Capture initial spawn point as baseline safe position
        S1_PlayerController player = FindFirstObjectByType<S1_PlayerController>();
        if (player != null)
        {
            initialSpawnPosition = player.transform.position; // Store the very first spawn location
        }

        // ADD THIS HERE INSTEAD OF UPDATE:
        if (jumpPanel != null)
        {
            jumpPanel.onClick.AddListener(ToggleMenu);
        }

        leftMenuPanel.anchoredPosition = menuOffScreenPos;
        OpenMenu();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (isMenuOpen) CloseMenu();
            else OpenMenu();
        }

        // SAFETY: Track safe positions and intercept falls
        S1_PlayerController player = FindFirstObjectByType<S1_PlayerController>();
        if (player != null && !isMenuOpen)
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                float camBottom = cam.transform.position.y - cam.orthographicSize;
                
                // Intercept the fall BEFORE the player script triggers its own death/respawn
                if (player.transform.position.y < camBottom + 0.5f)
                {
                    ResetPlayerToSafeZone(player, cam);
                }
            }
        }
    }

    public void ToggleMenu()
    {
        if (isMenuOpen) 
        {
            CloseMenu();
        }
        else 
        {
            OpenMenu();
        }
    }

    private void ResetPlayerToSafeZone(S1_PlayerController player, Camera cam)
    {
        // Teleport the player back to the initial spawn position
        player.transform.position = initialSpawnPosition;
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        // Immediately reset the camera so it doesn't wander off
        CameraFollow camFollow = cam.GetComponent<CameraFollow>();
        if (camFollow != null)
        {
            Vector3 targetCamPos = cam.transform.position;
            
            // Set the exact starting Y position for the camera
            targetCamPos.y = -1.64697f; 
            
            camFollow.ResetCamera(targetCamPos);
        }
    }

    void SelectTutorial(JumpType type)
    {
        currentTask = type;
        int index = (int)type;
        
        if (index < tutorialVideos.Length && tutorialVideos[index] != null)
        {
            StartCoroutine(PlayVideoWithFade(index));
        }

        if (descriptionText != null)
        {
            descriptionText.gameObject.SetActive(true);
        }

        switch (currentTask)
        {
            case JumpType.Short: descriptionText.text = "Short Jump:\nTap Spacebar as fast as you can for a tiny hop."; break;
            case JumpType.Long: descriptionText.text = "Long Jump:\nPress and hold Spacebar until you start falling."; break;
            case JumpType.Momentum: descriptionText.text = "Momentum Jump:\nRun fast to either side, then jump to launch higher."; break;
            case JumpType.Edge: descriptionText.text = "Edge Jump:\nWalk so one foot is off the ledge, then jump."; break;
            case JumpType.DirectionChange: descriptionText.text = "Direction Boost:\nJump while running, then quickly press the opposite direction mid-air."; break;
            case JumpType.Buffer: descriptionText.text = "Jump Buffer:\nJump high, then press Spacebar right before you hit the ground."; break;
        }

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        // Before closing the menu, force the player to reset to the initial spawn position
        S1_PlayerController player = FindFirstObjectByType<S1_PlayerController>();
        if (player != null)
        {
            ResetPlayerToSafeZone(player, Camera.main);
        }

        CloseMenu(); 
    }

    private IEnumerator PlayVideoWithFade(int index)
    {
        videoPlayer.gameObject.SetActive(true);
        
        // Start transparent
        if (videoCanvasGroup != null) videoCanvasGroup.alpha = 0f;
        
        videoPlayer.clip = tutorialVideos[index];
        videoPlayer.Play();

        // Fade In
        if (videoCanvasGroup != null)
        {
            float elapsed = 0f;
            while (elapsed < crossfadeDuration)
            {
                videoCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / crossfadeDuration);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            videoCanvasGroup.alpha = 1f;
        }
    }

    private void OnVideoEnd(VideoPlayer vp)
    {
        // Triggered automatically when the video reaches the end
        StartCoroutine(EndVideoWithFade());
    }

    private IEnumerator EndVideoWithFade()
    {
        // Fade Out
        if (videoCanvasGroup != null)
        {
            float elapsed = 0f;
            while (elapsed < crossfadeDuration)
            {
                videoCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / crossfadeDuration);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            videoCanvasGroup.alpha = 0f;
        }

        // Turn off the object so the player can immediately try the jump
        videoPlayer.gameObject.SetActive(false);
    }

    IEnumerator WaitToCompleteTutorial(JumpType completedType)
    {
        if (currentTask != completedType) yield break;
        if (isWaitingForLanding) yield break; 

        isWaitingForLanding = true;

        S1_PlayerController player = FindFirstObjectByType<S1_PlayerController>();
        if (player != null)
        {
            yield return new WaitForSeconds(0.1f);

            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            while (rb.linearVelocity.y > 0.1f || !player.isGrounded)
            {
                yield return null; 
            }
        }

        isWaitingForLanding = false;
        CompleteCurrentTutorial();
    }

    void CompleteCurrentTutorial()
    {
        if (currentTask == JumpType.None) return;

        int index = (int)currentTask;
        completedJumps[index] = true;
        
        if (index < buttonCheckmarks.Length && buttonCheckmarks[index] != null)
        {
            buttonCheckmarks[index].SetActive(true);
        }

        bool allDone = true;
        for (int i = 0; i < completedJumps.Length; i++)
        {
            if (!completedJumps[i]) allDone = false;
        }

        if (allDone)
        {
            if (videoPlayer != null) videoPlayer.gameObject.SetActive(false);
            if (completionPanel != null) completionPanel.SetActive(true);
        }

        currentTask = JumpType.None; 
        OpenMenu(); 
    }

    public void OpenMenu()
    {
        if (isMenuOpen) return;
        isMenuOpen = true;
        
        Time.timeScale = 0f; 

        if (slideCoroutine != null) StopCoroutine(slideCoroutine);
        slideCoroutine = StartCoroutine(AnimateMenuIn());
    }

    public void CloseMenu()
    {
        if (!isMenuOpen) return;
        isMenuOpen = false;
        
        Time.timeScale = 1f; 

        if (slideCoroutine != null) StopCoroutine(slideCoroutine);
        
        leftMenuPanel.anchoredPosition = menuOffScreenPos;
    }

    IEnumerator AnimateMenuIn()
    {
        foreach (var el in sequenceElements)
        {
            SetLocalX(el, offScreenX);
        }

        float elapsedTime = 0;
        Vector2 startPos = menuOffScreenPos;
        leftMenuPanel.anchoredPosition = startPos;

        while (elapsedTime < slideDuration)
        {
            leftMenuPanel.anchoredPosition = Vector2.Lerp(startPos, menuOnScreenPos, elapsedTime / slideDuration);
            elapsedTime += Time.unscaledDeltaTime; 
            yield return null;
        }
        
        leftMenuPanel.anchoredPosition = menuOnScreenPos; 
        yield return new WaitForSecondsRealtime(0.05f);

        foreach (var el in sequenceElements)
        {
            StartCoroutine(SlideElementIn(el, targetXPositions[el]));
            yield return new WaitForSecondsRealtime(buttonCascadeDelay);
        }
    }

    void SetLocalX(RectTransform rt, float xPos)
    {
        Vector3 pos = rt.localPosition;
        pos.x = xPos;
        rt.localPosition = pos;
    }

    IEnumerator SlideElementIn(RectTransform element, float targetX)
    {
        float elapsedTime = 0;
        float startX = element.localPosition.x;

        while (elapsedTime < slideSpeed)
        {
            float newX = Mathf.Lerp(startX, targetX, elapsedTime / slideSpeed);
            SetLocalX(element, newX);
            
            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }
        SetLocalX(element, targetX);
    }

    void FinishTutorial()
    {
        Time.timeScale = 1f; 
        
        PlayerPrefs.SetInt("hasSeenJumpTutorial", 1);
        PlayerPrefs.Save();
        
        if (S18_LoadingManager.Instance != null)
        {
            S18_LoadingManager.Instance.LoadScenes("GYM_TutorialGallery");
        }
    }

    // Helper to lock in progress immediately for complex mid-air tasks
    private void InstantCompleteTask(JumpType type)
    {
        if (type == JumpType.None) return;
        completedJumps[(int)type] = true;
    }
}