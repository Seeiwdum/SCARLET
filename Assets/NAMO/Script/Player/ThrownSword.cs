using System.Collections;
using UnityEngine;

public class ThrownSword : MonoBehaviour, IDamageable
{
    public enum SwordThrowType { Forward, UpwardWarp, DownwardVault }

    [Header("Movement Settings")]
    [SerializeField] private float flightSpeed = 22f;
    [SerializeField] private float maxTravelDistance = 6.5f;
    [SerializeField] private LayerMask collisionLayers; // เลือก Layer พื้นและกำแพง (เช่น Ground, Wall, Obstacle)

    [Header("Floating & Juice Settings")]
    [SerializeField] private float bobbingSpeed = 6f;
    [SerializeField] private float bobbingAmount = 0.12f;
    [SerializeField] private float flashDuration = 0.08f;

    [Header("Warp Slash Settings")]
    [SerializeField] private float slashRadius = 2.2f;
    [SerializeField] private int slashDamage = 2;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Perch Settings (ยืนบนมีด)")]
    [SerializeField] private Vector3 perchOffset = new Vector3(0f, 1.1f, 0f);

    [Header("Timers")]
    [SerializeField] private float warpWindowDuration = 2.5f;
    [SerializeField] private float platformStandDuration = 2.0f;
    [SerializeField] private float vaultBounceForce = 18f;

    [Header("Dynamic Prompt")]
    [SerializeField] private SpriteRenderer promptSprite;
    [SerializeField] private Vector3 promptTargetScale = new Vector3(1f, 1f, 1f);
    [SerializeField] private float promptAnimSpeed = 10f;

    [Header("Components & VFX")]
    [SerializeField] private SpriteRenderer swordRenderer;
    [SerializeField] private Collider2D solidCollider;
    [SerializeField] private GameObject warpVFXPrefab;
    [SerializeField] private LineRenderer flameTether;

    private Vector3 startPos;
    private Vector2 flyDirection;
    private SwordThrowType throwType;
    private PlayerController2D playerRef;
    private bool isStopped = false;
    private bool isPlayerPerched = false;
    private Vector3 promptInitialLocalPos;
    private Vector3 baseStoppedPos;
    private Color originalSwordColor;

    public void Initialize(Vector2 direction, SwordThrowType type, PlayerController2D player)
    {
        flyDirection = direction.normalized;
        throwType = type;
        playerRef = player;
        startPos = transform.position;

        if (swordRenderer == null) swordRenderer = GetComponent<SpriteRenderer>();
        if (swordRenderer != null) originalSwordColor = swordRenderer.color;

        if (solidCollider != null) solidCollider.enabled = false;

        if (promptSprite != null)
        {
            promptInitialLocalPos = promptSprite.transform.localPosition;
            SetPromptAlpha(0f);
            promptSprite.transform.localScale = Vector3.zero;
        }

        float angle = Mathf.Atan2(flyDirection.y, flyDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        if (flameTether != null)
        {
            flameTether.positionCount = 2;
            flameTether.enabled = true;
        }
    }

    private void Update()
    {
        UpdateFlameTether();

        if (!isStopped)
        {
            float moveStep = flightSpeed * Time.deltaTime;

            // ตรวจจับการชนพื้น/กำแพงล่วงหน้าก่อนเคลื่อนที่จริง เพื่อป้องกันการพุ่งทะลุ
            RaycastHit2D hit = Physics2D.Raycast(transform.position, flyDirection, moveStep + 0.1f, collisionLayers);
            if (hit.collider != null)
            {
                transform.position = hit.point;
                StopSword();
                return;
            }

            transform.position += (Vector3)(flyDirection * moveStep);

            if (Vector3.Distance(startPos, transform.position) >= maxTravelDistance)
            {
                StopSword();
            }
        }
        else
        {
            HandleFloatingMotion();
            HandleInteractionInput();
        }
    }

    private void UpdateFlameTether()
    {
        if (flameTether != null && playerRef != null)
        {
            flameTether.SetPosition(0, playerRef.transform.position + new Vector3(0, 0.5f, 0));
            flameTether.SetPosition(1, transform.position);
        }
    }

    private void StopSword()
    {
        if (isStopped) return;
        isStopped = true;
        baseStoppedPos = transform.position;

        StartCoroutine(FlashWhiteRoutine());
        AnimatePrompt(true);

        if (throwType == SwordThrowType.DownwardVault)
        {
            if (solidCollider != null) solidCollider.enabled = true;
            SnapPlayerToPerch();
            StartCoroutine(PlatformTimeoutRoutine());
        }
        else
        {
            StartCoroutine(WarpWindowTimeoutRoutine());
        }
    }

    private void HandleFloatingMotion()
    {
        // ท่าปักพื้นไม่ต้องลอยขึ้นลง เพื่อให้ตำแหน่งยืนบนมีดมั่นคง
        if (throwType == SwordThrowType.DownwardVault) return;

        float offset = Mathf.Sin(Time.time * bobbingSpeed) * bobbingAmount;
        transform.position = baseStoppedPos + new Vector3(0f, offset, 0f);

        if (promptSprite != null)
        {
            promptSprite.transform.rotation = Quaternion.identity;
        }
    }

    private void HandleInteractionInput()
    {
        if (Input.GetKeyDown(KeyCode.J) || Input.GetMouseButtonDown(1))
        {
            if (throwType == SwordThrowType.DownwardVault)
            {
                ExecuteVault();
            }
            else
            {
                bool isAttackHeld = Input.GetKey(KeyCode.Z) || Input.GetMouseButton(0);
                ExecuteWarp(isAttackHeld);
            }
        }
    }

    private void SnapPlayerToPerch()
    {
        if (playerRef != null)
        {
            isPlayerPerched = true;
            playerRef.transform.position = transform.position + perchOffset;
            playerRef.ResetJumps();
            playerRef.SetKnockbackState(false);
        }
    }

    private void ExecuteWarp(bool isWarpSlash)
    {
        StopAllCoroutines();
        SpawnVFX();

        if (playerRef != null)
        {
            playerRef.ExecuteFireWarp(transform.position);

            if (isWarpSlash)
            {
                PerformWarpSlash();
            }
        }

        CleanupAndDestroy();
    }

    private void PerformWarpSlash()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, slashRadius, enemyLayer);
        foreach (var hit in hits)
        {
            IDamageable target = hit.GetComponent<IDamageable>();
            target?.TakeDamage(slashDamage, transform.position);
        }
        GameEvents.OnEnemyHit?.Invoke();
    }

    private void ExecuteVault()
    {
        StopAllCoroutines();
        SpawnVFX();

        if (playerRef != null)
        {
            playerRef.ExecuteSwordVaultBounce(vaultBounceForce);
        }

        CleanupAndDestroy();
    }

    private IEnumerator FlashWhiteRoutine()
    {
        if (swordRenderer != null)
        {
            swordRenderer.color = Color.white;
            yield return new WaitForSeconds(flashDuration);
            swordRenderer.color = originalSwordColor;
        }
    }

    private void CleanupAndDestroy()
    {
        if (flameTether != null) flameTether.enabled = false;
        Destroy(gameObject);
    }

    private void SpawnVFX()
    {
        if (warpVFXPrefab != null) Instantiate(warpVFXPrefab, transform.position, Quaternion.identity);
    }

    #region Dynamic Prompt Animation
    private void AnimatePrompt(bool show)
    {
        if (promptSprite == null) return;
        StartCoroutine(PromptRoutine(show));
    }

    private IEnumerator PromptRoutine(bool show)
    {
        float targetAlpha = show ? 1f : 0f;
        Vector3 finalScale = show ? promptTargetScale : Vector3.zero;

        while (Mathf.Abs(promptSprite.color.a - targetAlpha) > 0.02f)
        {
            SetPromptAlpha(Mathf.Lerp(promptSprite.color.a, targetAlpha, Time.deltaTime * promptAnimSpeed));
            promptSprite.transform.localScale = Vector3.Lerp(promptSprite.transform.localScale, finalScale, Time.deltaTime * (promptAnimSpeed * 1.2f));
            yield return null;
        }

        SetPromptAlpha(targetAlpha);
        promptSprite.transform.localScale = finalScale;
    }

    private void SetPromptAlpha(float alpha)
    {
        if (promptSprite != null)
        {
            Color c = promptSprite.color;
            c.a = alpha;
            promptSprite.color = c;
        }
    }
    #endregion

    private IEnumerator WarpWindowTimeoutRoutine()
    {
        yield return new WaitForSeconds(warpWindowDuration);
        AnimatePrompt(false);
        yield return new WaitForSeconds(0.2f);
        CleanupAndDestroy();
    }

    private IEnumerator PlatformTimeoutRoutine()
    {
        yield return new WaitForSeconds(platformStandDuration);
        AnimatePrompt(false);
        yield return new WaitForSeconds(0.2f);
        CleanupAndDestroy();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // ตรวจจับเมื่อชนเข้ากับ Layer พื้น/กำแพง
        if (((1 << other.gameObject.layer) & collisionLayers) != 0)
        {
            StopSword();
        }
    }

    public void TakeDamage(int damage, Vector3 sourcePosition)
    {
        ExecuteVault();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, slashRadius);
    }
}