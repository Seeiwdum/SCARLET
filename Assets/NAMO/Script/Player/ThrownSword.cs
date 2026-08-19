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
    [SerializeField] private float vaultBounceForce = 18f;

    [Header("Dynamic Prompt (ปุ่ม Fade-in & เด้งนูนขึ้น)")]
    [Tooltip("SpriteRenderer ของปุ่มกด (เช่น ปุ่ม J) ลอยอยู่เหนือดาบ")]
    [SerializeField] private SpriteRenderer promptSprite;
    [SerializeField] private Vector3 promptTargetScale = new Vector3(1f, 1f, 1f);
    [SerializeField] private float promptAnimSpeed = 10f;

    [Header("Colliders & VFX")]
    [SerializeField] private Collider2D triggerCollider;  // Trigger เช็กชน
    [SerializeField] private Collider2D solidCollider;    // Platform แข็ง
    [SerializeField] private GameObject warpVFXPrefab;

    [Header("Prompt Bobbing (ลอยขึ้น-ลง)")]
    [SerializeField] private float bobbingSpeed = 6f;
    [SerializeField] private float bobbingAmount = 0.15f;
    private Vector3 promptInitialLocalPos;

    private Vector3 startPos;
    private Vector2 flyDirection;
    private SwordThrowType throwType;
    private PlayerController2D playerRef;
    private bool isStopped = false;
    private bool isPlayerTouching = false;
    private Coroutine promptCoroutine;

    public void Initialize(Vector2 direction, SwordThrowType type, PlayerController2D player)
    {
        flyDirection = direction.normalized;
        throwType = type;
        playerRef = player;
        startPos = transform.position;

        if (solidCollider != null) solidCollider.enabled = false;

        // ซ่อน Prompt ไว้ก่อน
        if (promptSprite != null)
        {
            SetPromptAlpha(0f);
            promptSprite.transform.localScale = Vector3.zero;
        }

        // หมุนหัวดาบตามทิศ
        float angle = Mathf.Atan2(flyDirection.y, flyDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
        
        // ให้ Prompt ตั้งตรงเสมอ ไม่หมุนเอียงตามดาบ
        if (promptSprite != null)
        {
            promptSprite.transform.rotation = Quaternion.identity;
        }
    }

    private void Update()
    {
        if (isStopped && promptSprite != null)
    {
        float newY = promptInitialLocalPos.y + Mathf.Sin(Time.time * bobbingSpeed) * bobbingAmount;
        promptSprite.transform.localPosition = new Vector3(promptInitialLocalPos.x, newY, promptInitialLocalPos.z);
    }
        else
        {
            HandleInteractionInput();
            
            // ล็อกไม่ให้ Prompt หมุนตามองศาของดาบ
            if (promptSprite != null)
            {
                promptSprite.transform.rotation = Quaternion.identity;
            }
        }
    }

    private void StopSword()
    {
        if (isStopped) return;
        isStopped = true;

        // ค่อยๆ Fade-in และทำปุ่มนูนขึ้นมาให้กด!
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
        // ผู้เล่นกดปุ่ม J เพื่อสั่ง Warp หรือเด้งตัว
        if (Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(1))
        {
            if (throwType == SwordThrowType.DownwardVault)
            {
                if (isPlayerTouching) ExecuteVault();
            }
            else
            {
                // สำหรับ Forward และ Upward ให้กด Warp ได้ทันทีเมื่อดาบหยุด
                ExecuteWarp();
            }
        }
    }

    private void ExecuteWarp()
    {
        StopAllCoroutines();
        SpawnVFX();

        if (playerRef != null)
        {
            // สั่งวาร์ปตัวละครมาที่ตำแหน่งดาบ + คืนโควต้า Jump ให้กระโดดต่อได้!
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

    public void TakeDamage(int damage, Vector3 sourcePosition)
    {
        ExecuteVault();
    }
}