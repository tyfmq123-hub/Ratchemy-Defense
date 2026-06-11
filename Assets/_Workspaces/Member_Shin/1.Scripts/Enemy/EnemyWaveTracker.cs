using System;
using UnityEngine;

// 웨이브에서 생성된 적이 제거되었는지 추적합니다.
// 적이 죽어서 Destroy되면 WaveManager에 제거 사실을 알려 줍니다.
public class EnemyWaveTracker : MonoBehaviour
{
    // EnemySpawner가 전달해 주는 제거 알림 함수입니다.
    private Action onEnemyRemoved;

    // 같은 적이 두 번 집계되지 않도록 막습니다.
    private bool hasReportedRemoval;

    // EnemySpawner에서 적을 생성한 직후 호출합니다.
    public void Initialize(Action removedCallback)
    {
        onEnemyRemoved = removedCallback;
    }

    // 적 오브젝트가 Destroy될 때 Unity가 자동으로 호출합니다.
    private void OnDestroy()
    {
        // 이미 제거 사실을 알렸다면 다시 실행하지 않습니다.
        if (hasReportedRemoval)
        {
            return;
        }

        hasReportedRemoval = true;

        // 콜백을 먼저 비운 뒤 한 번만 호출합니다.
        // 씬 전환 중 오브젝트가 연달아 정리되더라도
        // 같은 콜백 참조를 오래 유지하지 않도록 합니다.
        Action removedCallback = onEnemyRemoved;
        onEnemyRemoved = null;

        removedCallback?.Invoke();
    }
}