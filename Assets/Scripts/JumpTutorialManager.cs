using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;

public class JumpTutorialManager : MonoBehaviour
{
    public enum JumpType { Short, Long, Momentum, Edge, DirectionChange, Buffer, None }
    
    [Header("Tutorial State")]
    public JumpType currentTask = JumpType.None;
    private bool[] completedJumps = new bool[6];

    [Header("UI Panels")]
    public RectTransform leftMenuPanel;
    public Vector2 menuOnScreenPos = new Vector2(0, 0);       // Adjust based on your Canvas size
    public Vector2 menuOffScreenPos = new Vector2(-800, 0);   // Adjust so it hides off-screen to the left
    public float slideDuration = 0.25f;
    private bool isMenuOpen = false;
    private Coroutine slideCoroutine;

    [Header("Menu Buttons")]
    public Button[] jumpTypeButtons; // Assign in order: Short, Long, Momentum, Edge, Direction, Buffer
    public GameObject[] buttonCheckmarks; // Checkmark images inside the buttons to show completion
    public Button continueBtn;
    public Button returnToGalleryBtn;

    [Header("Gameplay UI")]
    public TextMeshProUGUI descriptionText;
    
    [Header("Video Setup")]
    public VideoPlayer videoPlayer;
    public VideoClip[] tutorialVideos;   // Assign in exact same order as JumpType enum
    public GameObject completionPanel;   // Panel to show when all are done

    void OnEnable()
    {
        S1_PlayerController.OnShortJumpAchieved += CheckShortJump;
        S1_PlayerController.OnLongJumpAchieved += CheckLongJump;
        S1_PlayerController.OnMomentumJumpAchieved += CheckMomentumJump;
        S1_PlayerController.OnEdgeJumpAchieved += CheckEdgeJump;
        S1_PlayerController.OnDirectionChangeBoostAchieved += CheckDirectionChangeBoost;
        S1_PlayerController.OnJumpBufferAchieved += CheckJumpBuffer;
    }

    void OnDisable()
    {
        S1_PlayerController.OnShortJumpAchieved -= CheckShortJump;
        S1_PlayerController.OnLongJumpAchieved -= CheckLongJump;
        S1_PlayerController.OnMomentumJumpAchieved -= CheckMomentumJump;
        S1_PlayerController.OnEdgeJumpAchieved -= CheckEdgeJump;
        S1_PlayerController.OnDirectionChangeBoostAchieved -= CheckDirectionChangeBoost;
        S1_PlayerController.OnJumpBufferAchieved -= CheckJumpBuffer;
    }

    void Start()
    {
        // Hook up the jump type buttons to load specific tutorials
        for (int i = 0; i < jumpTypeButtons.Length; i++)
        {
            int index = i; // Local copy for the lambda closure
            if (jumpTypeButtons[i] != null)
            {
                jumpTypeButtons[i].onClick.AddListener(() => SelectTutorial((JumpType)index));
            }
            if (i < buttonCheckmarks.Length && buttonCheckmarks[i] != null)
            {
                buttonCheckmarks[i].SetActive(false);
            }
        }

        if (continueBtn != null) continueBtn.onClick.AddListener(CloseMenu);
        if (returnToGalleryBtn != null) returnToGalleryBtn.onClick.AddListener(FinishTutorial);
        if (completionPanel != null) completionPanel.SetActive(false);

        // Start with menu off-screen, then slide it in immediately to pick the first tutorial
        leftMenuPanel.anchoredPosition = menuOffScreenPos;
        OpenMenu();
    }

    void Update()
    {
        // Toggle the menu when pressing Tab
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (isMenuOpen) CloseMenu();
            else OpenMenu();
        }
    }

    void SelectTutorial(JumpType type)
    {
        currentTask = type;
        int index = (int)type;
        
        if (index < tutorialVideos.Length && tutorialVideos[index] != null)
        {
            videoPlayer.gameObject.SetActive(true);
            videoPlayer.clip = tutorialVideos[index];
            videoPlayer.Play();
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

        // Auto-close menu so the player can attempt the task
        CloseMenu(); 
    }

    // --- EVENT LISTENERS ---
    void CheckShortJump() { if (currentTask == JumpType.Short) CompleteCurrentTutorial(); }
    void CheckLongJump() { if (currentTask == JumpType.Long) CompleteCurrentTutorial(); }
    void CheckMomentumJump() { if (currentTask == JumpType.Momentum) CompleteCurrentTutorial(); }
    void CheckEdgeJump() { if (currentTask == JumpType.Edge) CompleteCurrentTutorial(); }
    void CheckDirectionChangeBoost() { if (currentTask == JumpType.DirectionChange) CompleteCurrentTutorial(); }
    void CheckJumpBuffer() { if (currentTask == JumpType.Buffer) CompleteCurrentTutorial(); }

    void CompleteCurrentTutorial()
    {
        if (currentTask == JumpType.None) return;

        int index = (int)currentTask;
        completedJumps[index] = true;
        
        // Show the checkmark next to the button
        if (index < buttonCheckmarks.Length && buttonCheckmarks[index] != null)
        {
            buttonCheckmarks[index].SetActive(true);
        }

        // Check if all tutorials are done
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

        // Clear the current task so the game doesn't fire events randomly while menu is open
        currentTask = JumpType.None; 
        
        // Slide menu back in automatically for the player to select the next lesson
        OpenMenu(); 
    }

    public void OpenMenu()
    {
        if (isMenuOpen) return;
        isMenuOpen = true;
        
        // Freeze gameplay
        Time.timeScale = 0f; 

        if (slideCoroutine != null) StopCoroutine(slideCoroutine);
        slideCoroutine = StartCoroutine(SlidePanel(menuOnScreenPos));
    }

    public void CloseMenu()
    {
        if (!isMenuOpen) return;
        isMenuOpen = false;
        
        // Resume gameplay
        Time.timeScale = 1f; 

        if (slideCoroutine != null) StopCoroutine(slideCoroutine);
        slideCoroutine = StartCoroutine(SlidePanel(menuOffScreenPos));
    }

    IEnumerator SlidePanel(Vector2 targetPos)
    {
        Vector2 startPos = leftMenuPanel.anchoredPosition;
        float elapsedTime = 0;

        while (elapsedTime < slideDuration)
        {
            leftMenuPanel.anchoredPosition = Vector2.Lerp(startPos, targetPos, elapsedTime / slideDuration);
            // CRITICAL: Use unscaledDeltaTime because Time.timeScale is 0 while menu is open
            elapsedTime += Time.unscaledDeltaTime; 
            yield return null;
        }

        leftMenuPanel.anchoredPosition = targetPos;
    }

    void FinishTutorial()
    {
        // Unfreeze time before leaving scene
        Time.timeScale = 1f; 
        
        PlayerPrefs.SetInt("hasSeenJumpTutorial", 1);
        PlayerPrefs.Save();
        
        if (S18_LoadingManager.Instance != null)
        {
            S18_LoadingManager.Instance.LoadScenes("GYM_TutorialGallery");
        }
    }
}

//GEMINI: https://gemini.google.com/app/1cf5f149de99daad