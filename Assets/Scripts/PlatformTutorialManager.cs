using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; 

public class PlatformTutorialManager : MonoBehaviour
{
    [Header("Scene & Save Settings")]
    [Tooltip("The unique PlayerPrefs key for the platform tutorial.")]
    public string prefsKey = "hasSeenPlatformTutorial";

    [Tooltip("The name of the gallery scene to return to when done.")]
    public string gallerySceneName = "GYM_TutorialGallery";
    public string sceneToUnload = "GYM_PlatformTutorial"; // Matches your current scene name

    [Header("Navigation Elements")]
    public RectTransform contentPanel; 
    public float pageWidth = 1180f; // Adjusted to match your Inspector setting
    public float slideDuration = 0.3f;
    
    public Button leftArrow;
    public Button rightArrow;
    public Image[] pageDots;
    public Color activeDotColor = Color.white;
    public Color inactiveDotColor = Color.gray;

    [Header("Action Buttons")]
    public Button skipButton;
    public Button doneButton;

    private int currentPage = 0;
    private int totalPages;
    private bool isSliding = false;
    private bool hasSeenUIBefore;

    void Start()
    {
        totalPages = pageDots.Length;

        // Check if the player has seen this specific tutorial before
        hasSeenUIBefore = PlayerPrefs.GetInt(prefsKey, 0) == 1;

        if (leftArrow != null) leftArrow.onClick.AddListener(SlideLeft);
        if (rightArrow != null) rightArrow.onClick.AddListener(SlideRight);
        if (skipButton != null) skipButton.onClick.AddListener(FinishTutorial);
        if (doneButton != null) doneButton.onClick.AddListener(FinishTutorial);

        UpdateUI();
    }

    void SlideLeft()
    {
        if (currentPage > 0 && !isSliding)
        {
            currentPage--;
            StartCoroutine(SlideToPage(currentPage));
        }
    }

    void SlideRight()
    {
        if (currentPage < totalPages - 1 && !isSliding)
        {
            currentPage++;
            StartCoroutine(SlideToPage(currentPage));
        }
    }

    IEnumerator SlideToPage(int pageIndex)
    {
        isSliding = true;
        
        if (leftArrow != null) leftArrow.interactable = false;
        if (rightArrow != null) rightArrow.interactable = false;

        float targetX = -pageIndex * pageWidth;
        Vector2 startPos = contentPanel.anchoredPosition;
        Vector2 targetPos = new Vector2(targetX, startPos.y);

        float elapsedTime = 0;

        while (elapsedTime < slideDuration)
        {
            contentPanel.anchoredPosition = Vector2.Lerp(startPos, targetPos, elapsedTime / slideDuration);
            elapsedTime += Time.unscaledDeltaTime; 
            yield return null;
        }

        contentPanel.anchoredPosition = targetPos;
        isSliding = false;
        
        UpdateUI(); 
    }

    void FinishTutorial()
    {
        Debug.Log($"{gameObject.name} finished. Returning to {gallerySceneName}...");

        PlayerPrefs.SetInt(prefsKey, 1);
        PlayerPrefs.Save();

        Time.timeScale = 1f;

        // Transition back to the Gallery scene using the Loading Manager
        if (S18_LoadingManager.Instance != null)
        {
            S18_LoadingManager.Instance.LoadScenes(gallerySceneName);
        }
        else
        {
            // Fallback just in case the LoadingManager isn't in this scene
            SceneManager.LoadScene(gallerySceneName);
        }
    }

    void UpdateUI()
    {
        if (leftArrow != null)
        {
            leftArrow.gameObject.SetActive(currentPage > 0);
            leftArrow.interactable = true;
        }

        if (rightArrow != null)
        {
            rightArrow.gameObject.SetActive(currentPage < totalPages - 1);
            rightArrow.interactable = true;
        }

        for (int i = 0; i < pageDots.Length; i++)
        {
            if (pageDots[i] != null)
            {
                pageDots[i].color = (i == currentPage) ? activeDotColor : inactiveDotColor;
            }
        }

        bool isLastPage = (currentPage == totalPages - 1);
        if (doneButton != null) doneButton.gameObject.SetActive(isLastPage);
        if (skipButton != null) skipButton.gameObject.SetActive(!isLastPage && hasSeenUIBefore);
    }
}