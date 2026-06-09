using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class HoverColorEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image targetImage;
    [SerializeField] private Color hoverColor = new Color(0.7f, 0.7f, 0.7f, 1f);
    private Color normalColor;

    private void Awake()
    {
        if (targetImage != null)
            normalColor = targetImage.color;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (targetImage != null)
            targetImage.color = hoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (targetImage != null)
            targetImage.color = normalColor;
    }
}
