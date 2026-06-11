using UnityEngine;
using UnityEngine.UI;

// 플레이어 스킬 쿨타임 MP 바 — 비어 있다가 쿨이 돌면 채워짐 (1 = 스킬 사용 가능)
public class Mpbar : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField] private PlayerUnitBase playerUnit;

    private void Awake()
    {
        ResolveFillImage();

        if (playerUnit == null)
            playerUnit = GetComponentInParent<PlayerUnitBase>();
    }

    private void OnEnable()
    {
        Refresh();
    }

    private void Update()
    {
        Refresh();
    }

    public void Bind(PlayerUnitBase unit)
    {
        playerUnit = unit;
        Refresh();
    }

    private void Refresh()
    {
        if (playerUnit == null)
            return;

        ApplyFill(playerUnit.SkillCooldownFill);
    }

    private void ApplyFill(float fillAmount)
    {
        if (fillImage == null)
            return;

        fillImage.fillAmount = Mathf.Clamp01(fillAmount);
    }

    private void ResolveFillImage()
    {
        if (fillImage != null)
            return;

        Transform fillTransform = transform.Find("Background/Fill");
        if (fillTransform == null)
            fillTransform = transform.Find("Fill");

        if (fillTransform != null)
        {
            fillImage = fillTransform.GetComponent<Image>();
            if (fillImage != null)
                return;
        }

        Image[] images = GetComponentsInChildren<Image>(true);
        foreach (Image image in images)
        {
            if (image.gameObject.name == "Outline")
                continue;

            if (image.type == Image.Type.Filled)
            {
                fillImage = image;
                return;
            }
        }
    }
}
