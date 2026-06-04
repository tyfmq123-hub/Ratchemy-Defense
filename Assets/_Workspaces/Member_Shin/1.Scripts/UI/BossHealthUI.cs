using TMPro;          // TextMeshPro 텍스트를 사용하기 위해 필요합니다.
using UnityEngine;    // Unity 기본 기능을 사용하기 위해 필요합니다.
using UnityEngine.UI; // UI Image를 사용하기 위해 필요합니다.

public class BossHealthUI : MonoBehaviour
{
    [Header("연결할 UI")]
    public Image bossHealthFill;        // 실제로 줄어드는 보스 체력 게이지입니다.
    public TextMeshProUGUI bossHealthText; // 보스 체력 숫자를 표시합니다. 없어도 됩니다.

    public void SetHealth(
        float currentHealth,
        float maxHealth
    )
    {
        // 최대 체력이 0이면 나눗셈 오류가 생길 수 있으므로
        // 안전하게 UI를 비웁니다.
        if (maxHealth <= 0f)
        {
            bossHealthFill.fillAmount = 0f;

            if (bossHealthText != null)
            {
                bossHealthText.text = "0 / 0";
            }

            return;
        }

        // 현재 체력이 0보다 작아지거나
        // 최대 체력보다 커지지 않도록 제한합니다.
        currentHealth = Mathf.Clamp(
            currentHealth,
            0f,
            maxHealth
        );

        // 현재 체력을 0~1 사이의 비율로 바꿉니다.
        //
        // 예:
        // 현재 체력 750 / 최대 체력 1000
        // → fillAmount = 0.75
        bossHealthFill.fillAmount =
            currentHealth / maxHealth;

        // 텍스트를 연결했다면
        // 현재 체력과 최대 체력을 화면에 표시합니다.
        if (bossHealthText != null)
        {
            bossHealthText.text =
                currentHealth.ToString();
        }
    }
}