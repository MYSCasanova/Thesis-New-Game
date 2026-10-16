using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; 

public class BasicUITutorialManager : MonoBehaviour
{
    [Header("Navigation Elements")]
    public RectTransform contentPanel; 
    public float pageWidth = 800f; // Adjust this to the width of your page/TV image
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
        // Set total pages based on the number of dots assigned
        totalPages = pageDots.Length;

        // Check if the player has seen this tutorial before
        hasSeenUIBefore = PlayerPrefs.GetInt("hasSeenBasicUI", 0) == 1;

        // Hook up the buttons
        leftArrow.onClick.AddListener(SlideLeft);
        rightArrow.onClick.AddListener(SlideRight);
        skipButton.onClick.AddListener(FinishBasicUI);
        doneButton.onClick.AddListener(FinishBasicUI);

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
        
        leftArrow.interactable = false;
        rightArrow.interactable = false;

        float targetX = -pageIndex * pageWidth;
        Vector2 startPos = contentPanel.anchoredPosition;
        Vector2 targetPos = new Vector2(targetX, startPos.y);

        float elapsedTime = 0;

        while (elapsedTime < slideDuration)
        {
            contentPanel.anchoredPosition = Vector2.Lerp(startPos, targetPos, elapsedTime / slideDuration);
            // CRITICAL: Use unscaledDeltaTime because Time.timeScale is currently 0
            elapsedTime += Time.unscaledDeltaTime; 
            yield return null;
        }

        contentPanel.anchoredPosition = targetPos;
        isSliding = false;
        
        UpdateUI(); 
    }

    void FinishBasicUI()
    {
        Debug.Log("Basic UI finished or skipped. Unfreezing GYM_TutorialLevel...");

        // Save that the player has now seen this Basic UI tutorial
        PlayerPrefs.SetInt("hasSeenBasicUI", 1);
        PlayerPrefs.Save();

        // Unfreeze the game so the Tutorial Level can play
        Time.timeScale = 1f;

        // Set the Tutorial Level as the active scene so gameplay lighting/settings apply correctly
        Scene tutorialScene = SceneManager.GetSceneByName("GYM_TutorialLevel");
        if (tutorialScene.isLoaded)
        {
            SceneManager.SetActiveScene(tutorialScene);
        }

        // Unload this Basic UI scene so it removes itself from the screen
        SceneManager.UnloadSceneAsync("GYM_BasicUI");
    }

    void UpdateUI()
    {
        // Arrow visibility
        leftArrow.gameObject.SetActive(currentPage > 0);
        rightArrow.gameObject.SetActive(currentPage < totalPages - 1);

        // CRITICAL: Re-enable button clicks now that sliding is done
        leftArrow.interactable = true;
        rightArrow.interactable = true;

        // Update the dot indicators
        for (int i = 0; i < pageDots.Length; i++)
        {
            if (pageDots[i] != null)
            {
                pageDots[i].color = (i == currentPage) ? activeDotColor : inactiveDotColor;
            }
        }

        // Check if we are on the last slide
        bool isLastPage = (currentPage == totalPages - 1);

        // Show Done button ONLY on the last page
        doneButton.gameObject.SetActive(isLastPage);

        // Show Skip button ONLY if it's NOT the last page AND the player has seen it before
        skipButton.gameObject.SetActive(!isLastPage && hasSeenUIBefore);
    }
}