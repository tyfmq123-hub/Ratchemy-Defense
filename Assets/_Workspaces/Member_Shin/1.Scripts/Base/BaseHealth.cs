using UnityEngine;

public class BaseHealth : MonoBehaviour
{
    [Header("배터리 온도 설정")]
    [Range(0f, 130f)]
    public float currentTemperature = 0f;         // 게임 시작 시 배터리 온도입니다.
    public float thermalRunawayTemperature = 130f;  // 이 온도에 도달하면 열폭주 상태가 됩니다.

    [Header("연결할 UI")]
    public BaseHealthUI baseHealthUI;                // 화면 왼쪽 위의 배터리 UI입니다.

    void Start()
    {
        // UI에서 사용하는 최대 온도도 열폭주 온도와 동일하게 맞춥니다.
        baseHealthUI.maxTemperature = thermalRunawayTemperature;

        // 게임 시작 시 초기 온도를 화면에 표시합니다.
        baseHealthUI.SetTemperature(currentTemperature);
    }

    public void AddTemperature(float amount)
    {
        // 적이 기지에 닿으면 적에게 설정된 값만큼 온도가 올라갑니다.
        currentTemperature += amount;

        // 온도가 최대값을 넘지 않도록 제한합니다.
        currentTemperature = Mathf.Clamp(
            currentTemperature,
            0f,
            thermalRunawayTemperature
        );

        // 변경된 온도를 UI에 표시합니다.
        baseHealthUI.SetTemperature(currentTemperature);

        // 열폭주 온도에 도달하면 패배 상태입니다.
        if (currentTemperature >= thermalRunawayTemperature)
        {
            Debug.Log("배터리 열폭주 발생! 게임 오버");

            // 나중에 이 위치에 패배 화면을 연결하면 됩니다.
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 적이 투명 박스에 들어왔을 때 이 로그가 보여야 합니다.
        Debug.Log("BaseTrigger 충돌 감지: " + other.gameObject.name);

        // 충돌한 오브젝트 또는 부모에서 EnemyBaseDamage를 찾습니다.
        EnemyBaseDamage enemy =
            other.GetComponentInParent<EnemyBaseDamage>();

        // EnemyBaseDamage를 찾지 못했다면 여기에서 종료됩니다.
        if (enemy == null)
        {
            return;
        }

        Debug.Log("EnemyBaseDamage 찾음 / 온도 증가량: " + enemy.temperatureDamage);

        // 이미 처리한 적인지 확인합니다.
        if (!enemy.TryReachBase())
        {
            Debug.LogWarning("이미 처리된 적이라서 온도를 올리지 않습니다.");
            return;
        }

        // 적에게 설정된 값만큼 배터리 온도를 올립니다.
        AddTemperature(enemy.temperatureDamage);

        Debug.Log("현재 배터리 온도: " + currentTemperature);
    }
}