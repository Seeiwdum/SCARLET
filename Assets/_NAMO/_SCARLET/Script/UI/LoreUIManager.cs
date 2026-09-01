using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class LoreUIManager : MonoBehaviour
{
    public static LoreUIManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI contentText;
    [SerializeField] private Image illustrationImage;

    [Header("Settings")]
    [SerializeField] private float fadeSpeed = 6f;
    [SerializeField] private float typingSpeed = 0.02f;

    private CanvasGroup canvasGroup;
    private bool isReading = false;
    private bool canClose = false;
    private Coroutine fadeCoroutine;
    private Coroutine typeRoutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        canvasGroup = GetComponent<CanvasGroup>();
        ResetUIImmediate();
    }

    private void OnEnable()
    {
        GameEvents.OnLoreOpened += OpenLore;
    }

    private void OnDisable()
    {
        GameEvents.OnLoreOpened -= OpenLore;
    }

    private void ResetUIImmediate()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        isReading = false;
        canClose = false;
    }

    private void Update()
    {
        // ปลดล็อกให้กดปิดได้หลังจากเปิดขึ้นมาแล้ว ผ่านปุ่ม Escape, Space, หรือคลิกเมาส์
        if (isReading && canClose)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            {
                CloseLore();
            }
        }
    }

    private void OpenLore(LoreData data)
    {
        if (isReading) return;
        isReading = true;
        canClose = false;

        if (titleText != null) titleText.text = data.title;
        if (contentText != null) contentText.text = "";
        
        if (illustrationImage != null)
        {
            illustrationImage.gameObject.SetActive(data.illustration != null);
            illustrationImage.sprite = data.illustration;
        }

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        if (typeRoutine != null) StopCoroutine(typeRoutine);

        fadeCoroutine = StartCoroutine(FadeUI(1f));
        typeRoutine = StartCoroutine(TypeContent(data.content));
        StartCoroutine(EnableCloseCooldownRoutine());
    }

    private IEnumerator EnableCloseCooldownRoutine()
    {
        // หน่วงเวลา 0.2 วินาที (Realtime) เพื่อไม่ให้ปุ่มที่กดเปิดไปทับกับปุ่มปิด
        yield return new WaitForSecondsRealtime(0.2f);
        canClose = true;
    }

    public void CloseLore()
    {
        if (!isReading) return;
        isReading = false;
        canClose = false;

        GameEvents.OnLoreClosed?.Invoke();

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        if (typeRoutine != null) StopCoroutine(typeRoutine);

        fadeCoroutine = StartCoroutine(FadeUI(0f));
    }

    private IEnumerator FadeUI(float targetAlpha)
    {
        if (targetAlpha > 0) Time.timeScale = 0f;

        while (Mathf.Abs(canvasGroup.alpha - targetAlpha) > 0.02f)
        {
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, Time.unscaledDeltaTime * fadeSpeed);
            yield return null;
        }
        canvasGroup.alpha = targetAlpha;

        canvasGroup.interactable = targetAlpha > 0;
        canvasGroup.blocksRaycasts = targetAlpha > 0;

        // คืนค่าเวลาให้เกมกลับมาเดินได้ตามปกติ
        if (targetAlpha == 0) Time.timeScale = 1f;
    }

    private IEnumerator TypeContent(string content)
    {
        if (contentText == null || string.IsNullOrEmpty(content)) yield break;

        foreach (char letter in content.ToCharArray())
        {
            contentText.text += letter;
            yield return new WaitForSecondsRealtime(typingSpeed);
        }
    }
}