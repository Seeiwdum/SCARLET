using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class BaseEnemy : MonoBehaviour, IDamageable, IFormSwappable
{
    public enum EnemyState { Patrol, Chase, Stunned }

    [Header("State")]
    [SerializeField] private EnemyState currentState = EnemyState.Patrol;

    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 3;
    private int currentHealth;

    [Header("Movement Settings")]
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float chaseSpeed = 4f;
    [SerializeField] private bool movingRight = true;

    [Header("Detection Settings")]
    [SerializeField] private float detectionRange = 5f;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private Transform wallCheckPoint;
    [SerializeField] private float checkDistance = 0.5f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Knockback Settings")]
    [SerializeField] private float knockbackForce = 5f;
    [SerializeField] private float stunDuration = 0.2f;

    [Header("Form Swapping")]
    [SerializeField] private Sprite monsterSprite;
    [SerializeField] private Sprite villagerSprite;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Transform playerTransform;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void Start()
    {
        currentHealth = maxHealth;
        PlayerController2D player = FindFirstObjectByType<PlayerController2D>();
        if (player != null) playerTransform = player.transform;
    }

    private void OnEnable()
    {
        GameEvents.OnHoodToggled += OnHoodToggled;
        if (spriteRenderer != null && villagerSprite != null)
            spriteRenderer.sprite = villagerSprite;
    }

    private void OnDisable()
    {
        GameEvents.OnHoodToggled -= OnHoodToggled;
    }

    private void Update()
    {
        if (currentState == EnemyState.Stunned) return;
        CheckForPlayer();
        switch (currentState)
        {
            case EnemyState.Patrol: PatrolLogic(); break;
            case EnemyState.Chase:  ChaseLogic();  break;
        }
    }

    #region Patrol & Chase Logic

    private void PatrolLogic()
    {
        bool isGroundedFront = Physics2D.Raycast(groundCheckPoint.position, Vector2.down, checkDistance, groundLayer);
        bool isWallFront     = Physics2D.Raycast(wallCheckPoint.position, movingRight ? Vector2.right : Vector2.left, checkDistance, groundLayer);
        if (!isGroundedFront || isWallFront) Flip();
        float speed = movingRight ? patrolSpeed : -patrolSpeed;
        rb.linearVelocity = new Vector2(speed, rb.linearVelocity.y);
    }

    private void ChaseLogic()
    {
        if (playerTransform == null) return;
        float direction = playerTransform.position.x - transform.position.x;
        if (direction > 0 && !movingRight) Flip();
        else if (direction < 0 && movingRight) Flip();
        bool isGroundedFront = Physics2D.Raycast(groundCheckPoint.position, Vector2.down, checkDistance, groundLayer);
        if (!isGroundedFront) { rb.linearVelocity = new Vector2(0, rb.linearVelocity.y); return; }
        float speed = movingRight ? chaseSpeed : -chaseSpeed;
        rb.linearVelocity = new Vector2(speed, rb.linearVelocity.y);
    }

    private void CheckForPlayer()
    {
        if (playerTransform == null) return;
        float dist = Vector2.Distance(transform.position, playerTransform.position);
        if (dist <= detectionRange) currentState = EnemyState.Chase;
        else if (currentState == EnemyState.Chase && dist > detectionRange * 1.3f) currentState = EnemyState.Patrol;
    }

    private void Flip()
    {
        movingRight = !movingRight;
        Vector3 s = transform.localScale;
        s.x *= -1;
        transform.localScale = s;
    }

    #endregion

    #region Combat & Damage Receiver

    public void TakeDamage(int damage, Vector3 attackerPosition)
    {
        if (currentHealth <= 0) return;
        currentHealth -= damage;
        Debug.Log($"<color=orange>[ENEMY HIT] {gameObject.name} HP: {currentHealth}/{maxHealth}</color>");
        if (currentHealth <= 0) Die();
        else StartCoroutine(HitStunRoutine(attackerPosition));
    }

    private IEnumerator HitStunRoutine(Vector3 attackerPosition)
    {
        currentState = EnemyState.Stunned;
        float knockbackDir = transform.position.x < attackerPosition.x ? -1f : 1f;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(new Vector2(knockbackDir * knockbackForce, knockbackForce * 0.5f), ForceMode2D.Impulse);
        if (spriteRenderer != null) spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(stunDuration);
        if (spriteRenderer != null) spriteRenderer.color = Color.white;
        currentState = EnemyState.Chase;
    }

    private void Die()
    {
        Debug.Log($"<color=red><b>[ENEMY DIED] {gameObject.name}</b></color>");
        GetComponent<Collider2D>().enabled = false;
        this.enabled = false;
        Destroy(gameObject, 0.5f);
    }

    #endregion

    #region IFormSwappable Implementation

    public void SwapForm(bool isHoodWorn)
    {
        if (spriteRenderer != null)
            spriteRenderer.sprite = isHoodWorn ? monsterSprite : villagerSprite;
    }

    private void OnHoodToggled(bool isHoodWorn) => SwapForm(isHoodWorn);

    #endregion

    #region Parry Domain - virtual hook for future parryable enemies (IParryable pruned)

    // Domain operation: reaction to being parried. Base enemies ignore by default.
    public virtual void OnParrySuccess(Vector3 parrySourcePosition) { }

    public virtual bool CanBeParried() => false;

    #endregion

    #region Debug Gizmos

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        if (groundCheckPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(groundCheckPoint.position, groundCheckPoint.position + Vector3.down * checkDistance);
        }
        if (wallCheckPoint != null)
        {
            Gizmos.color = Color.blue;
            Vector3 wallDir = movingRight ? Vector3.right : Vector3.left;
            Gizmos.DrawLine(wallCheckPoint.position, wallCheckPoint.position + wallDir * checkDistance);
        }
    }

    #endregion
}
