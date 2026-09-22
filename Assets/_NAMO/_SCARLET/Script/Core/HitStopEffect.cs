using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HitStopEffect : MonoBehaviour
{
    private static HitStopEffect _instance;
    public static HitStopEffect Instance 
    { 
        get 
        { 
            if (_instance == null) 
            {
                GameObject go = new GameObject("HitStopEffect_AutoCreated");
                _instance = go.AddComponent<HitStopEffect>();
            }
            return _instance; 
        } 
    }

    [Header("Flash Effect")]
    [Tooltip("ใส่ Image สีขาวเต็มจอที่ปรับ Alpha เป็น 0 ไว้")]
    [SerializeField] private Image whiteFlashImage;
    [SerializeField] private float flashFadeSpeed = 5f;

    private void Awake()
    {
        if (_instance == null) _instance = this;
        else if (_instance != this) Destroy(gameObject);
    }

    private void OnEnable()
    {
        GameEvents.OnEnemyHit += HandleEnemyHit;
        GameEvents.OnSwordVaultPerformed += HandleEnemyHit;
    }

    private void OnDisable()
    {
        GameEvents.OnEnemyHit -= HandleEnemyHit;
        GameEvents.OnSwordVaultPerformed -= HandleEnemyHit;
    }

    private void HandleEnemyHit() => ApplyHitPause(0.06f);

    // Domain operations - concrete VFX objects per tech lead
    public void ApplyHitPause(float duration = 0.06f) => TriggerHit(duration);
    public void ApplyParryFreeze(float duration = 0.15f) => TriggerParryHitStop(duration);

    /// <summary>
    /// Domain: Parry Freeze (ZZZ) + ScreenFlashFX
    /// </summary>
    /// <param name="stopDuration">ระยะเวลาค้างหน้าจอ (Realtime)</param>
    public void TriggerParryHitStop(float stopDuration = 0.15f)
    {
        StopAllCoroutines();
        StartCoroutine(HitStopRoutine(stopDuration));
        StartCoroutine(ScreenFlashRoutine());
    }

    /// <summary>
    /// เรียกใช้เมื่อโจมตีโดนศัตรูปกติ (หยุดสั้นๆ ไม่ใช่ ZZZ Slow-Mo เต็มๆ)
    /// </summary>
    public void TriggerHit(float duration = 0.06f)
    {
        // Don't interrupt an ongoing parry ZZZ slow-mo
        if (Time.timeScale < 0.5f) return;
        StopAllCoroutines();
        StartCoroutine(QuickHitStopRoutine(duration));
    }

    private IEnumerator QuickHitStopRoutine(float duration)
    {
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
    }

    private IEnumerator HitStopRoutine(float duration)
    {
        // 1. ZZZ Style: หยุดเวลาแทบสนิททันที (0.005f) ให้ภาพแทบจะค้าง 
        Time.timeScale = 0.005f;
        
        // 2. ค้างเฟรมแห่ง Impact นานขึ้นเพื่อความสะใจ (0.25 วินาที realtime)
        yield return new WaitForSecondsRealtime(0.25f);
        
        // 3. Smooth Cinema Recovery: ค่อยๆ เร่งเวลากลับมาอย่างเท่ๆ
        while (Time.timeScale < 1f)
        {
            Time.timeScale += Time.unscaledDeltaTime * 1.5f; 
            if (Time.timeScale > 1f) Time.timeScale = 1f;
            yield return null;
        }
    }

    private IEnumerator ScreenFlashRoutine()
    {
        if (whiteFlashImage == null) yield break;

        // สว่างวาบทันที
        Color c = whiteFlashImage.color;
        c.a = 0.8f;
        whiteFlashImage.color = c;

        // ค่อยๆ จางหายไป
        while (whiteFlashImage.color.a > 0.01f)
        {
            c.a = Mathf.MoveTowards(whiteFlashImage.color.a, 0f, Time.unscaledDeltaTime * flashFadeSpeed);
            whiteFlashImage.color = c;
            yield return null;
        }
        c.a = 0f;
        whiteFlashImage.color = c;
    }

    // ScreenFlashFX domain object encapsulation
    public void FlashScreen(float peakAlpha = 0.8f) => StartCoroutine(ScreenFlashRoutine());
}

// Legacy alias for prefabs / code still referencing old name
[System.Obsolete("Use HitStopEffect")] public class HitStopManager : HitStopEffect {}