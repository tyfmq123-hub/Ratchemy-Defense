using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Canvas))]
public class ResultUICanvas : MonoBehaviour
{
    [SerializeField] private int sortingOrder = 100;

    private Canvas canvas;

    private void Awake()
    {
        canvas = GetComponent<Canvas>();
        ApplyCanvasSettings();
    }

    public void BringToFront()
    {
        ApplyCanvasSettings();
    }

    private void ApplyCanvasSettings()
    {
        if (canvas == null)
            return;

        canvas.overrideSorting = true;
        canvas.sortingOrder = sortingOrder;

        if (transform is RectTransform rect && rect.localScale == Vector3.zero)
            rect.localScale = Vector3.one;
    }
}
