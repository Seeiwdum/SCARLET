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

    [Header("Juice: Squash & Stretch Settings")]
    [SerializeField] private Transform spriteTransform; // Drag ส่วน Sprite ของ Player มาใส่
    [SerializeField] private Vector3 jumpStretch = new Vector3(0.75f, 1.25f, 1f);
    [SerializeField] private Vector3 landSquash = new Vector3(1.25f, 0.75f, 1f);
    [SerializeField] private float squashRecoverySpeed = 10f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.6f, 0.1f);
    [SerializeField] private LayerMask groundLayer;

    public Collider2D PlayerCollider => col; // 🔥 เพิ่มบรรทัดนี้เข้ามา

    // Components & Private Variables

    private Animator anim;
    private Rigidbody2D rb;
    private Collider2D col;
    private SpriteRenderer spriteRenderer;
    private float horizontalInput;
    private bool isFacingRight = true;
    private bool isGrounded;
    private bool wasGroundedLastFrame;
    private int jumpsRemaining;
    private float coyoteTimer;
    private float jumpBufferTimer;
    private bool isDashing;
    private bool canDash = true;
    private bool isOverheated = false;
    private bool isKnockedBack = false;
    private Coroutine squashCoroutine;

    public bool IsFacingRight => isFacingRight;
    public bool IsGroundedCheck => isGrounded;
    public bool IsDashing => isDashing;
    public bool IsOverheated => isOverheated;

    public void SetKnockbackState(bool state) => isKnockedBack = state;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        anim = GetComponentInChildren<Animator>(); // ค้นหา Animator จาก Capsule
        if (spriteTransform == null && spriteRenderer != null) spriteTransform = spriteRenderer.transform;
        
        moveSpeed = baseMoveSpeed;
        rb.gravityScale = gravityScale;
    }

    private void Update()
    {
        if (isDashing || isKnockedBack) return;

        horizontalInput = Input.GetAxisRaw("Horizontal");
        CheckGrounded();

        if (Input.GetButtonDown("Jump")) jumpBufferTimer = jumpBufferTime;
        else jumpBufferTimer -= Time.deltaTime;

        if (jumpBufferTimer > 0f) TryJump();

        if (Input.GetButtonUp("Jump") && GetVelocityY() > 0f)
        {
            SetVelocity(GetVelocityX(), GetVelocityY() * jumpCutMultiplier);
            coyoteTimer = 0f;
        }

        if (Input.GetKeyDown(KeyCode.LeftShift) && canDash && !isDashing)
        {
            StartCoroutine(DashRoutine());
        }

        if (horizontalInput > 0 && !isFacingRight) Flip();
        else if (horizontalInput < 0 && isFacingRight) Flip();

        ApplyGravityAdjustments();

        // 2. ส่งค่าเข้า Animator ทุกเฟรม (แก้บั๊ก Airborne ค้างและเดินไม่ได้)
        UpdateAnimationParameters();
    }

    private void FixedUpdate()
    {
        if (isDashing || isKnockedBack) return;
        ApplyMovement();
    }

    private void UpdateAnimationParameters()
    {
        if (anim == null) return;

        float normalizedSpeed = Mathf.Clamp01(Mathf.Abs(GetVelocityX()) / baseMoveSpeed);
        anim.SetFloat("MoveSpeed", normalizedSpeed);
        anim.SetFloat("VerticalVelocity", GetVelocityY());
        anim.SetBool("IsGrounded", isGrounded);
        anim.SetBool("IsDashing", isDashing);
    }

    #region Movement & Physics

    private void CheckGrounded()
    {
        wasGroundedLastFrame = isGrounded;
        if (groundCheck != null)
        {
            isGrounded = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer);
        }

        if (isGrounded)
        {
            coyoteTimer = coyoteTime;
            jumpsRemaining = maxJumps;

            // จังหวะเท้าแตะพื้น (Landing Squash!)
            if (!wasGroundedLastFrame && GetVelocityY() <= 0.1f)
            {
                TriggerSquashAndStretch(landSquash);
            }
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
        if (coyoteTimer > 0f || jumpsRemaining > 0)
        {
            ExecuteJump();
            coyoteTimer = 0f;
            jumpBufferTimer = 0f;
        }
    }

    private void ExecuteJump()
    {
        SetVelocity(GetVelocityX(), jumpForce);
        jumpsRemaining--;
        TriggerSquashAndStretch(jumpStretch); // ยืดตัวตอนกระโดด
    }

    private IEnumerator DashRoutine()
    {
        canDash = false;
        isDashing = true;

        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0f;

        float dashDir = isFacingRight ? 1f : -1f;
        SetVelocity(dashDir * dashSpeed, 0f);

        // เสก Ghost Trail เงาตามตัว
        VisualEffectsManager.Instance?.StartGhostTrail(spriteRenderer, dashDuration);
        TriggerSquashAndStretch(new Vector3(1.3f, 0.7f, 1f));

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

    #region Juice: Squash & Stretch

    public void TriggerSquashAndStretch(Vector3 targetScale)
    {
        if (spriteTransform == null) return;
        if (squashCoroutine != null) StopCoroutine(squashCoroutine);
        squashCoroutine = StartCoroutine(SquashRoutine(targetScale));
    }

    private IEnumerator SquashRoutine(Vector3 squashedScale)
{
    // 1. ตรวจสอบว่าภาพ Sprite เป็นตัวเดียวกับ Root สคริปต์หรือไม่
    bool isRoot = (spriteTransform == transform);
    
    // 2. กำหนดทิศทางเริ่มต้น โดยอิงจากฝั่งที่หันหน้าอยู่ปัจจุบัน
    float startSign = isRoot ? (isFacingRight ? 1f : -1f) : 1f;
    spriteTransform.localScale = new Vector3(squashedScale.x * startSign, squashedScale.y, squashedScale.z);

    while (true)
    {
        // 3. อัปเดตเป้าหมายการยืดหดตัวทุกเฟรม (แก้บัคเมื่อผู้เล่นกดหันหลังกระทันหันกลางอากาศ)
        float targetSign = isRoot ? (isFacingRight ? 1f : -1f) : 1f;
        Vector3 targetScale = new Vector3(targetSign, 1f, 1f);

        spriteTransform.localScale = Vector3.Lerp(spriteTransform.localScale, targetScale, Time.deltaTime * squashRecoverySpeed);
        
        // 4. เมื่อยืดหดตัวเสร็จสมบูรณ์ ให้ออกจาก Loop อย่างปลอดภัย
        if (Vector3.Distance(spriteTransform.localScale, targetScale) < 0.01f)
        {
            spriteTransform.localScale = targetScale;
            break;
        }
        yield return null;
    }
}

    #endregion

    #region Sword Vault & Warp Mechanics

    public void ExecuteSwordVaultBounce(float bounceForce)
    {
        SetVelocity(GetVelocityX(), bounceForce);
        ResetJumps();
        TriggerSquashAndStretch(new Vector3(0.6f, 1.4f, 1f)); // ยืดตัวพุ่งสูง
        GameEvents.OnSwordVaultPerformed?.Invoke();
    }

    public void ExecuteFireWarp(Vector3 targetPosition)
    {
        transform.position = targetPosition;
        SetVelocity(0f, 0f);
        ResetJumps();
        VisualEffectsManager.Instance?.StartGhostTrail(spriteRenderer, 0.15f);
        TriggerSquashAndStretch(new Vector3(1.2f, 1.2f, 1f));
        GameEvents.OnSwordVaultPerformed?.Invoke();
    }

    public void ResetJumps() => jumpsRemaining = maxJumps;

    public void Bounce(float bounceForce)
    {
        SetVelocity(GetVelocityX(), bounceForce);
        jumpsRemaining = maxJumps - 1;
        TriggerSquashAndStretch(jumpStretch);
    }

    #endregion

    #region Buffs & Overheat

    public void ApplySpeedBuff(float multiplier) => moveSpeed = baseMoveSpeed * multiplier;
    public void RemoveSpeedBuff() => moveSpeed = baseMoveSpeed;

    public void ApplyOverheatPenalty(float duration, float slowMultiplier)
    {
        StartCoroutine(OverheatRoutine(duration, slowMultiplier));
    }

    private IEnumerator OverheatRoutine(float duration, float slowMultiplier)
    {
        isOverheated = true;
        moveSpeed = baseMoveSpeed * slowMultiplier;
        yield return new WaitForSeconds(duration);
        moveSpeed = baseMoveSpeed;
        isOverheated = false;
    }

    #endregion

    #region Velocity Helpers

    private float GetVelocityX() => rb.linearVelocity.x;
    private float GetVelocityY() => rb.linearVelocity.y;
    private void SetVelocity(float x, float y) => rb.linearVelocity = new Vector2(x, y);

    #endregion
}