using System.Collections;
using UnityEngine;

public class VisualEffectsManager : MonoBehaviour
{
    public static VisualEffectsManager Instance { get; private set; }

    [Header("Hitstop Settings (จังหวะฟันหยุดชะงัก)")]
    [SerializeField] private float defaultHitstopDuration = 0.06f;

    [Header("Ghost Trail Settings (เงาตอน Dash / Warp)")]
    [SerializeField] private GameObject ghostPrefab;
    [SerializeField] private float ghostSpawnInterval = 0.04f;
    [SerializeField] private Color ghostColor = new Color(1f, 0.3f, 0.1f, 0.6f);

    private Coroutine hitstopCoroutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnEnable()
    {
        // Subscribe ฟังวิทยุสื่อสาร
        GameEvents.OnEnemyHit += DoDefaultHitstop;
        GameEvents.OnSwordVaultPerformed += DoDefaultHitstop;
    }

    private void OnDisable()
    {
        GameEvents.OnEnemyHit -= DoDefaultHitstop;
        GameEvents.OnSwordVaultPerformed -= DoDefaultHitstop;
    }

    private void DoDefaultHitstop()
    {
        TriggerHitstop(defaultHitstopDuration);
    }

    public void TriggerHitstop(float duration)
    {
        if (hitstopCoroutine != null) StopCoroutine(hitstopCoroutine);
        hitstopCoroutine = StartCoroutine(HitstopRoutine(duration));
    }

    private IEnumerator HitstopRoutine(float duration)
    {
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
    }

    /// <summary>
    /// สั่งเสกเงาตามติดตัวละคร (Ghost Trail)
    /// </summary>
    public void StartGhostTrail(SpriteRenderer sourceRenderer, float duration)
    {
        StartCoroutine(GhostTrailRoutine(sourceRenderer, duration));
    }

    private IEnumerator GhostTrailRoutine(SpriteRenderer sourceRenderer, float duration)
    {
        float timer = 0f;
        while (timer < duration)
        {
            if (sourceRenderer != null)
            {
                SpawnGhost(sourceRenderer);
            }
            yield return new WaitForSeconds(ghostSpawnInterval);
            timer += ghostSpawnInterval;
        }
    }

    private void SpawnGhost(SpriteRenderer sourceRenderer)
    {
        GameObject ghost = new GameObject("GhostTrail");
        ghost.transform.position = sourceRenderer.transform.position;
        ghost.transform.rotation = sourceRenderer.transform.rotation;
        ghost.transform.localScale = sourceRenderer.transform.lossyScale;

        SpriteRenderer sr = ghost.AddComponent<SpriteRenderer>();
        sr.sprite = sourceRenderer.sprite;
        sr.color = ghostColor;
        sr.flipX = sourceRenderer.flipX;
        sr.sortingOrder = sourceRenderer.sortingOrder - 1;

        StartCoroutine(FadeAndDestroyGhost(sr, 0.25f));
    }

    private IEnumerator FadeAndDestroyGhost(SpriteRenderer sr, float fadeTime)
    {
        float t = 0f;
        Color startCol = sr.color;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            Color c = startCol;
            c.a = Mathf.Lerp(startCol.a, 0f, t / fadeTime);
            sr.color = c;
            yield return null;
        }
        Destroy(sr.gameObject);
    }

    /// <summary>
    /// เสกเงาร่างไฟเรียงตามแนวเส้นทางวาร์ปจากจุดเริ่มต้นไปยังจุดเป้าหมาย
    /// </summary>
    public void CreateWarpGhostTrail(SpriteRenderer sourceRenderer, Vector3 startPos, Vector3 targetPos, int ghostCount = 4)
    {
        if (sourceRenderer == null || ghostPrefab == null) return;

        for (int i = 0; i < ghostCount; i++)
        {
            float t = (float)i / (ghostCount - 1);
            Vector3 spawnPos = Vector3.Lerp(startPos, targetPos, t);

            GameObject ghost = new GameObject("WarpGhostTrail");
            ghost.transform.position = spawnPos;
            ghost.transform.rotation = sourceRenderer.transform.rotation;
            ghost.transform.localScale = sourceRenderer.transform.lossyScale;

            SpriteRenderer sr = ghost.AddComponent<SpriteRenderer>();
            sr.sprite = sourceRenderer.sprite;
            sr.color = ghostColor;
            sr.flipX = sourceRenderer.flipX;
            sr.sortingOrder = sourceRenderer.sortingOrder - 1;

            StartCoroutine(FadeAndDestroyGhost(sr, 0.2f + (t * 0.1f)));
        }
    }
}