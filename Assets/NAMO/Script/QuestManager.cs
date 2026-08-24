using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

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
    /// เรียกใช้เมื่อกำจัดบอสผู้พิทักษ์และเก็บหัวใจเพลิงได้
    /// </summary>
    public void CollectFlameHeart()
    {
        currentFlameHearts = Mathf.Clamp(currentFlameHearts + 1, 0, totalFlameHearts);
        Debug.Log($"<color=orange>[QUEST] ได้รับหัวใจเพลิง! ({currentFlameHearts}/{totalFlameHearts})</color>");
        
        GameEvents.OnFlameHeartCollected?.Invoke(currentFlameHearts, totalFlameHearts);
    }
}