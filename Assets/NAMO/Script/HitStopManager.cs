using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HitStopManager : MonoBehaviour
{
    public static HitStopManager Instance { get; private set; }

    [Header("Flash Effect")]
    [Tooltip("ใส่ Image สีขาวเต็มจอที่ปรับ Alpha เป็น 0 ไว้")]
    [SerializeField] private Image whiteFlashImage;
    [SerializeField] private float flashFadeSpeed = 5f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
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
        // 1. หยุดเวลาเกือบสนิททันที (ZZZ Style)
        Time.timeScale = 0.01f;
        
        // 2. ค้างจังหวะเฟรมหยุดไว้แบบรู้สึกได้ชัดเจน
        yield return new WaitForSecondsRealtime(duration);
        
        // 3. ค่อยๆ สโลว์กลับอย่างช้าๆ (Smooth Cinema Recovery) แบบภาพยนตร์
        while (Time.timeScale < 1f)
        {
            Time.timeScale += Time.unscaledDeltaTime * 1.25f; 
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