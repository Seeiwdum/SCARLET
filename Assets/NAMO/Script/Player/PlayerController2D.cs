using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class PlayerController2D : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float baseMoveSpeed = 9f;
    private float moveSpeed;
    [SerializeField] private float acceleration = 60f;
    [SerializeField] private float deceleration = 60f;

    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 16f;
    [SerializeField] private int maxJumps = 2;
    [SerializeField] private float jumpCutMultiplier = 0.5f;
    [SerializeField] private float coyoteTime = 0.15f;
    [SerializeField] private float jumpBufferTime = 0.15f;
    [SerializeField] private float gravityScale = 3.5f;
    [SerializeField] private float fallGravityMultiplier = 1.5f;

    [Header("Dash Settings")]
    [SerializeField] private float dashSpeed = 22f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 0.6f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.6f, 0.1f);
    [SerializeField] private LayerMask groundLayer;

    // Components & Private Variables
    private Rigidbody2D rb;
    private Collider2D col;
    private float horizontalInput;
    private bool isFacingRight = true;
    private bool isGrounded;
    private int jumpsRemaining;
    private float coyoteTimer;
    private float jumpBufferTimer;
    private bool isDashing;
    private bool canDash = true;
    
    // 🔥 ตัวแปรเก็บสถานะ Overheat ที่เพิ่มกลับมา
    private bool isOverheated = false; 

    // Public Properties สำหรับส่งให้ระบบอื่นเช็ก
    public bool IsFacingRight => isFacingRight;
    public bool IsGroundedCheck => isGrounded;
    public bool IsDashing => isDashing;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        moveSpeed = baseMoveSpeed;
        rb.gravityScale = gravityScale;
    }

    private void Update()
    {
        if (isDashing) return;

        horizontalInput = Input.GetAxisRaw("Horizontal");

        CheckGrounded();

        if (Input.GetButtonDown("Jump"))
        {
            jumpBufferTimer = jumpBufferTime;
        }
        else
        {
            jumpBufferTimer -= Time.deltaTime;
        }

        if (jumpBufferTimer > 0f)
        {
            TryJump();
        }

        if (Input.GetButtonUp("Jump") && GetVelocityY() > 0f)
        {
            SetVelocity(GetVelocityX(), GetVelocityY() * jumpCutMultiplier);
            coyoteTimer = 0f;
        }

        if (Input.GetKeyDown(KeyCode.LeftShift) && canDash && !isDashing)
        {
            StartCoroutine(DashRoutine());
        }

        if (horizontalInput > 0 && !isFacingRight)
        {
            Flip();
        }
        else if (horizontalInput < 0 && isFacingRight)
        {
            Flip();
        }

        ApplyGravityAdjustments();
    }

    private void FixedUpdate()
    {
        if (isDashing) return;
        ApplyMovement();
    }

    #region Movement & Physics

    private void CheckGrounded()
    {
        if (groundCheck != null)
        {
            isGrounded = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer);
        }

        if (isGrounded)
        {
            coyoteTimer = coyoteTime;
            jumpsRemaining = maxJumps;
        }
        else
        {
            coyoteTimer -= Time.deltaTime;
        }
    }

    public bool IsGrounded() => isGrounded;

    private void ApplyMovement()
    {
        float targetSpeed = horizontalInput * moveSpeed;
        float speedDif = targetSpeed - GetVelocityX();
        float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;
        float movement = speedDif * accelRate;

        rb.AddForce(movement * Vector2.right, ForceMode2D.Force);
    }

    private void TryJump()
    {
        if (coyoteTimer > 0f)
        {
            ExecuteJump();
            coyoteTimer = 0f;
            jumpBufferTimer = 0f;
        }
        else if (jumpsRemaining > 0)
        {
            ExecuteJump();
            jumpBufferTimer = 0f;
        }
    }

    private void ExecuteJump()
    {
        SetVelocity(GetVelocityX(), jumpForce);
        jumpsRemaining--;
    }

    private IEnumerator DashRoutine()
    {
        canDash = false;
        isDashing = true;

        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0f;

        float dashDir = isFacingRight ? 1f : -1f;
        SetVelocity(dashDir * dashSpeed, 0f);

        yield return new WaitForSeconds(dashDuration);

        rb.gravityScale = originalGravity;
        isDashing = false;

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    private void ApplyGravityAdjustments()
    {
        if (GetVelocityY() < 0) rb.gravityScale = gravityScale * fallGravityMultiplier;
        else rb.gravityScale = gravityScale;
    }

    private void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    #endregion

    #region Sword Vault & Warp Mechanics

    public void ExecuteSwordVaultBounce(float bounceForce)
    {
        SetVelocity(GetVelocityX(), bounceForce);
        ResetJumps();
        Debug.Log("<color=orange>[SWORD VAULT] กระโดดเด้งตัวจากดาบสำเร็จ!</color>");
    }

    public void ExecuteFireWarp(Vector3 targetPosition)
    {
        transform.position = targetPosition;
        SetVelocity(0f, 0f);
        ResetJumps();
        Debug.Log("<color=cyan>[FIRE WARP] วาร์ปไปหาดาบสำเร็จ!</color>");
    }

    public void ResetJumps()
    {
        jumpsRemaining = maxJumps;
    }

    public void Bounce(float bounceForce)
    {
        SetVelocity(GetVelocityX(), bounceForce);
        jumpsRemaining = maxJumps - 1;
    }

    #endregion

    #region Pure Flame & Buff Support

    // 🔥 เพิ่มเมธอดนี้กลับมาให้ PlayerCombatSystem เรียกใช้
    public bool IsOverheated => isOverheated;

    public void ApplySpeedBuff(float multiplier)
    {
        moveSpeed = baseMoveSpeed * multiplier;
    }

    public void RemoveSpeedBuff()
    {
        moveSpeed = baseMoveSpeed;
    }

    public void ApplyOverheatPenalty(float slowMultiplier, float duration)
    {
        StartCoroutine(OverheatRoutine(slowMultiplier, duration));
    }

    private IEnumerator OverheatRoutine(float slowMultiplier, float duration)
    {
        isOverheated = true; // เปิดสถานะ Overheat
        moveSpeed = baseMoveSpeed * slowMultiplier;
        
        yield return new WaitForSeconds(duration);
        
        moveSpeed = baseMoveSpeed;
        isOverheated = false; // ปิดสถานะ Overheat
    }

    #endregion

    #region Helper Methods

    private float GetVelocityX() => rb.velocity.x;
    private float GetVelocityY() => rb.velocity.y;
    private void SetVelocity(float x, float y) => rb.velocity = new Vector2(x, y);

    #endregion

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
        }
    }
}