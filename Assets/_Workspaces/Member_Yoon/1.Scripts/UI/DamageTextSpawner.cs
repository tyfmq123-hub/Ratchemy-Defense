using UnityEngine;

public class DamageTextSpawner : MonoBehaviour
{
    public static DamageTextSpawner Instance { get; private set; }

    [SerializeField] private DamageText prefab;
    [SerializeField] private float yAboveSpriteTop = 0.1f;
    [SerializeField] private float randomXRange    = 0.2f;
    [SerializeField] private float spawnZ          = -1f;

    private static readonly Color ColorEnemyHit = Color.white;
    private static readonly Color ColorAllyHit  = new Color(1f, 0.3f, 0.3f, 1f);

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public static void Spawn(int damage, SpriteRenderer sr, bool isAllyHit)
    {
        if (Instance == null || Instance.prefab == null || sr == null) return;

        Bounds b = sr.bounds;
        Vector3 pos = new Vector3(
            b.center.x + Random.Range(-Instance.randomXRange, Instance.randomXRange),
            b.max.y + Instance.yAboveSpriteTop,
            Instance.spawnZ
        );
        DamageText obj = Instantiate(Instance.prefab, pos, Quaternion.identity);
        obj.Show(damage, isAllyHit ? ColorAllyHit : ColorEnemyHit);
    }
}
