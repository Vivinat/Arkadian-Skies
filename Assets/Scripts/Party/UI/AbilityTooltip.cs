using UnityEngine;
using UnityEngine.UI;
using TMPro;

// One instance per scene, living on its own small panel directly under the root Canvas (start
// disabled, anchored at canvas center). Any TooltipTrigger just calls Show/Hide on it - it owns
// all the cursor-following logic.
public class AbilityTooltip : MonoBehaviour
{
    public static AbilityTooltip Instance { get; private set; }

    [Header("Refs")]
    public GameObject panelRoot;
    public TMP_Text titleLabel;
    public TMP_Text descriptionLabel;

    [Header("Offset from cursor, in canvas units")]
    public Vector2 cursorOffset = new Vector2(16f, -16f);

    RectTransform panelRect;
    RectTransform canvasRect;
    Camera uiCamera;
    bool isVisible;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        panelRect = (RectTransform)panelRoot.transform;
        Canvas canvas = panelRoot.GetComponentInParent<Canvas>().rootCanvas;
        canvasRect = (RectTransform)canvas.transform;
        uiCamera = UIPositioning.CameraFor(canvas);

        // the tooltip follows the cursor and can end up overlapping whatever triggered it - if any
        // of its own graphics were raycastable, that overlap would steal the hover and flicker
        // (enter -> show -> covers source -> exit -> hide -> exposed again -> enter -> ...)
        foreach (Graphic graphic in panelRoot.GetComponentsInChildren<Graphic>(true))
            graphic.raycastTarget = false;

        panelRoot.SetActive(false);
    }

    public void Show(string title, string description)
    {
        titleLabel.text = title;

        bool hasDescription = !string.IsNullOrEmpty(description);
        descriptionLabel.gameObject.SetActive(hasDescription);
        if (hasDescription) descriptionLabel.text = description;

        isVisible = true;
        panelRoot.SetActive(true);
        panelRect.SetAsLastSibling();
        UpdatePosition();
    }

    public void Hide()
    {
        isVisible = false;
        panelRoot.SetActive(false);
    }

    void Update()
    {
        if (isVisible) UpdatePosition();
    }

    void UpdatePosition()
    {
        if (!UIPositioning.ScreenPointToCanvasLocal(canvasRect, Input.mousePosition, uiCamera, out Vector2 localPoint))
            return;

        Vector2 anchored = localPoint + cursorOffset;
        panelRect.anchoredPosition = UIPositioning.ClampToRect(anchored, canvasRect, panelRect);
    }
}