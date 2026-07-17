using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Shakes and flashes the spin button red when a spin fails (no gold), and shows the
// failure reason as a short-lived message. Lives on an always-active object (the Canvas)
// so coroutines survive the roulette window being toggled off.
public class RouletteFeedbackUI : MonoBehaviour
{
    public RouletteManager roulette;
    public RectTransform shakeTarget;
    public Graphic flashGraphic;
    public TMP_Text messageLabel;

    public float shakeDuration = 0.45f;
    public float shakeStrength = 9f;
    public float messageDuration = 1.6f;

    static readonly Color FlashColor = new Color(0.95f, 0.25f, 0.2f);

    Vector2 basePos;
    Color baseColor;
    Coroutine shakeRoutine;
    Coroutine messageRoutine;

    void Awake()
    {
        basePos = shakeTarget.anchoredPosition;
        baseColor = flashGraphic.color;
        messageLabel.gameObject.SetActive(false);
    }

    void OnEnable()
    {
        roulette.OnSpinFailed += HandleSpinFailed;
        roulette.OnAcquireFailed += HandleAcquireFailed;
    }

    void OnDisable()
    {
        roulette.OnSpinFailed -= HandleSpinFailed;
        roulette.OnAcquireFailed -= HandleAcquireFailed;
    }

    void HandleSpinFailed(string reason)
    {
        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
            shakeTarget.anchoredPosition = basePos;
            flashGraphic.color = baseColor;
        }
        shakeRoutine = StartCoroutine(Shake());
        ShowMessage(reason);
    }

    void HandleAcquireFailed(string reason) => ShowMessage(reason);

    void ShowMessage(string text)
    {
        if (messageRoutine != null) StopCoroutine(messageRoutine);
        messageRoutine = StartCoroutine(MessageRoutine(text));
    }

    IEnumerator Shake()
    {
        float t = 0f;
        while (t < shakeDuration)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / shakeDuration);
            float offset = Mathf.Sin(progress * 40f) * shakeStrength * (1f - progress);
            shakeTarget.anchoredPosition = basePos + new Vector2(offset, 0f);
            flashGraphic.color = Color.Lerp(FlashColor, baseColor, progress);
            yield return null;
        }

        shakeTarget.anchoredPosition = basePos;
        flashGraphic.color = baseColor;
        shakeRoutine = null;
    }

    IEnumerator MessageRoutine(string text)
    {
        messageLabel.text = text;
        messageLabel.gameObject.SetActive(true);

        Color c = messageLabel.color;
        float t = 0f;
        while (t < messageDuration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Clamp01((messageDuration - t) / 0.4f);
            messageLabel.color = c;
            yield return null;
        }

        c.a = 1f;
        messageLabel.color = c;
        messageLabel.gameObject.SetActive(false);
        messageRoutine = null;
    }
}
