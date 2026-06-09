using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UnitInfoPopup : MonoBehaviour
{
    [Header("기본 정보")]
    [SerializeField] private TMP_Text unitNameText;
    [SerializeField] private Image unitImage;
    [SerializeField] private TMP_Text unitCostValueText;

    [Header("타입")]
    [SerializeField] private TMP_Text roleTypeText;
    [SerializeField] private TMP_Text elementTypeText;

    [Header("스탯")]
    [SerializeField] private TMP_Text attackPointText;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private TMP_Text moveSpeedText;
    [SerializeField] private TMP_Text attackSpeedText;
    [SerializeField] private TMP_Text attackDistanceText;

    [Header("설명")]
    [SerializeField] private TMP_Text descriptionText;

    [Header("스킬 슬롯 (최대 3개)")]
    [SerializeField] private SkillSlotUI[] skillSlots;

    [Header("버튼")]
    [SerializeField] private Button checkButton;

    private void Awake()
    {
        checkButton.onClick.AddListener(Hide);
        gameObject.SetActive(false);
    }

    public void Show(IUnitDisplayData data)
    {
        unitNameText.text = $"{data.UnitName} 상세 정보";
        unitImage.sprite = data.UnitSprite;
        unitImage.color = data.UnitSpriteColor;
        unitImage.preserveAspect = true;
        if (unitCostValueText != null)
        {
            unitCostValueText.gameObject.SetActive(data.Cost > 0);
            unitCostValueText.text = $"{data.Cost} 코스트";
        }
        roleTypeText.text = data.RoleType;
        elementTypeText.text = data.ElementType;
        attackPointText.text = data.Damage.ToString();
        hpText.text = data.MaxHp.ToString();
        moveSpeedText.text = data.MoveSpeed.ToString("F1");
        attackSpeedText.text = data.AttackSpeed.ToString("F1");
        attackDistanceText.text = data.AttackRange.ToString("F1");
        descriptionText.text = data.PopupDescription;
        SetupSkills(data.Skills);
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void SetupSkills(SkillData[] skills)
    {
        if (skillSlots == null) return;

        for (int i = 0; i < skillSlots.Length; i++)
        {
            bool hasSkill = skills != null && i < skills.Length;
            skillSlots[i].gameObject.SetActive(hasSkill);
            if (hasSkill)
                skillSlots[i].Setup(skills[i]);
        }
    }
}
