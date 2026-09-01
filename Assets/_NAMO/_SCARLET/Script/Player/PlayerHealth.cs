using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
// 1. นำ IDamageable มาสวมให้ PlayerHealth
public class PlayerHealth : MonoBehaviour, IDamageable 
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 5;
    [SerializeField] private int currentHealth;

    [Header("Invincibility (i-frames) Settings")]
    [SerializeField] private float invincibilityDuration = 1.5f;
    [SerializeField] private float flashInterval = 0.1f;
    private bool isInvincible = false;

    [Header("Knockback Settings")]
    [SerializeField] private Vector2 knockbackForce = new Vector2(8f, 10f);
    [SerializeField] private float knockbackDuration = 0.25f;
    private bool isKnockedBack = false;

    [Header("Components & References")]
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private PlayerController2D playerController;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsInvincible => isInvincible;
    public bool IsKnockedBack => isKnockedBack;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        playerController = GetComponent<PlayerController2D>();
    }

    private void Start()
    {
        currentHealth = maxHealth;
        // 2. ส่งค่าเริ่มต้นไปให้ระบบ UI
        GameEvents.OnPlayerHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    // 3. ฟังก์ชันนี้ตรงตามสัญญาของ IDamageable เป๊ะ
    public void TakeDamage(int damage, Vector3 damageSourcePosition)
    {
        if (isInvincible || currentHealth <= 0) return;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        // 4. ประกาศผ่านวิทยุสื่อสารว่า "ผู้เล่นโดนตีนะ เลือดเหลือเท่านี้!"
        GameEvents.OnPlayerHealthChanged?.Invoke(currentHealth, maxHealth);
        CameraController2D.Instance?.TriggerShake(0.2f, 0.4f);

        Debug.Log($"<color=red>[PLAYER HIT] เลือดเหลือ: {currentHealth}/{maxHealth}</color>");

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            float knockbackDirection = transform.position.x < damageSourcePosition.x ? -1f : 1f;
            StartCoroutine(ApplyKnockbackRoutine(knockbackDirection));
            StartCoroutine(InvincibilityRoutine());
        }
    }

    public void Heal(int amount)
    {
        if (currentHealth <= 0) return;

        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        
        // ส่งอัปเดตตอนฮีลด้วย
        GameEvents.OnPlayerHealthChanged?.Invoke(currentHealth, maxHealth);
        Debug.Log($"<color=green>[HEAL] ฟื้นฟูเลือด: +{amount} (ปัจจุบัน: {currentHealth}/{maxHealth})</color>");
    }

    private IEnumerator ApplyKnockbackRoutine(float directionX)
    {
        isKnockedBack = true;
        
        // เราจะไม่ปิด playerController.enabled แบบดื้อๆ เพราะมันจะไปปิด Input & Cooldown timers ทุกอย่าง!
        // ใช้ DisableControl แบบเฉพาะเจาะจง หรือถ้าไม่มี ให้พึ่งพา Velocity Override แทน
        if (playerController != null) playerController.SetKnockbackState(true);

        rb.linearVelocity = Vector2.zero; // Unity 6.3 syntax ถูกต้องแล้ว
        Vector2 force = new Vector2(directionX * knockbackForce.x, knockbackForce.y);
        rb.AddForce(force, ForceMode2D.Impulse);

        yield return new WaitForSeconds(knockbackDuration);

        if (playerController != null) playerController.SetKnockbackState(false);
        isKnockedBack = false;
    }

    private IEnumerator InvincibilityRoutine()
    {
        isInvincible = true;
        float timer = 0f;
        
        while (timer < invincibilityDuration)
        {
            if (spriteRenderer != null)
            {
                Color c = spriteRenderer.color;
                c.a = (c.a == 1f) ? 0.3f : 1f; 
                spriteRenderer.color = c;
            }
            yield return new WaitForSeconds(flashInterval);
            timer += flashInterval;
        }

        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = 1f;
            spriteRenderer.color = c;
        }
        isInvincible = false;
    }

    private void Die()
    {
        Debug.Log("<color=black><b>[GAME OVER] ตัวละครเสียชีวิต!</b></color>");
        if (playerController != null) playerController.enabled = false;
        
        // 5. ส่ง Event เพื่อบอก GameManager ให้ขึ้นจอ Game Over
        GameEvents.OnPlayerDied?.Invoke();
    }
}