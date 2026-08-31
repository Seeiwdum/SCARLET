using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class GuardianBoss : MonoBehaviour, IDamageable, IParryable // เพิ่ม IParryable
{
    public enum BossState { Dormant, Chase, AttackCharge, Stunned, Dead }

    [Header("Boss Identity")]
    [SerializeField] private int maxHealth = 30;
    private int currentHealth;

    [Header("Movement & Range")]
    [SerializeField] private float chaseSpeed = 3.5f;
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float attackRange = 5f; // ระยะเริ่มชาร์จ

    [Header("Charge Attack Settings")]
    [SerializeField] private float chargeSpeed = 12f;
    [SerializeField] private int attackDamage = 1;
    [SerializeField] private LayerMask playerLayer;

    [Header("Parry & Stun Feedback")]
    [SerializeField] private float knockbackForce = 5f;
    [SerializeField] private float stunDuration = 2.0f; // เวลาสตั้นให้ตีฟรี
    [SerializeField] private Color chargeTelegraphColor = new Color(1f, 0.5f, 0f); // สีส้มตอนชาร์จ
    [SerializeField] private Color stunColor = Color.gray;
    private SpriteRenderer bossSprite;
    private Color originalColor;

    private BossState currentState = BossState.Dormant;
    private Transform playerTransform;
    private Rigidbody2D rb;
    private bool isFacingRight = false;
    private Coroutine currentAttackRoutine;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bossSprite = GetComponent<SpriteRenderer>();
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
        if (currentState == BossState.Dead || currentState == BossState.AttackCharge || currentState == BossState.Stunned || playerTransform == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        if (currentState == BossState.Dormant && distanceToPlayer <= detectionRange)
        {
            currentState = BossState.Chase;
            BossHealthBarUI.Instance?.ShowBossBar("Guardian Boss", maxHealth);
        }
        else if (currentState == BossState.Chase)
        {
            LookAtPlayer();
            if (distanceToPlayer <= attackRange)
            {
                currentAttackRoutine = StartCoroutine(PerformChargeAttack());
            }
            else
            {
                MoveTowardsPlayer();
            }
        }
    }

    private void MoveTowardsPlayer()
    {
        Vector2 target = new Vector2(playerTransform.position.x, rb.position.y);
        Vector2 newPos = Vector2.MoveTowards(rb.position, target, chaseSpeed * Time.deltaTime);
        rb.MovePosition(newPos);
    }

    private void LookAtPlayer()
    {
        if (playerTransform.position.x > transform.position.x && !isFacingRight) Flip();
        else if (playerTransform.position.x < transform.position.x && isFacingRight) Flip();
    }

    private void Flip()
    {
        isFacingRight = !isFacingRight;
        transform.Rotate(0f, 180f, 0f);
    }

    // ==========================================
    // [ Boss Pattern ] ท่าชาร์จพุ่งชน
    // ==========================================
    private IEnumerator PerformChargeAttack()
    {
        currentState = BossState.AttackCharge;
        rb.linearVelocity = Vector2.zero;

        // 1. Telegraph (ส่งสัญญาณเตือนว่าจะพุ่ง) เปลี่ยนเป็นสีส้ม
        if (bossSprite != null) bossSprite.color = chargeTelegraphColor;
        
        // สั่นตัวเตือน
        Vector3 origPos = transform.position;
        for (int i = 0; i < 10; i++)
        {
            transform.position = origPos + (Vector3)(Random.insideUnitCircle * 0.1f);
            yield return new WaitForSeconds(0.05f);
        }
        transform.position = origPos;

        // 2. Dash (พุ่งชน)
        if (bossSprite != null) bossSprite.color = Color.red;
        Vector2 dashDirection = isFacingRight ? Vector2.right : Vector2.left;
        rb.linearVelocity = dashDirection * chargeSpeed;

        // พุ่งเป็นเวลา 0.5 วินาที
        float timer = 0f;
        while (timer < 0.5f)
        {
            // ทำดาเมจถ้าชนผู้เล่นระหว่างพุ่ง (เว้นแต่ผู้เล่นกำลัง Parry อยู่)
            Collider2D hit = Physics2D.OverlapBox(transform.position, new Vector2(1.5f, 1.5f), 0, playerLayer);
            if (hit != null)
            {
                PlayerParry pp = hit.GetComponent<PlayerParry>();
                if (pp == null || !pp.IsParrying) // ถ้าผู้เล่นไม่ได้กด Parry ให้ทำดาเมจ
                {
                    hit.GetComponent<IDamageable>()?.TakeDamage(attackDamage, transform.position);
                }
            }
            timer += Time.deltaTime;
            yield return null;
        }

        // 3. Recovery (ฟื้นตัวหลังพุ่ง)
        rb.linearVelocity = Vector2.zero;
        if (bossSprite != null) bossSprite.color = originalColor;
        yield return new WaitForSeconds(1.0f);
        
        currentState = BossState.Chase;
    }

    // ==========================================
    // [ Parry Reaction ] การตอบสนองเมื่อถูกปัดป้อง
    // ==========================================
    public void OnParrySuccess(Vector3 parrySourcePosition)
    {
        if (currentState == BossState.Dead) return;

        // ยกเลิกท่าโจมตีปัจจุบันทันที
        if (currentAttackRoutine != null) StopCoroutine(currentAttackRoutine);

        StartCoroutine(StunRoutine(parrySourcePosition));
    }

    private IEnumerator StunRoutine(Vector3 sourcePos)
    {
        currentState = BossState.Stunned;
        
        // 1. หยุดบอสให้อยู่กับที่ ไม่กระเด็นทะลุฉาก
        rb.linearVelocity = Vector2.zero;

        // 2. เปลี่ยนสีแสดงอาการ Stun (บังคับให้ Alpha = 1 เพื่อไม่ให้บอสหักเหหายไป)
        if (bossSprite != null) 
        {
            Color solidStun = stunColor;
            solidStun.a = 1f;
            bossSprite.color = solidStun;
        }

        // 3. ค้างสถานะ Stun ให้ผู้เล่นตีฟรี
        yield return new WaitForSeconds(stunDuration);

        // 4. กลับเข้าสู่โหมดปกติ
        if (bossSprite != null) bossSprite.color = originalColor;
        currentState = BossState.Chase;
    }

    public void TakeDamage(int damage, Vector3 sourcePosition)
    {
        if (currentState == BossState.Dead) return;
        currentHealth -= damage;
        
        BossHealthBarUI.Instance?.UpdateHealth(currentHealth);
        StartCoroutine(HitFlashRoutine());

        if (currentHealth <= 0) Die();
    }

    private IEnumerator HitFlashRoutine()
    {
        if (bossSprite != null)
        {
            bossSprite.color = Color.white;
            yield return new WaitForSeconds(0.1f);
            // คืนค่าสีเดิมถ้าไม่ได้อยู่ในสถานะ Stun หรือ Charge
            if (currentState != BossState.Stunned && currentState != BossState.AttackCharge)
            {
                bossSprite.color = originalColor;
            }
        }
    }

    private void Die()
    {
        currentState = BossState.Dead;
        rb.linearVelocity = Vector2.zero;
        BossHealthBarUI.Instance?.HideBossBar();
        Destroy(gameObject, 0.2f);
    }
}