using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class LoreUIManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI contentText;
    [SerializeField] private Image illustrationImage;

    [Header("Settings")]
    [SerializeField] private float fadeSpeed = 3f;
    [SerializeField] private float typingSpeed = 0.02f;

    private CanvasGroup canvasGroup;
    private bool isReading = false;
    private Coroutine typeRoutine;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void OnEnable()
    {
        GameEvents.OnLoreOpened += OpenLore;
    }

    private void OnDisable()
    {
        GameEvents.OnLoreOpened -= OpenLore;
    }

    private void Update()
    {
        // กด Esc หรือ W เพื่อปิดหน้าอ่าน
        if (isReading && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.W)))
        {
            CloseLore();
        }
    }

    private void OpenLore(LoreData data)
    {
        Debug.Log("3. ฝั่ง UI ได้รับสัญญาณแล้ว! กำลังวาดหน้าจอ!");
        
        if (isReading) return;
        isReading = true;

        titleText.text = data.title;
        contentText.text = ""; // เคลียร์ข้อความเพื่อรอพิมพ์
        
        if (illustrationImage != null)
        {
            illustrationImage.gameObject.SetActive(data.illustration != null);
            illustrationImage.sprite = data.illustration;
        }

        StopAllCoroutines();
        StartCoroutine(FadeUI(1f));
        typeRoutine = StartCoroutine(TypeContent(data.content));
    }

    private void CloseLore()
    {
        if (!isReading) return;
        isReading = false;

        GameEvents.OnLoreClosed?.Invoke();
        StopAllCoroutines();
        StartCoroutine(FadeUI(0f));
    }

    private IEnumerator FadeUI(float targetAlpha)
    {
        // หยุดเวลาเมื่อเปิด UI (Fade ไปด้วย หยุดเวลาไปด้วย)
        if (targetAlpha > 0) Time.timeScale = 0f; 

        while (Mathf.Abs(canvasGroup.alpha - targetAlpha) > 0.01f)
        {
            // ใช้ unscaledDeltaTime เพราะ timeScale เป็น 0
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, Time.unscaledDeltaTime * fadeSpeed);
            yield return null;
        }
        canvasGroup.alpha = targetAlpha;

        canvasGroup.interactable = targetAlpha > 0;
        canvasGroup.blocksRaycasts = targetAlpha > 0;

        // คืนค่าเวลาเมื่อปิด UI เสร็จ
        if (targetAlpha == 0) Time.timeScale = 1f; 
    }

    private IEnumerator TypeContent(string content)
    {
        foreach (char letter in content.ToCharArray())
        {
            contentText.text += letter;
            // ใช้ WaitForSecondsRealtime เพื่อไม่ให้ Typewriter ค้างตอน Time.timeScale = 0
            yield return new WaitForSecondsRealtime(typingSpeed);
        }
    }
}