using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PoisonFogPostProcess : MonoBehaviour
{
    [Header("UI Overlays")]
    [Tooltip("Image สีแดงสำหรับย้อมหน้าจอตอนใส่ฮู้ด")]
    [SerializeField] private Image redVeilOverlay; 
    [Tooltip("Image รูปขอบหมอกพิษสีม่วง/ดำ ตอนถอดฮู้ดในดงพิษ")]
    [SerializeField] private Image poisonVignetteOverlay;

    [Header("Transition Settings")]
    [SerializeField] private float fadeSpeed = 4f;
    [SerializeField] private float maxRedVeilAlpha = 0.35f;
    [SerializeField] private float maxPoisonAlpha = 0.85f;

    private PlayerHood playerHood;
    private Coroutine transitionCoroutine;

    private void Start()
    {
        playerHood = FindFirstObjectByType<PlayerHood>();
        
        // เซ็ตค่าเริ่มต้นให้โปร่งใส 100%
        if (redVeilOverlay != null) SetAlpha(redVeilOverlay, 0f);
        if (poisonVignetteOverlay != null) SetAlpha(poisonVignetteOverlay, 0f);
    }

    private void OnEnable()
    {
        GameEvents.OnHoodToggled += HandleHoodToggle;
    }

    private void OnDisable()
    {
        GameEvents.OnHoodToggled -= HandleHoodToggle;
    }

    private void Update()
    {
        HandlePoisonZoneAtmosphere();
    }

    private void HandleHoodToggle(bool isWearingHood)
    {
        float targetRedAlpha = isWearingHood ? maxRedVeilAlpha : 0f;
        StartFade(redVeilOverlay, targetRedAlpha);
    }

    private void HandlePoisonZoneAtmosphere()
    {
        if (playerHood == null || poisonVignetteOverlay == null) return;

        // ถ้านอก Safe Zone และ ไม่ได้ใส่ฮู้ด -> ให้หมอกพิษค่อยๆ คลุมจอ
        bool isInDanger = !playerHood.IsInSafeZone && !playerHood.IsWearingHood;
        float targetAlpha = isInDanger ? maxPoisonAlpha : 0f;

        float currentAlpha = poisonVignetteOverlay.color.a;
        float newAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, Time.deltaTime * (fadeSpeed * 0.5f));
        SetAlpha(poisonVignetteOverlay, newAlpha);
    }

    private void StartFade(Image targetImage, float targetAlpha)
    {
        if (targetImage == null) return;
        StartCoroutine(FadeRoutine(targetImage, targetAlpha));
    }

    private IEnumerator FadeRoutine(Image img, float targetAlpha)
    {
        while (Mathf.Abs(img.color.a - targetAlpha) > 0.01f)
        {
            float a = Mathf.Lerp(img.color.a, targetAlpha, Time.deltaTime * fadeSpeed);
            SetAlpha(img, a);
            yield return null;
        }
        SetAlpha(img, targetAlpha);
    }

    private void SetAlpha(Image img, float a)
    {
        Color c = img.color;
        c.a = a;
        img.color = c;
    }
}