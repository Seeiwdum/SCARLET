using System.Collections.Generic;
using UnityEngine;

public class MagicStoneBeacon : MonoBehaviour
{
    private static readonly List<BeaconTarget> activeTargets = new List<BeaconTarget>();

    [Header("Visual Feedback")]
    [Tooltip("ใส่ SpriteRenderer รูปแสงออร่า/วงกลมฟุ้งๆ ที่คอ Scarlet")]
    [SerializeField] private SpriteRenderer stoneAuraSprite;

    [Header("Scale & Opacity Settings")]
    [SerializeField] private float baseScale = 0.8f;
    [SerializeField] private float maxScale = 2.5f;
    [SerializeField] private float baseAlpha = 0.2f;
    [SerializeField] private float maxAlpha = 0.9f;

    [Header("Pulse Frequency (ความถี่การกะพริบ)")]
    [SerializeField] private float minPulseSpeed = 2f;
    [SerializeField] private float maxPulseSpeed = 10f;

    [Header("Color Palette per Target")]
    [SerializeField] private Color defaultColor = new Color(0.4f, 0.8f, 1f, 1f);       // ฟ้าเวทมนตร์
    [SerializeField] private Color loreFragmentColor = new Color(1f, 0.85f, 0.3f, 1f); // สีทองโบราณ
    [SerializeField] private Color bossColor = new Color(1f, 0.3f, 0f, 1f);            // สีส้มเพลิง
    [SerializeField] private Color corruptedColor = new Color(0.7f, 0f, 0.9f, 1f);     // สีม่วงมืดมน

    private BeaconTarget closestTarget;
    private float currentDistance = float.MaxValue;

    public static void RegisterTarget(BeaconTarget target)
    {
        if (!activeTargets.Contains(target)) activeTargets.Add(target);
    }

    public static void UnregisterTarget(BeaconTarget target)
    {
        if (activeTargets.Contains(target)) activeTargets.Remove(target);
    }

    private void Update()
    {
        FindClosestTarget();
        UpdateStoneFeedback();
    }

    private void FindClosestTarget()
    {
        closestTarget = null;
        currentDistance = float.MaxValue;

        for (int i = 0; i < activeTargets.Count; i++)
        {
            if (activeTargets[i] == null) continue;

            float dist = Vector2.Distance(transform.position, activeTargets[i].transform.position);
            if (dist <= activeTargets[i].DetectionRadius && dist < currentDistance)
            {
                currentDistance = dist;
                closestTarget = activeTargets[i];
            }
        }
    }

    private void UpdateStoneFeedback()
    {
        if (closestTarget == null)
        {
            ApplyFeedback(defaultColor, baseAlpha, baseScale, minPulseSpeed);
            return;
        }

        float proximity = 1f - Mathf.Clamp01(currentDistance / closestTarget.DetectionRadius);

        Color targetColor = defaultColor;
        switch (closestTarget.Type)
        {
            case BeaconTarget.TargetType.LoreFragment:
                targetColor = loreFragmentColor;
                break;
            case BeaconTarget.TargetType.GuardianBoss:
                targetColor = bossColor;
                break;
            case BeaconTarget.TargetType.CorruptedOrigin:
                targetColor = corruptedColor;
                break;
        }

        float calculatedAlpha = Mathf.Lerp(baseAlpha, maxAlpha, proximity);
        float calculatedScale = Mathf.Lerp(baseScale, maxScale, proximity);
        float calculatedSpeed = Mathf.Lerp(minPulseSpeed, maxPulseSpeed, proximity);

        if (closestTarget.Type == BeaconTarget.TargetType.CorruptedOrigin)
        {
            calculatedAlpha *= Random.Range(0.4f, 1.2f);
        }

        ApplyFeedback(targetColor, calculatedAlpha, calculatedScale, calculatedSpeed);
    }

    private void ApplyFeedback(Color color, float targetAlpha, float targetScale, float pulseSpeed)
    {
        if (stoneAuraSprite == null) return;

        float pulseFactor = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
        
        Color c = color;
        c.a = targetAlpha * Mathf.Lerp(0.6f, 1.2f, pulseFactor);
        stoneAuraSprite.color = c;

        float finalScale = targetScale * (1f + (pulseFactor * 0.15f));
        stoneAuraSprite.transform.localScale = new Vector3(finalScale, finalScale, 1f);
    }
}