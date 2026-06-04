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
    private readonly HashSet<EnemyUnit> damagedEnemies = new HashSet<EnemyUnit>();

    public void Initialize(int damageAmount, LayerMask enemies, Vector2 direction)
    {
        damage = damageAmount;
        enemyLayer = enemies;
        SetDirection(direction);
    }

    private void Awake()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.isTrigger = true;
    }

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        transform.Translate(moveDirection * moveSpeed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDamageEnemy(other);
    }

    private void TryDamageEnemy(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & enemyLayer) == 0)
            return;

        EnemyUnit enemy = other.GetComponent<EnemyUnit>();
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
