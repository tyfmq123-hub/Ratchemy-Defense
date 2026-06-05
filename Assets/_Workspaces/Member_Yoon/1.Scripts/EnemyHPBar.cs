using UnityEngine;
using UnityEngine.UI;

public class EnemyHPBar : MonoBehaviour
{
    [SerializeField] private Image fillImage;

    public void UpdateHP(int current, int max)
    {
        if (fillImage == null) return;
        fillImage.fillAmount = max > 0 ? (float)current / max : 0f;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
