using UnityEngine;

// 씬에 1개만 두는 소환 관리자
// - spawnPointA / B에 번갈아 소환 (1번째→A, 2번째→B, 3번째→A …)
// - UnitCard 5개가 모두 이 컴포넌트를 참조
public class UnitCardSpawner : MonoBehaviour
{
    [Header("플레이어 소환 위치 (A ↔ B 교대)")]
    [SerializeField] private Transform spawnPointA; // 예: P.SpawnPoint_01
    [SerializeField] private Transform spawnPointB; // 예: P.SpawnPoint_02

    [Header("소환 사운드")]
    [SerializeField][Range(0f, 3f)] private float spawnSoundVolumeBoost = 2f;
    [SerializeField] private bool playSpawnSoundAtCamera = true;

    private bool nextSpawnUsesA = true;

    // 다음 소환에 쓸 위치·회전을 반환하고, 다음에는 반대 지점을 쓰도록 토글
    public bool TryGetNextSpawnTransform(out Vector3 position, out Quaternion rotation)
    {
        Transform point = nextSpawnUsesA ? spawnPointA : spawnPointB;
        nextSpawnUsesA = !nextSpawnUsesA;

        if (point == null)
        {
            Debug.LogWarning("[UnitCardSpawner] spawnPointA 또는 spawnPointB가 비어 있습니다.");
            position = Vector3.zero;
            rotation = Quaternion.identity;
            return false;
        }

        position = point.position;
        rotation = point.rotation;
        return true;
    }

    public PlayerUnitBase SpawnUnit(
        PlayerUnitBase prefab,
        int spentCost = 0,
        float deathRefundRatio = -1f,
        AudioClip spawnSound = null,
        float spawnSoundVolume = 1f)
    {
        if (prefab == null)
        {
            Debug.LogWarning("[UnitCardSpawner] unitPrefab이 null입니다.");
            return null;
        }

        if (!TryGetNextSpawnTransform(out Vector3 pos, out Quaternion rot))
            return null;

        PlayerUnitBase instance = Instantiate(prefab, pos, rot);
        instance.ConfigureSpawnCost(spentCost, deathRefundRatio);

        if (spawnSound != null)
            PlaySpawnSound(spawnSound, pos, spawnSoundVolume);

        Debug.Log($"[UnitCardSpawner] {prefab.name} 소환 완료 (다음 소환: {(nextSpawnUsesA ? "A" : "B")})");
        return instance;
    }

    private void PlaySpawnSound(AudioClip clip, Vector3 spawnPosition, float volume)
    {
        float finalVolume = Mathf.Clamp(volume * spawnSoundVolumeBoost, 0f, 3f);
        Vector3 playPosition = spawnPosition;

        if (playSpawnSoundAtCamera && Camera.main != null)
            playPosition = Camera.main.transform.position;

        AudioSource.PlayClipAtPoint(clip, playPosition, finalVolume);
    }
}
