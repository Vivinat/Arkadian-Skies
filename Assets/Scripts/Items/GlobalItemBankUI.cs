using System.Collections;
using TMPro;
using UnityEngine;

// Owns a GLOBAL ITEMS panel. Tooltips always work; in battle (manager + pause set) the
// slots are only clickable during a pause - the manager then charges one pause per use
// and reports failures (e.g. out of charges) through the red message label.
public class GlobalItemBankUI : MonoBehaviour
{
    public PlayerGlobalItemBank bank;
    public GlobalItemManager manager;             // optional - null outside battle
    public BattlePauseController pauseController; // optional - null outside battle
    public GlobalItemSlotUI[] slots;
    public CanvasGroup panelGroup;
    public TMP_Text messageLabel;                 // optional

    bool InBattle => manager != null && pauseController != null;

    Coroutine messageRoutine;

    void Awake()
    {
        for (int i = 0; i < slots.Length; i++)
            slots[i].Initialize(this, i);
    }

    void OnEnable()
    {
        bank.OnChanged += RefreshAll;
        if (manager != null) manager.OnUseFailed += ShowError;
        if (messageLabel != null) messageLabel.text = "";
        RefreshAll();
    }

    void OnDisable()
    {
        bank.OnChanged -= RefreshAll;
        if (manager != null) manager.OnUseFailed -= ShowError;
    }

    void Update()
    {
        // raycasts stay on so tooltips are always readable; clicks are gated by interactable
        panelGroup.blocksRaycasts = true;
        bool clickable = !InBattle || pauseController.IsPaused;
        panelGroup.interactable = clickable;
        panelGroup.alpha = clickable ? 1f : 0.7f;
    }

    void RefreshAll()
    {
        for (int i = 0; i < slots.Length; i++)
            slots[i].SetStack(bank.GetItemAt(i), bank.GetCountAt(i));
    }

    public void OnSlotClicked(int index)
    {
        if (!InBattle) return;

        GlobalItemData item = bank.GetItemAt(index);
        if (item == null) return;

        if (manager.Use(item)) bank.ConsumeAt(index);
    }

    void ShowError(string reason)
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
