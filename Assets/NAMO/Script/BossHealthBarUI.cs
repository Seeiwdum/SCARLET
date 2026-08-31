using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(CanvasGroup))]
public class BossHealthBarUI : MonoBehaviour
{
    public static BossHealthBarUI Instance { get; private set; }

    [Header("UI Elements")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider easeHealthSlider; // แถบเลือดดีเลย์สีขาว/ส้ม
    [SerializeField] private TextMeshProUGUI bossNameText;

    [Header("Settings")]
    [SerializeField] private float fadeSpeed = 3f;
    [SerializeField] private float easeSpeed = 0.05f;

    private CanvasGroup canvasGroup;
    private Coroutine fadeCoroutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        
        // Hide completely at start by disabling the first child (which usually contains the UI elements)
        if (transform.childCount > 0)
        {
            transform.GetChild(0).gameObject.SetActive(false);
        }
    }

    public void ShowBossBar(string bossName, int maxHealth)
    {
        // Re-enable the UI visuals when waking up
        if (transform.childCount > 0)
        {
            transform.GetChild(0).gameObject.SetActive(true);
        }

        if (bossNameText != null) bossNameText.text = bossName;
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = maxHealth;
        }
        if (easeHealthSlider != null)
        {
            easeHealthSlider.maxValue = maxHealth;
            easeHealthSlider.value = maxHealth;
        }

        FadeBar(1f);
    }

    public void UpdateHealth(int currentHealth)
    {
        if (healthSlider != null)
        {
            healthSlider.value = currentHealth;
        }
    }

    private void Update()
    {
        if (healthSlider != null && easeHealthSlider != null)
        {
            if (easeHealthSlider.value > healthSlider.value)
            {
                easeHealthSlider.value = Mathf.Lerp(easeHealthSlider.value, healthSlider.value, easeSpeed);
            }
        }
    }

    public void HideBossBar()
    {
        FadeBar(0f);
    }

    private void FadeBar(float targetAlpha)
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeRoutine(targetAlpha));
    }

    private IEnumerator FadeRoutine(float targetAlpha)
    {
        while (Mathf.Abs(canvasGroup.alpha - targetAlpha) > 0.02f)
        {
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);
            yield return null;
        }
        canvasGroup.alpha = targetAlpha;
    }
}