using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Battle pause flow: charges accrue over time and are the action currency - pausing,
// equipping, unequipping and using a global item each cost one. While paused the item
// bank unlocks; charge-denied warnings from any system show on the shared red label.
public class BattlePauseUI : MonoBehaviour
{
    public BattlePauseController pauseController;
    public Button pauseButton;
    public TMP_Text pauseButtonLabel;
    public TMP_Text chargesLabel; // optional: big pause-charge counter next to the button
    public GameObject pausedBanner;
    public Button resumeButton;
    public CanvasGroup itemBankGroup;
    public TMP_Text messageLabel; // optional: red "no charges" warnings

    Coroutine messageRoutine;

    void OnEnable()
    {
        pauseButton.onClick.AddListener(HandlePauseClicked);
        resumeButton.onClick.AddListener(HandleResumeClicked);
        pauseController.OnChargeDenied += ShowWarning;
        if (messageLabel != null) messageLabel.text = "";
    }

    void OnDisable()
    {
        pauseButton.onClick.RemoveListener(HandlePauseClicked);
        resumeButton.onClick.RemoveListener(HandleResumeClicked);
        pauseController.OnChargeDenied -= ShowWarning;
    }

    void HandlePauseClicked() => pauseController.RequestPause();

    void HandleResumeClicked() => pauseController.ResumeBattle();

    void Update()
    {
        bool paused = pauseController.IsPaused;

        if (pauseButtonLabel != null) pauseButtonLabel.text = "PAUSE";
        if (chargesLabel != null) chargesLabel.text = pauseController.AvailablePauses.ToString();
        pauseButton.interactable = !paused && pauseController.AvailablePauses > 0;

        if (pausedBanner.activeSelf != paused) pausedBanner.SetActive(paused);

        // raycasts stay on so equipment tooltips are always readable - actually equipping
        // is still pause-gated (and charge-priced) by EquippedItemSlotUI's pauseGate
        itemBankGroup.interactable = paused;
        itemBankGroup.blocksRaycasts = true;
        itemBankGroup.alpha = paused ? 1f : 0.7f;
    }

    void ShowWarning(string reason)
    {
        if (messageLabel == null) return;
        if (messageRoutine != null) StopCoroutine(messageRoutine);
        messageRoutine = StartCoroutine(MessageRoutine(reason));
    }

    IEnumerator MessageRoutine(string text)
    {
        messageLabel.text = text;
        Color c = messageLabel.color;
        c.a = 1f;
        messageLabel.color = c;

        yield return new WaitForSeconds(1.8f);

        float t = 0f;
        while (t < 0.4f)
        {
            t += Time.deltaTime;
            c.a = 1f - Mathf.Clamp01(t / 0.4f);
            messageLabel.color = c;
            yield return null;
        }
        messageLabel.text = "";
        c.a = 1f;
        messageLabel.color = c;
        messageRoutine = null;
    }
}
