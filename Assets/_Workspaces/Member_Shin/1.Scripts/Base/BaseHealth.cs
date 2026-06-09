using UnityEngine;

public class BaseHealth : MonoBehaviour
{
    [Header("배터리 온도 설정")]
    [Range(0f, 130f)]
    public float currentTemperature = 0f;            // 게임 시작 시 배터리 온도입니다.
    public float thermalRunawayTemperature = 130f;  // 이 온도에 도달하면 열폭주 상태가 됩니다.

    [Header("연결할 UI")]
    public BaseHealthUI baseHealthUI;                // 화면 왼쪽 위의 배터리 UI입니다.

    [Header("연결할 게임 매니저")]
    [SerializeField] private GameManager gameManager; // 패배 화면을 실행할 GameManager입니다.

    // 열폭주 패배 처리가 여러 번 실행되지 않도록 막습니다.
    private bool isThermalRunawayTriggered = false;

    private void Awake()
    {
        // Inspector 연결을 빠뜨린 경우를 대비해 자동으로 찾습니다.
        // 가능하면 Inspector에서도 직접 연결해 두는 편이 안전합니다.
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }
    }

    private void Start()
    {
        // UI에서 사용하는 최대 온도도 열폭주 온도와 동일하게 맞춥니다.
        baseHealthUI.maxTemperature = thermalRunawayTemperature;

        // 게임 시작 시 초기 온도를 화면에 표시합니다.
        baseHealthUI.SetTemperature(currentTemperature);
    }

    public void AddTemperature(float amount)
    {
        // 이미 열폭주 패배 처리가 실행됐다면 추가 온도 처리를 하지 않습니다.
        if (isThermalRunawayTriggered)
        {
            return;
        }

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

        // 열폭주 온도에 도달하면 패배 화면을 실행합니다.
        if (currentTemperature >= thermalRunawayTemperature)
        {
            isThermalRunawayTriggered = true;

            if (gameManager == null)
            {
                Debug.LogError(
                    "[BaseHealth] GameManager가 연결되지 않았습니다."
                );

                return;
            }

            gameManager.Defeat();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 충돌한 오브젝트 또는 부모에서 EnemyBaseDamage를 찾습니다.
        EnemyBaseDamage enemy =
            other.GetComponentInParent<EnemyBaseDamage>();

        // EnemyBaseDamage를 찾지 못했다면 여기에서 종료됩니다.
        if (enemy == null)
        {
            return;
        }

        // 이미 처리한 적인지 확인합니다.
        if (!enemy.TryReachBase())
        {
            return;
        }

        // 적에게 설정된 값만큼 배터리 온도를 올립니다.
        AddTemperature(enemy.temperatureDamage);
    }
}