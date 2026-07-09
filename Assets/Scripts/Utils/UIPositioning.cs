using UnityEngine;

// Shared math for any floating UI element that needs to appear near a screen point (the cursor,
// a click position...) while staying fully inside its Canvas. Used by AbilityTooltip and
// CharacterInfoPanelController so this logic only has to be written once.
// Assumes "content" is a direct child of "containerRect" (the root Canvas), anchored at its center.
public static class UIPositioning
{
    public static bool ScreenPointToCanvasLocal(RectTransform canvasRect, Vector2 screenPoint, Camera uiCamera, out Vector2 localPoint)
    {
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, uiCamera, out localPoint);
    }

    public static Vector2 ClampToRect(Vector2 anchoredPos, RectTransform containerRect, RectTransform content)
    {
        Vector2 containerSize = containerRect.rect.size;
        Vector2 size = content.rect.size;
        Vector2 pivot = content.pivot;

        float halfW = containerSize.x * 0.5f;
        float halfH = containerSize.y * 0.5f;

        float minX = -halfW + pivot.x * size.x;
        float maxX = halfW - (1f - pivot.x) * size.x;
        float minY = -halfH + pivot.y * size.y;
        float maxY = halfH - (1f - pivot.y) * size.y;

        anchoredPos.x = Mathf.Clamp(anchoredPos.x, minX, maxX);
        anchoredPos.y = Mathf.Clamp(anchoredPos.y, minY, maxY);
        return anchoredPos;
    }

    public static Camera CameraFor(Canvas canvas)
    {
        return canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
    }
}