using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class SafeAreaFitter : MonoBehaviour
{
    private RectTransform targetRectTransform;
    private Rect previousSafeArea;
    private int previousScreenWidth;
    private int previousScreenHeight;

    private void OnEnable()
    {
        ApplySafeArea(force: true);
    }

    private void Update()
    {
        ApplySafeArea(force: false);
    }

    [ContextMenu("Apply Safe Area")]
    private void ApplySafeAreaFromContextMenu()
    {
        ApplySafeArea(force: true);
    }

    private void ApplySafeArea(bool force)
    {
        int screenWidth = Screen.width;
        int screenHeight = Screen.height;

        if (screenWidth <= 0 || screenHeight <= 0)
        {
            return;
        }

        Rect safeArea = Screen.safeArea;
        if (!force
            && safeArea == previousSafeArea
            && screenWidth == previousScreenWidth
            && screenHeight == previousScreenHeight)
        {
            return;
        }

        if (targetRectTransform == null)
        {
            targetRectTransform = GetComponent<RectTransform>();
        }

        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;
        anchorMin.x /= screenWidth;
        anchorMin.y /= screenHeight;
        anchorMax.x /= screenWidth;
        anchorMax.y /= screenHeight;

        targetRectTransform.anchorMin = anchorMin;
        targetRectTransform.anchorMax = anchorMax;
        targetRectTransform.offsetMin = Vector2.zero;
        targetRectTransform.offsetMax = Vector2.zero;

        previousSafeArea = safeArea;
        previousScreenWidth = screenWidth;
        previousScreenHeight = screenHeight;
    }
}
