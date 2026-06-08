using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class UnitInfoCard : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private TMP_Text numberText;
    [SerializeField] private TMP_Text unitNameText;
    [SerializeField] private Image unitImage;
    [SerializeField] private TMP_Text unitTypeText;
    [SerializeField] private TMP_Text descriptionText;

    private IUnitDisplayData data;
    private UnitInfoPopup popup;

    private void Awake()
    {
        popup = FindAnyObjectByType<UnitInfoPopup>(FindObjectsInactive.Include);
    }

    public void Setup(IUnitDisplayData unitData)
    {
        data = unitData;
        if (numberText != null)
        {
            numberText.gameObject.SetActive(data.Number > 0);
            numberText.text = data.Number.ToString();
        }
        unitNameText.text = data.UnitName;
        unitImage.sprite = data.UnitSprite;
        unitImage.color = data.UnitSpriteColor;
        unitImage.preserveAspect = true;
        unitImage.rectTransform.sizeDelta = data.UnitSpriteSize;
        unitTypeText.text = data.ElementType;
        descriptionText.text = data.Description;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (popup != null)
            popup.Show(data);
    }
}
