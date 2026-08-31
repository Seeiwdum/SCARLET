using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HitStopManager : MonoBehaviour
{
    private static HitStopManager _instance;
    public static HitStopManager Instance 
    { 
        get 
        { 
            if (_instance == null) 
            {
                GameObject go = new GameObject("HitStopManager_AutoCreated");
                _instance = go.AddComponent<HitStopManager>();
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

    /// <summary>
    /// เรียกใช้เมื่อ Parry ติด
    /// </summary>
    /// <param name="stopDuration">ระยะเวลาค้างหน้าจอ (Realtime)</param>
    public void TriggerParryHitStop(float stopDuration = 0.15f)
    {
        StopAllCoroutines();
        StartCoroutine(HitStopRoutine(stopDuration));
        StartCoroutine(ScreenFlashRoutine());
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
}