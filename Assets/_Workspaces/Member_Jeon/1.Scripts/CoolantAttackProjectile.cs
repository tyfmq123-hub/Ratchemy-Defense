using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CoolantAttackProjectile : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float lifeTime = 3f;
    [SerializeField] private float spinSpeed = 360f;

    private Vector2 moveDirection = Vector2.right;
    private int damage;
    private LayerMask enemyLayer;
    private Rigidbody2D rb;
    private Transform spinVisual;
    private SpriteRenderer spinSpriteRenderer;

    public void Initialize(int damageAmount, LayerMask enemies, Vector2 direction, float speed = -1f)
    {
        damage = damageAmount;
        enemyLayer = enemies;

        if (speed > 0f)
            moveSpeed = speed;

        SetDirection(direction);
        SetupSpinVisual();
        ApplyVelocity();
    }

    public void ApplySortingOrder(int sortingOrder)
    {
        SpriteRenderer spriteRenderer = GetVisibleSpriteRenderer();
        if (spriteRenderer != null)
            spriteRenderer.sortingOrder = sortingOrder;
    }

    private void Awake()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.isTrigger = true;

        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
    }

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void FixedUpdate()
    {
        if (rb != null)
            rb.linearVelocity = moveDirection * moveSpeed;
        else
            transform.position += (Vector3)(moveDirection * moveSpeed * Time.fixedDeltaTime);
    }

    private void Update()
    {
        if (spinVisual == null || Mathf.Approximately(spinSpeed, 0f))
            return;

        spinVisual.Rotate(0f, 0f, spinSpeed * Time.deltaTime);
        UpdateSpinVisualOffset();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & enemyLayer) == 0)
            return;

        EnemyUnit enemy = other.GetComponent<EnemyUnit>();
        if (enemy == null || enemy.IsDead())
            return;

        enemy.TakeDamage(damage);
        Destroy(gameObject);
    }

    private void ApplyVelocity()
    {
        if (rb != null)
            rb.linearVelocity = moveDirection * moveSpeed;
    }

    private void SetDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.0001f)
            direction = Vector2.right;

        moveDirection = direction.normalized;
        transform.rotation = Quaternion.identity;
    }

    private SpriteRenderer GetVisibleSpriteRenderer()
    {
        if (spinSpriteRenderer != null)
            return spinSpriteRenderer;

        return GetComponent<SpriteRenderer>();
    }

    private void SetupSpinVisual()
    {
        SpriteRenderer rootSprite = GetComponent<SpriteRenderer>();
        if (rootSprite == null)
            return;

        if (Mathf.Approximately(spinSpeed, 0f))
            return;

        Animator rootAnimator = GetComponent<Animator>();

        GameObject visual = new GameObject("SpinVisual");
        visual.transform.SetParent(transform, false);
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;

        spinSpriteRenderer = visual.AddComponent<SpriteRenderer>();
        spinSpriteRenderer.sprite = rootSprite.sprite;
        spinSpriteRenderer.color = rootSprite.color;
        spinSpriteRenderer.sortingLayerID = rootSprite.sortingLayerID;
        spinSpriteRenderer.sortingOrder = rootSprite.sortingOrder;
        spinSpriteRenderer.flipX = false;
        spinSpriteRenderer.flipY = false;

        if (rootAnimator != null)
        {
            Animator spinAnimator = visual.AddComponent<Animator>();
            spinAnimator.runtimeAnimatorController = rootAnimator.runtimeAnimatorController;
            rootAnimator.enabled = false;
        }

        rootSprite.enabled = false;
        spinVisual = visual.transform;
        UpdateSpinVisualOffset();
    }

    private void UpdateSpinVisualOffset()
    {
        if (spinVisual == null || spinSpriteRenderer == null || spinSpriteRenderer.sprite == null)
            return;

        Vector3 spriteCenter = spinSpriteRenderer.sprite.bounds.center;
        float angleZ = spinVisual.localEulerAngles.z;
        spinVisual.localPosition = -(Quaternion.Euler(0f, 0f, angleZ) * spriteCenter);
    }
}
