using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SkillEffectProjectile : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float lifeTime = 1.5f;

    private Vector2 moveDirection = Vector2.right;
    private int damage;
    private LayerMask enemyLayer;
    private Collider2D hitCollider;
    private ContactFilter2D enemyContactFilter;
    private bool contactFilterReady;
    private readonly HashSet<EnemyUnit> damagedEnemies = new HashSet<EnemyUnit>();
    private readonly Collider2D[] overlapBuffer = new Collider2D[32];

    public void Initialize(int damageAmount, LayerMask enemies, Vector2 direction)
    {
        damage = damageAmount;
        enemyLayer = enemies;
        SetDirection(direction);

        enemyContactFilter = new ContactFilter2D();
        enemyContactFilter.SetLayerMask(enemyLayer);
        enemyContactFilter.useTriggers = false;
        contactFilterReady = true;
    }

    private void Awake()
    {
        hitCollider = GetComponent<Collider2D>();
        if (hitCollider != null)
            hitCollider.isTrigger = true;
    }

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        Vector2 delta = moveDirection * (moveSpeed * Time.deltaTime);
        float travel = delta.magnitude;

        if (hitCollider != null && travel > 0f)
        {
            Bounds bounds = hitCollider.bounds;
            RaycastHit2D[] castHits = Physics2D.BoxCastAll(
                bounds.center,
                bounds.size,
                0f,
                moveDirection,
                travel,
                enemyLayer);

            foreach (RaycastHit2D hit in castHits)
            {
                if (hit.collider != null)
                    TryDamageEnemy(hit.collider);
            }
        }

        transform.Translate(delta, Space.World);
        ScanCurrentOverlaps();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDamageEnemy(other);
    }

    private void ScanCurrentOverlaps()
    {
        if (hitCollider == null || !contactFilterReady)
            return;

        int count = hitCollider.Overlap(enemyContactFilter, overlapBuffer);
        for (int i = 0; i < count; i++)
            TryDamageEnemy(overlapBuffer[i]);
    }

    private void TryDamageEnemy(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & enemyLayer) == 0)
            return;

        EnemyUnit enemy = other.GetComponent<EnemyUnit>();
        if (enemy == null)
            enemy = other.GetComponentInParent<EnemyUnit>();

        if (enemy == null || enemy.IsDead())
            return;

        if (!damagedEnemies.Add(enemy))
            return;

        enemy.TakeDamage(damage);
    }

    public void SetDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.0001f)
            direction = Vector2.right;

        moveDirection = direction.normalized;

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            spriteRenderer.flipX = moveDirection.x < 0f;
    }
}
