
using UnityEngine;
using System.Collections;

public class S18_LandingFeedback : MonoBehaviour
{
    public SpriteRenderer feedbackRenderer;

    [Header("Landing Text Sprites")]
    public Sprite perfectSprite;
    public Sprite goodSprite;
    public Sprite badSprite;

    [Header("Display Settings")]
    public float displayDuration = 0.8f;

    private Coroutine hideCoroutine;

    void Awake()
    {
        if (feedbackRenderer == null)
            feedbackRenderer = GetComponent<SpriteRenderer>();

        feedbackRenderer.enabled = false;
    }

    public void ShowPerfect()
    {
        ShowFeedback(perfectSprite);
    }

    public void ShowGood()
    {
        ShowFeedback(goodSprite);
    }

    public void ShowBad()
    {
        ShowFeedback(badSprite);
    }

    private void ShowFeedback(Sprite sprite)
    {
        if (feedbackRenderer == null || sprite == null)
            return;

        if (hideCoroutine != null)
            StopCoroutine(hideCoroutine);

        feedbackRenderer.sprite = sprite;
        feedbackRenderer.enabled = true;

        hideCoroutine = StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(displayDuration);

        feedbackRenderer.enabled = false;
        hideCoroutine = null;
    }
}
