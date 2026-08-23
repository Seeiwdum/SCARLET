using System.Collections;
using UnityEngine;

public class ThrownSword : MonoBehaviour, IDamageable
{
    public enum SwordThrowType { Forward, UpwardWarp, DownwardVault }

    [Header("Movement Settings")]
    [SerializeField] private float flightSpeed = 22f;
    [SerializeField] private float maxTravelDistance = 6.5f;

    [Header("Timers")]
    [Tooltip("ระยะเวลารอให้กด Warp กลางอากาศ (ปาขึ้น/หน้า)")]
    [SerializeField] private float warpWindowDuration = 2.0f;

    [Tooltip("ระยะเวลาที่ยืนพักบนดาบได้ก่อนดาบสลาย (ปาลงล่าง)")]
    [SerializeField] private float platformStandDuration = 1.8f;

    [Header("Vault Settings")]
    [Tooltip("แรงเด้งขึ้นฟ้าเมื่อกด Interact หรือฟันใส่ดาบ")]
    [SerializeField] private float vaultBounceForce = 18f;

    [Header("Dynamic Prompt (Fade-in & ขยายนูน)")]
    [Tooltip("SpriteRenderer ของปุ่มกด (เช่น ปุ่ม J) ลอยอยู่เหนือดาบ")]
    [SerializeField] private SpriteRenderer promptSprite;
    [SerializeField] private Vector3 promptTargetScale = new Vector3(1f, 1f, 1f);
    [SerializeField] private float promptAnimSpeed = 10f;

    [Header("Prompt Bobbing (ลอยขึ้น-ลงอย่างนุ่มนวล)")]
    [SerializeField] private float bobbingSpeed = 6f;
    [SerializeField] private float bobbingAmount = 0.15f;

    [Header("Colliders & VFX")]
    [SerializeField] private Collider2D triggerCollider;  // Trigger เช็กชน/สัมผัส
    [SerializeField] private Collider2D solidCollider;    // Platform แข็งสำหรับยืนพัก
    [SerializeField] private GameObject warpVFXPrefab;

    private Vector3 startPos;
    private Vector2 flyDirection;
    private SwordThrowType throwType;
    private PlayerController2D playerRef;
    private bool isStopped = false;
    private bool isPlayerTouching = false;
    private Coroutine promptCoroutine;
    private Vector3 promptInitialLocalPos;

    public void Initialize(Vector2 direction, SwordThrowType type, PlayerController2D player)
    {
        flyDirection = direction.normalized;
        throwType = type;
        playerRef = player;
        startPos = transform.position;

        if (solidCollider != null) solidCollider.enabled = false;

        // ซ่อน Prompt ไว้ก่อนตอนเริ่มปา
        if (promptSprite != null)
        {
            promptInitialLocalPos = promptSprite.transform.localPosition;
            SetPromptAlpha(0f);
            promptSprite.transform.localScale = Vector3.zero;
            promptSprite.transform.rotation = Quaternion.identity;
        }

        // หมุนตัวดาบตามทิศทางการปา
        float angle = Mathf.Atan2(flyDirection.y, flyDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private void Update()
    {
        if (!isStopped)
        {
            transform.position += (Vector3)(flyDirection * flightSpeed * Time.deltaTime);

            if (Vector3.Distance(startPos, transform.position) >= maxTravelDistance)
            {
                StopSword();
            }
        }
        else
        {
            HandleInteractionInput();
            HandlePromptFloating();
        }
    }

    private void StopSword()
    {
        if (isStopped) return;
        isStopped = true;

        // เริ่มแอนิเมชัน Fade-in และขยายปุ่มขึ้นมา
        AnimatePrompt(true);

        if (throwType == SwordThrowType.DownwardVault)
        {
            if (solidCollider != null) solidCollider.enabled = true;
            StartCoroutine(PlatformTimeoutRoutine());
        }
        else
        {
            StartCoroutine(WarpWindowTimeoutRoutine());
        }
    }

    private void HandleInteractionInput()
    {
        // ปุ่มปาดาบ/Interact (J หรือคลิกขวา)
        if (Input.GetKeyDown(KeyCode.J) || Input.GetMouseButtonDown(1))
        {
            if (throwType == SwordThrowType.DownwardVault)
            {
                if (isPlayerTouching) ExecuteVault();
            }
            else
            {
                ExecuteWarp();
            }
        }
    }

    private void HandlePromptFloating()
    {
        if (promptSprite == null) return;

        // ล็อกไม่ให้ Prompt เอียงตามมุมดาบ
        promptSprite.transform.rotation = Quaternion.identity;

        // คำนวณการลอยขึ้น-ลงแบบ Sine Wave
        float newY = promptInitialLocalPos.y + (Mathf.Sin(Time.time * bobbingSpeed) * bobbingAmount);
        promptSprite.transform.localPosition = new Vector3(promptInitialLocalPos.x, newY, promptInitialLocalPos.z);
    }

    private void ExecuteWarp()
    {
        StopAllCoroutines();
        SpawnVFX();

        if (playerRef != null)
        {
            playerRef.ExecuteFireWarp(transform.position);
        }

        Destroy(gameObject);
    }

    private void ExecuteVault()
    {
        StopAllCoroutines();
        SpawnVFX();

        if (playerRef != null)
        {
            playerRef.ExecuteSwordVaultBounce(vaultBounceForce);
        }

        Destroy(gameObject);
    }

    private void SpawnVFX()
    {
        if (warpVFXPrefab != null)
        {
            Instantiate(warpVFXPrefab, transform.position, Quaternion.identity);
        }
    }

    #region Dynamic Prompt Animation

    private void AnimatePrompt(bool show)
    {
        if (promptSprite == null) return;
        if (promptCoroutine != null) StopCoroutine(promptCoroutine);
        promptCoroutine = StartCoroutine(PromptRoutine(show));
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
        Destroy(gameObject);
    }

    private IEnumerator PlatformTimeoutRoutine()
    {
        yield return new WaitForSeconds(platformStandDuration);
        AnimatePrompt(false);
        yield return new WaitForSeconds(0.2f);
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerTouching = true;
            if (throwType == SwordThrowType.DownwardVault && !isStopped)
            {
                StopSword();
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerTouching = false;
        }
    }

    // รองรับ Polymorphism: หากใช้การโจมตีฟันใส่ดาบ จะสั่งเด้งตัว Vault
    public void TakeDamage(int damage, Vector3 sourcePosition)
    {
        ExecuteVault();
    }
}