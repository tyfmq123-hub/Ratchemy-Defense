using UnityEngine;

// 화염슬라임 3단계 원거리 투사체
// FlameSlimeTier3.Attack()에서 Instantiate 후 Initialize() 호출
// Collider2D (IsTrigger = true) 컴포넌트 필요
[RequireComponent(typeof(Collider2D))]
public class FlameProjectile : MonoBehaviour
{
    private Vector3 direction;
    private float speed;
    private int damage;

    [SerializeField] private float lifetime = 5f;

    public void Initialize(Vector3 dir, float spd, int dmg)
    {
        direction = dir;
        speed = spd;
        damage = dmg;

        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerUnitBase player = other.GetComponentInParent<PlayerUnitBase>();
        if (player != null)
        {
            player.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}
