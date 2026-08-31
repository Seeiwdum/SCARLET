using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class GuardianBoss : MonoBehaviour, IDamageable
{
    public enum BossState { Dormant, Chase, AttackSmash, AttackShockwave, EnrageTransition, Dead }

    [Header("Boss Identity & Health")]
    [SerializeField] private string bossName = "ผู้พิทักษ์แห่งพงไพร (Guardian of Thorns)";
    [SerializeField] private int maxHealth = 30;
    private int currentHealth;
    private bool isPhase2 = false;

    [Header("Movement & Range Settings")]
    [SerializeField] private float chaseSpeed = 3.5f;
    [SerializeField] private float enrageSpeed = 5.2f;
    [SerializeField] private float attackRange = 2.2f;
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Attack Settings")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackRadius = 1.8f;
    [SerializeField] private int attackDamage = 1;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private float attackCooldown = 2.0f;

    [Header("Drops & VFX")]
    [SerializeField] private GameObject flameHeartPrefab; // Prefab หัวใจเพลิง
    [SerializeField] private GameObject deathVFX;
    [SerializeField] private SpriteRenderer bossSprite;
    [SerializeField] private Color hitColor = Color.red;

    private BossState currentState = BossState.Dormant;
    private Transform playerTransform;
    private Rigidbody2D rb;
    private bool isAttacking = false;
    private bool isFacingRight = false;
    private Color originalColor;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (bossSprite == null) bossSprite = GetComponent<SpriteRenderer>();
        if (bossSprite != null) originalColor = bossSprite.color;
        currentHealth = maxHealth;
    }

    private void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerTransform = p.transform;
    }

    private void Update()
    {
        if (currentState == BossState.Dead || isAttacking || playerTransform == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        switch (currentState)
        {
            case BossState.Dormant:
                if (distanceToPlayer <= detectionRange)
                {
                    WakeUpBoss();
                }
                break;

            case BossState.Chase:
                LookAtPlayer();
                if (distanceToPlayer <= attackRange)
                {
                    StartCoroutine(PerformSmashAttack());
                }
                else
                {
                    MoveTowardsPlayer();
                }
                break;
        }
    }

    private void WakeUpBoss()
    {
        currentState = BossState.Chase;
        if (BossHealthBarUI.Instance != null)
        {
            BossHealthBarUI.Instance.ShowBossBar(bossName, maxHealth);
        }
    }

    private void MoveTowardsPlayer()
    {
        float speed = isPhase2 ? enrageSpeed : chaseSpeed;
        Vector2 target = new Vector2(playerTransform.position.x, rb.position.y);
        Vector2 newPos = Vector2.MoveTowards(rb.position, target, speed * Time.deltaTime);
        rb.MovePosition(newPos);
    }

    private void LookAtPlayer()
    {
        if (playerTransform.position.x > transform.position.x && !isFacingRight)
        {
            Flip();
        }
        else if (playerTransform.position.x < transform.position.x && isFacingRight)
        {
            Flip();
        }
    }

    private void Flip()
    {
        isFacingRight = !isFacingRight;
        transform.Rotate(0f, 180f, 0f);
    }

    private IEnumerator PerformSmashAttack()
    {
        isAttacking = true;
        rb.linearVelocity = Vector2.zero;

        // หน่วงเวลาเตือนก่อนทุบ (Telegraph)
        if (bossSprite != null) bossSprite.color = Color.yellow;
        yield return new WaitForSeconds(0.6f);
        if (bossSprite != null) bossSprite.color = originalColor;

        // วงดาเมจตอนทุบ
        if (attackPoint != null)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, attackRadius, playerLayer);
            foreach (var hit in hits)
            {
                IDamageable target = hit.GetComponent<IDamageable>();
                target?.TakeDamage(attackDamage, transform.position);
            }
        }

        yield return new WaitForSeconds(isPhase2 ? attackCooldown * 0.6f : attackCooldown);
        isAttacking = false;
    }

    public void TakeDamage(int damage, Vector3 sourcePosition)
    {
        if (currentState == BossState.Dead) return;

        if (currentState == BossState.Dormant)
        {
            WakeUpBoss();
        }

        currentHealth -= damage;
        if (BossHealthBarUI.Instance != null)
        {
            BossHealthBarUI.Instance.UpdateHealth(currentHealth);
        }

        StartCoroutine(HitFlashRoutine());

        // ตรวจสอบเข้าสู่ Phase 2 (Enrage) เมื่อเลือดต่ำกว่าครึ่ง
        if (!isPhase2 && currentHealth <= maxHealth / 2)
        {
            StartCoroutine(TriggerPhase2Routine());
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private IEnumerator TriggerPhase2Routine()
    {
        isPhase2 = true;
        isAttacking = true;
        currentState = BossState.EnrageTransition;
        rb.linearVelocity = Vector2.zero;

        // กระพริบตัวแดงเข้าสู่โหมดคลั่ง
        if (bossSprite != null) bossSprite.color = Color.red;
        yield return new WaitForSeconds(1.0f);
        if (bossSprite != null) bossSprite.color = originalColor;

        isAttacking = false;
        currentState = BossState.Chase;
    }

    private IEnumerator HitFlashRoutine()
    {
        if (bossSprite != null)
        {
            bossSprite.color = hitColor;
            yield return new WaitForSeconds(0.08f);
            bossSprite.color = isPhase2 ? new Color(1f, 0.6f, 0.6f) : originalColor;
        }
    }

    private void Die()
    {
        currentState = BossState.Dead;
        rb.linearVelocity = Vector2.zero;

        if (BossHealthBarUI.Instance != null)
        {
            BossHealthBarUI.Instance.HideBossBar();
        }

        if (deathVFX != null)
        {
            Instantiate(deathVFX, transform.position, Quaternion.identity);
        }

        // เสกดรอปหัวใจเพลิง
        if (flameHeartPrefab != null)
        {
            Instantiate(flameHeartPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
        }

        Destroy(gameObject, 0.2f);
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
        }
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}