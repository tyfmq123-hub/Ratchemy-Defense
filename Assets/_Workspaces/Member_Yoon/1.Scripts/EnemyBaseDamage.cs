using UnityEngine;

public class EnemyBaseDamage : MonoBehaviour
{
    [Header("기지에 도착했을 때 상승시키는 온도")]
    public float temperatureDamage = 10f;

    [Header("기지 도착 이펙트")]
    [SerializeField] private GameObject effectPrefab;
    [SerializeField] private Vector2 effectOffset = Vector2.zero;

    private bool hasReachedBase = false;

    public bool TryReachBase()
    {
        if (hasReachedBase)
            return false;

        hasReachedBase = true;

        if (effectPrefab != null)
        {
            var effect = Instantiate(effectPrefab, transform.position + (Vector3)effectOffset, Quaternion.identity);
            Destroy(effect, 1f);
        }

        Destroy(gameObject);
        return true;
    }
}