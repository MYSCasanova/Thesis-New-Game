using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; 

public class TutorialGalleryManager : MonoBehaviour
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

    [Header("Page Buttons")]
    public Button basicTutorialBtn;
    public Button jumpTypesBtn;
    public Button platformTypesBtn;

    private int currentPage = 0;
    private int totalPages = 3;
    private bool isSliding = false;

    void Start()
    {
        // Hook up the navigation arrows
        leftArrow.onClick.AddListener(SlideLeft);
        rightArrow.onClick.AddListener(SlideRight);

        // Hook up the center image clicks
        basicTutorialBtn.onClick.AddListener(OnBasicTutorialClicked);
        
        // // Use lambda expressions to pass the specific button and label
        // jumpTypesBtn.onClick.AddListener(() => OnUnavailableClicked(jumpTypesBtn, jumpComingSoonLabel));
        // platformTypesBtn.onClick.AddListener(() => OnUnavailableClicked(platformTypesBtn, platformComingSoonLabel));

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
        UpdateUI(); // Immediately hide arrows/update dots so the user can't spam click

        float targetX = -pageIndex * pageWidth;
        Vector2 startPos = contentPanel.anchoredPosition;
        Vector2 targetPos = new Vector2(targetX, startPos.y);

        float elapsedTime = 0;

        while (elapsedTime < slideDuration)
        {
            // Smoothly lerp the position for the slide transition
            contentPanel.anchoredPosition = Vector2.Lerp(startPos, targetPos, elapsedTime / slideDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        contentPanel.anchoredPosition = targetPos;
        isSliding = false;
    }

    void UpdateUI()
    {
        // The first page doesn't have a left button, the last doesn't have a right button
        leftArrow.gameObject.SetActive(currentPage > 0);
        rightArrow.gameObject.SetActive(currentPage < totalPages - 1);

        // Update the dot indicators below the TV
        for (int i = 0; i < pageDots.Length; i++)
        {
            if (pageDots[i] != null)
            {
                pageDots[i].color = (i == currentPage) ? activeDotColor : inactiveDotColor;
            }
        }
    }

    void OnBasicTutorialClicked()
    {
        Debug.Log("Loading Scene: Basic Tutorial...");
        
        // Indicate that we are entering from the Tutorial Gallery
        SceneTransition.isFromGallery = true;

        SceneManager.LoadScene("PersistentGameplay");
        SceneManager.LoadScene("GYM_TutorialLevel", LoadSceneMode.Additive);
    }
    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene("GYM_MainMenu", LoadSceneMode.Single);
    }
}