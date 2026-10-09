using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class S9_TutorialManager : MonoBehaviour
{
    [System.Serializable]
    public class TutorialTip
    {
        public int triggerFloor;           // Tip is queued once the player reaches this floor or higher
        [TextArea] public string message;
        public float displayTime = 3f;     // How long it stays on screen
    }

    public TMP_Text instructionText;

    // IMPORTANT: keep this list sorted by triggerFloor (lowest first)
    public TutorialTip[] tips = new TutorialTip[]
    {
        new TutorialTip { triggerFloor = 3,  message = "The camera continuously moves upward, so keep climbing!" },
        new TutorialTip { triggerFloor = 6,  message = "Keep an eye out for different platforms!" },
        new TutorialTip { triggerFloor = 13, message = "Tap SPACE quickly to make a short jump" },
        new TutorialTip { triggerFloor = 14, message = "Hold SPACE to jump higher" },
        new TutorialTip { triggerFloor = 15, message = "Move before jumping for more height" },
        new TutorialTip { triggerFloor = 17, message = "Aim for the center of the platform for a safe landing!" },
        new TutorialTip { triggerFloor = 19, message = "Keep landing to build your combo" },
        new TutorialTip { triggerFloor = 21, message = "And lastly, watch out for hazards!" },
        new TutorialTip { triggerFloor = 25, message = "You're all set. Good Luck!", displayTime = 5f },
    };

    private int nextTipIndex = 0;                       // Next tip that hasn't been queued yet
    private readonly Queue<TutorialTip> tipQueue = new Queue<TutorialTip>();
    private Coroutine playRoutine;

    // Intro steps
    private bool movedYet = false;
    private bool introDone = false;

    void Start()
    {
        instructionText.gameObject.SetActive(true);
        instructionText.text = "Use A/D to move";
    }

    void Update()
    {
        if (introDone) return;

        if (!movedYet && (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.D)))
        {
            movedYet = true;
            instructionText.text = "Press SPACE to jump";
        }
        else if (Input.GetKeyDown(KeyCode.Space))
        {
            // Jumping also finishes the intro if they skipped the move step
            introDone = true;
            instructionText.gameObject.SetActive(false);
        }
    }

    public void PlatformLanded(int floorNumber)
    {
        // Queue every tip the player has reached but we haven't queued yet.
        // nextTipIndex only moves forward, so falling never repeats a tip.
        while (nextTipIndex < tips.Length && tips[nextTipIndex].triggerFloor <= floorNumber)
        {
            tipQueue.Enqueue(tips[nextTipIndex]);
            nextTipIndex++;
        }

        if (playRoutine == null && tipQueue.Count > 0)
            playRoutine = StartCoroutine(PlayQueue());
    }

    IEnumerator PlayQueue()
    {
        // Don't show floor tips on top of the intro text
        while (!introDone) yield return null;

        while (tipQueue.Count > 0)
        {
            TutorialTip tip = tipQueue.Dequeue();
            instructionText.gameObject.SetActive(true);
            instructionText.text = tip.message;
            yield return new WaitForSeconds(tip.displayTime);
        }

        instructionText.gameObject.SetActive(false);
        playRoutine = null;
    }
}