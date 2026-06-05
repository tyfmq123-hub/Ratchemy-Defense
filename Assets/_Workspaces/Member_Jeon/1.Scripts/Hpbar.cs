using UnityEngine;
using UnityEngine.UI;

// 플레이어 유닛 머리 위 HP 바 — PlayerUnitBase 체력과 연동
public class Hpbar : MonoBehaviour
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
        Subscribe();
        Refresh();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    public void Bind(PlayerUnitBase unit)
    {
        if (playerUnit == unit)
            return;

        Unsubscribe();
        playerUnit = unit;
        Subscribe();
        Refresh();
    }

    private void Subscribe()
    {
        if (playerUnit != null)
            playerUnit.OnHealthChanged += HandleHealthChanged;
    }

    private void Unsubscribe()
    {
        if (playerUnit != null)
            playerUnit.OnHealthChanged -= HandleHealthChanged;
    }

    private void HandleHealthChanged(float currentHp, float maxHp)
    {
        ApplyFill(currentHp, maxHp);
    }

    private void Refresh()
    {
        if (playerUnit == null)
            return;

        ApplyFill(playerUnit.CurrentHp, playerUnit.MaxHp);
    }

    private void ApplyFill(float currentHp, float maxHp)
    {
        if (fillImage == null)
            return;

        fillImage.fillAmount = maxHp > 0f ? Mathf.Clamp01(currentHp / maxHp) : 0f;
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
