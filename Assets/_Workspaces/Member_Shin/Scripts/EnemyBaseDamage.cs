using UnityEngine;

public class EnemyBaseDamage : MonoBehaviour
{
    [Header("기지에 도착했을 때 상승시키는 온도")]
    public float temperatureDamage = 10f; // 일반 적은 우선 10°C로 시작합니다.

    private bool hasReachedBase = false;  // 이미 기지에 닿은 적인지 확인합니다.

    public bool TryReachBase()
    {
        // 이미 처리된 적이라면 다시 데미지를 주지 않습니다.
        if (hasReachedBase)
        {
            return false;
        }

        // 기지 도착 처리가 중복되지 않도록 기록합니다.
        hasReachedBase = true;

        // 기지에 도착한 적은 제거합니다.
        Destroy(gameObject);

        // 온도를 상승시켜도 된다는 의미로 true를 반환합니다.
        return true;
    }
}