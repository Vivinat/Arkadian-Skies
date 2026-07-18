using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Battle pause flow per the game rules: the player earns one pause charge every few
// seconds (BattlePauseController) and spends one to freeze the fight. Only while
// paused does the item bank unlock, so equipping is impossible outside a pause.
public class BattlePauseUI : MonoBehaviour
{
    public BattlePauseController pauseController;
    public Button pauseButton;
    public TMP_Text pauseButtonLabel;
    public GameObject pausedBanner;
    public Button resumeButton;
    public CanvasGroup itemBankGroup;

    void OnEnable()
    {
        pauseButton.onClick.AddListener(HandlePauseClicked);
        resumeButton.onClick.AddListener(HandleResumeClicked);
    }

    void OnDisable()
    {
        pauseButton.onClick.RemoveListener(HandlePauseClicked);
        resumeButton.onClick.RemoveListener(HandleResumeClicked);
    }

    void HandlePauseClicked() => pauseController.RequestPause();

    void HandleResumeClicked() => pauseController.ResumeBattle();

    void Update()
    {
        bool paused = pauseController.IsPaused;

        pauseButtonLabel.text = $"PAUSE ({pauseController.AvailablePauses})";
        pauseButton.interactable = !paused && pauseController.AvailablePauses > 0;

        if (pausedBanner.activeSelf != paused) pausedBanner.SetActive(paused);

        itemBankGroup.interactable = paused;
        itemBankGroup.blocksRaycasts = paused;
        itemBankGroup.alpha = paused ? 1f : 0.7f;
    }
}
