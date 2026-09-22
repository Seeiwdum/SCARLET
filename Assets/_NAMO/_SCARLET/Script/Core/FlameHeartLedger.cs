using UnityEngine;

public class FlameHeartLedger : MonoBehaviour
{
    public static FlameHeartLedger Instance { get; private set; }

    [Header("Main Quest: Flame Hearts")]
    [SerializeField] private int totalFlameHearts = 4;
    private int currentFlameHearts = 0;

    public int CurrentFlameHearts => currentFlameHearts;
    public int TotalFlameHearts => totalFlameHearts;
    public bool IsAllHeartsCollected => currentFlameHearts >= totalFlameHearts;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// Domain operation: Deposit a Flame Heart
    /// </summary>
    public void Deposit(int amount = 1)
    {
        currentFlameHearts = Mathf.Clamp(currentFlameHearts + amount, 0, totalFlameHearts);
        Debug.Log($"<color=orange>[QUEST] ได้รับหัวใจเพลิง! ({currentFlameHearts}/{totalFlameHearts})</color>");
        GameEvents.OnFlameHeartCollected?.Invoke(currentFlameHearts, totalFlameHearts);
    }

    // Legacy compatibility
    [System.Obsolete("Use Deposit")] public void CollectFlameHeart() => Deposit(1);

    // Domain queries
    public bool IsComplete() => IsAllHeartsCollected;
    public float Progress() => (float)currentFlameHearts / Mathf.Max(1, totalFlameHearts);
}

// Legacy alias for inspector references
[System.Obsolete("Use FlameHeartLedger")] public class QuestManager : FlameHeartLedger {}