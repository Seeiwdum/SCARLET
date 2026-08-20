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

    [Header("Warp & Platform Jump Settings")]
    [SerializeField] private float warpTravelDuration = 0.1f;
    [SerializeField] private float swordVaultBonusForce = 18f;
    [SerializeField] private float flashDuration = 0.08f;
    [SerializeField] private Material flashWhiteMaterial; // ลาก Material สีขาวล้วนมาใส่ (หรือเว้นว่างไว้จะใช้ Boost Color)

    [Header("Juice: Squash & Stretch Settings")]
    [SerializeField] private Transform spriteTransform;
    [SerializeField] private Vector3 jumpStretch = new Vector3(0.75f, 1.25f, 1f);
    [SerializeField] private Vector3 landSquash = new Vector3(1.25f, 0.75f, 1f);
    [SerializeField] private float squashRecoverySpeed = 10f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.6f, 0.1f);
    [SerializeField] private LayerMask groundLayer;

    // Components & References
    private Rigidbody2D rb;
    private Collider2D col;
    private SpriteRenderer spriteRenderer;
    private Animator anim;
    private Material defaultMaterial;

    // State Variables
    private float horizontalInput;
    private bool isFacingRight = true;
    private bool isGrounded;
    private bool wasGroundedLastFrame;
    private int jumpsRemaining;
    private float coyoteTimer;
    private float jumpBufferTimer;
    private bool isDashing;
    private bool isWarping;
    private bool canDash = true;
    private bool isOverheated = false;
    private bool isOnSwordPlatform = false;
    private ThrownSword currentSwordPlatform;

    private Coroutine squashCoroutine;
    private Coroutine warpCoroutine;
    private Coroutine overheatCoroutine;

    // Encapsulated Properties
    public bool IsFacingRight => isFacingRight;
    public bool IsGroundedCheck => isGrounded || isOnSwordPlatform;
    public bool IsDashing => isDashing || isWarping;
    public bool IsOverheated => isOverheated;
    public Collider2D PlayerCollider => col;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        anim = GetComponent<Animator>();
        if (anim == null) anim = GetComponentInChildren<Animator>();

        if (spriteTransform == null && spriteRenderer != null) spriteTransform = spriteRenderer.transform;
        if (spriteRenderer != null) defaultMaterial = spriteRenderer.material;

        moveSpeed = baseMoveSpeed;
        rb.gravityScale = gravityScale;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation; // ล็อกไม่ให้ตัวละครหมุนเอียง
    }

    private void Update()
    {
        if (isDashing || isWarping) return;

        horizontalInput = Input.GetAxisRaw("Horizontal");
        CheckGrounded();
        UpdateAnimationStates();

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

        // เช็กทิศทางการหันหน้า
        if (horizontalInput > 0 && !isFacingRight) Flip();
        else if (horizontalInput < 0 && isFacingRight) Flip();

        ApplyGravityAdjustments();
    }

    private void FixedUpdate()
    {
        if (isDashing || isWarping) return;
        ApplyMovement();
    }

    #region Movement & Animation

    private void UpdateAnimationStates()
    {
        if (anim == null) return;
        anim.SetFloat("Speed", Mathf.Abs(horizontalInput));
        anim.SetBool("IsGrounded", IsGrounded());
        anim.SetFloat("VerticalVelocity", GetVelocityY());
    }

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

    public bool IsGrounded() => isGrounded || isOnSwordPlatform;

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
        if (isOnSwordPlatform)
        {
            ExecuteSwordPlatformJump();
            coyoteTimer = 0f;
            jumpBufferTimer = 0f;
            return;
        }

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
        if (anim != null) anim.SetTrigger("Jump");
        TriggerSquashAndStretch(jumpStretch);
    }

    private IEnumerator DashRoutine()
    {
        canDash = false;
        isDashing = true;

        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0f;

        float dashDir = isFacingRight ? 1f : -1f;
        SetVelocity(dashDir * dashSpeed, 0f);

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
        if (spriteTransform != null)
        {
            Vector3 s = spriteTransform.localScale;
            s.x = isFacingRight ? Mathf.Abs(s.x) : -Mathf.Abs(s.x);
            spriteTransform.localScale = s;
        }
        else if (spriteRenderer != null)
        {
            spriteRenderer.flipX = !isFacingRight;
        }
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
        spriteTransform.localScale = new Vector3(
            (isFacingRight ? 1f : -1f) * Mathf.Abs(squashedScale.x),
            squashedScale.y,
            squashedScale.z
        );

        while (true)
        {
            Vector3 currentTargetScale = new Vector3(isFacingRight ? 1f : -1f, 1f, 1f);

            spriteTransform.localScale = Vector3.Lerp(
                spriteTransform.localScale, 
                currentTargetScale, 
                Time.deltaTime * squashRecoverySpeed
            );

            if (Mathf.Abs(Mathf.Abs(spriteTransform.localScale.x) - 1f) < 0.01f &&
                Mathf.Abs(spriteTransform.localScale.y - 1f) < 0.01f)
            {
                spriteTransform.localScale = currentTargetScale;
                break;
            }

            yield return null;
        }
    }

    #endregion

    #region Sword Platform, Warp & Bounce Mechanics

    public void ExecuteFireWarp(Vector3 targetPosition, ThrownSword sword)
    {
        if (warpCoroutine != null) StopCoroutine(warpCoroutine);
        warpCoroutine = StartCoroutine(WarpDashRoutine(targetPosition, sword));
    }

    private IEnumerator WarpDashRoutine(Vector3 targetPosition, ThrownSword sword)
    {
        isWarping = true;
        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0f;
        SetVelocity(0f, 0f);

        Vector3 startPos = transform.position;
        float elapsed = 0f;
        VisualEffectsManager.Instance?.StartGhostTrail(spriteRenderer, warpTravelDuration);

        while (elapsed < warpTravelDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Sin((elapsed / warpTravelDuration) * Mathf.PI * 0.5f);
            transform.position = Vector3.Lerp(startPos, targetPosition, t);
            yield return null;
        }

        transform.position = targetPosition;
        rb.gravityScale = originalGravity;
        isWarping = false;

        // สลับดาบเป็นแพลตฟอร์มยืน
        if (sword != null)
        {
            sword.ConvertToPlatform();
            MountSwordPlatform(sword);
        }
    }

    private void ExecuteSwordPlatformJump()
    {
        SetVelocity(GetVelocityX(), swordVaultBonusForce);
        ResetJumps();
        StartCoroutine(FlashWhiteRoutine());

        if (anim != null) anim.SetTrigger("Jump");
        TriggerSquashAndStretch(new Vector3(0.6f, 1.4f, 1f));

        if (currentSwordPlatform != null)
        {
            currentSwordPlatform.BreakSword();
            currentSwordPlatform = null;
        }
        isOnSwordPlatform = false;
        GameEvents.OnSwordVaultPerformed?.Invoke();
    }

    public void MountSwordPlatform(ThrownSword sword)
    {
        isOnSwordPlatform = true;
        currentSwordPlatform = sword;
        ResetJumps();
    }

    public void DismountSwordPlatform()
    {
        isOnSwordPlatform = false;
        currentSwordPlatform = null;
    }

    public void ExecuteSwordVaultBounce(float bounceForce)
    {
        SetVelocity(GetVelocityX(), bounceForce);
        ResetJumps();
        TriggerSquashAndStretch(new Vector3(0.6f, 1.4f, 1f));
        GameEvents.OnSwordVaultPerformed?.Invoke();
    }

    // เมธอด Bounce สำหรับ HitboxTrigger.cs
    public void Bounce(float bounceForce)
    {
        SetVelocity(GetVelocityX(), bounceForce);
        jumpsRemaining = maxJumps - 1;
        TriggerSquashAndStretch(jumpStretch);
    }

    private IEnumerator FlashWhiteRoutine()
    {
        if (spriteRenderer == null) yield break;

        Color originalColor = spriteRenderer.color;
        if (flashWhiteMaterial != null) spriteRenderer.material = flashWhiteMaterial;
        else spriteRenderer.color = Color.white * 2f;

        yield return new WaitForSeconds(flashDuration);

        if (flashWhiteMaterial != null) spriteRenderer.material = defaultMaterial;
        spriteRenderer.color = originalColor;
    }

    public void ResetJumps() => jumpsRemaining = maxJumps;

    #endregion

    #region Buffs & Overheat (สำหรับ PlayerCombatSystem.cs)

    public void ApplySpeedBuff(float multiplier) => moveSpeed = baseMoveSpeed * multiplier;
    public void RemoveSpeedBuff() => moveSpeed = baseMoveSpeed;

    public void ApplyOverheatPenalty(float duration, float slowMultiplier)
    {
        if (overheatCoroutine != null) StopCoroutine(overheatCoroutine);
        overheatCoroutine = StartCoroutine(OverheatRoutine(duration, slowMultiplier));
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