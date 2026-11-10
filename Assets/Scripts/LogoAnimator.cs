using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
[RequireComponent(typeof(CanvasGroup))]
public class LogoAnimator : MonoBehaviour
{
    [Header("Animation Settings")]
    public float revealDuration = 1.5f;
    public float bounceDuration = 5.0f;
    public float bounceHeight = 15f;
    public float bounceSpeed = 2f;

    private Image logoImage;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Vector2 startPosition;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        logoImage = GetComponent<Image>();
        canvasGroup = GetComponent<CanvasGroup>();
        
        startPosition = rectTransform.anchoredPosition;

        // Automatically configure the Image for a left-to-right wipe
        logoImage.type = Image.Type.Filled;
        logoImage.fillMethod = Image.FillMethod.Horizontal;
        logoImage.fillOrigin = (int)Image.OriginHorizontal.Left;

        StartCoroutine(AnimationSequence());
    }

    IEnumerator AnimationSequence()
    {
        while (true) // Infinite loop
        {
            // 1. Reset for the start of the sequence
            logoImage.fillAmount = 0f;
            canvasGroup.alpha = 0f;
            rectTransform.anchoredPosition = startPosition;

            float elapsed = 0f;

            // 2. Fade in and wipe left to right (Flag effect)
            while (elapsed < revealDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / revealDuration;
                
                logoImage.fillAmount = progress;
                canvasGroup.alpha = progress; // Fades opacity alongside the wipe
                
                yield return null;
            }
            
            logoImage.fillAmount = 1f;
            canvasGroup.alpha = 1f;

            // 3. Bounce slowly for 5 seconds
            elapsed = 0f;
            while (elapsed < bounceDuration)
            {
                elapsed += Time.deltaTime;
                
                // Sine wave calculation for smooth up and down movement
                float newY = startPosition.y + Mathf.Sin(elapsed * bounceSpeed) * bounceHeight;
                rectTransform.anchoredPosition = new Vector2(startPosition.x, newY);
                
                yield return null;
            }
        }
    }
}