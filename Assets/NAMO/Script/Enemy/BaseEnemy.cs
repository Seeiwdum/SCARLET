using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class BaseEnemy : MonoBehaviour, IDamageable
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
        
        // ค้นหา Player ใน Scene อัตโนมัติด้วย Tag "Player"
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
    }

    private void Update()
    {
        if (currentState == EnemyState.Stunned) return;

        CheckForPlayer();

        switch (currentState)
        {
            case EnemyState.Patrol:
                PatrolLogic();
                break;
            case EnemyState.Chase:
                ChaseLogic();
                break;
        }
    }

    #region Patrol & Chase Logic

    private void PatrolLogic()
    {
        // ตรวจจับขอบเหว และ กำแพงด้านหน้า
        bool isGroundedFront = Physics2D.Raycast(groundCheckPoint.position, Vector2.down, checkDistance, groundLayer);
        bool isWallFront = Physics2D.Raycast(wallCheckPoint.position, movingRight ? Vector2.right : Vector2.left, checkDistance, groundLayer);

        if (!isGroundedFront || isWallFront)
        {
            Flip();
        }

        float speed = movingRight ? patrolSpeed : -patrolSpeed;
        rb.linearVelocity = new Vector2(speed, rb.linearVelocity.y);
    }

    private void ChaseLogic()
    {
        if (playerTransform == null) return;

        // เช็คว่าผู้เล่นอยู่ทางซ้ายหรือขวา
        float direction = playerTransform.position.x - transform.position.x;

        if (direction > 0 && !movingRight) Flip();
        else if (direction < 0 && movingRight) Flip();

        // ตรวจสอบไม่ให้เดินตกเหวขณะวิ่งไล่ผู้เล่น
        bool isGroundedFront = Physics2D.Raycast(groundCheckPoint.position, Vector2.down, checkDistance, groundLayer);
        if (!isGroundedFront)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        float speed = movingRight ? chaseSpeed : -chaseSpeed;
        rb.linearVelocity = new Vector2(speed, rb.linearVelocity.y);
    }

    private void CheckForPlayer()
    {
        if (playerTransform == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer <= detectionRange)
        {
            currentState = EnemyState.Chase;
        }
        else if (currentState == EnemyState.Chase && distanceToPlayer > detectionRange * 1.3f)
        {
            // หากผู้เล่นหนีออกไปไกลเกินระยะ ให้กลับไปเดิน Patrol
            currentState = EnemyState.Patrol;
        }
    }

    private void Flip()
    {
        movingRight = !movingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    #endregion

    #region Combat & Damage Receiver

    /// <summary>
    /// ฟังก์ชันรับดาเมจจากผู้เล่น
    /// </summary>
    public void TakeDamage(int damage, Vector3 attackerPosition)
    {
        if (currentHealth <= 0) return;

        currentHealth -= damage;
        Debug.Log($"<color=orange>[ENEMY HIT] {gameObject.name} เหลือ HP: {currentHealth}/{maxHealth}</color>");

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(HitStunRoutine(attackerPosition));
        }
    }

    private IEnumerator HitStunRoutine(Vector3 attackerPosition)
    {
        currentState = EnemyState.Stunned;

        // คำนวณทิศทาง Knockback ถอยหลัง
        float knockbackDir = transform.position.x < attackerPosition.x ? -1f : 1f;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(new Vector2(knockbackDir * knockbackForce, knockbackForce * 0.5f), ForceMode2D.Impulse);

        // แสดง Effect สีแดงกระพริบตอนโดนตี
        if (spriteRenderer != null) spriteRenderer.color = Color.red;

        yield return new WaitForSeconds(stunDuration);

        if (spriteRenderer != null) spriteRenderer.color = Color.white;

        // กลับสู่สถานะ Chase ผู้เล่นทันทีหลังหายมึน
        currentState = EnemyState.Chase;
    }

    private void Die()
    {
        Debug.Log($"<color=red><b>[ENEMY DIED] {gameObject.name} พ่ายแพ้!</b></color>");
        
        // ปิด Collider และทำลาย GameObject
        GetComponent<Collider2D>().enabled = false;
        this.enabled = false;
        Destroy(gameObject, 0.5f);
    }

    #endregion

    #region Debug Gizmos

    private void OnDrawGizmosSelected()
    {
        // วาดวงกลมระยะตรวจจับ Player
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // วาดเส้น Raycast ตรวจจับพื้นและกำแพง
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