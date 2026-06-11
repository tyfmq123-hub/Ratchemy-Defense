using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillSlotUI : MonoBehaviour
{
    [SerializeField] private Image skillIconImage;
    [SerializeField] private TMP_Text skillNameText;
    [SerializeField] private TMP_Text skillDescriptionText;

    public void Setup(SkillData data)
    {
        if (skillIconImage != null)
        {
            skillIconImage.sprite = data.skillIcon;
            skillIconImage.gameObject.SetActive(data.skillIcon != null);
        }
        if (skillNameText != null)
            skillNameText.text = data.skillName;
        if (skillDescriptionText != null)
            skillDescriptionText.text = data.skillDescription;
    }
}
