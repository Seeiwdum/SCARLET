using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class NPC : MonoBehaviour, IInteractable
{
    public enum NPCType { Karen, Grace, Villager }

    [Header("NPC Identity")]
    [SerializeField] private NPCType npcType;
    [SerializeField] private string npcName = "ยายคาเรน";
    [SerializeField] private Sprite npcPortrait;

    [Header("Dialogues: ยายคาเรน (Karen)")]
    [TextArea(2, 4)]
    [SerializeField] private string[] karenInitialQuest = new string[] {
        "เด็กน้อยเอ๋ย... จงนำ 'หัวใจเพลิง' ทั้ง 4 จากผู้พิทักษ์ในป่ากลับมา",
        "เราต้องจุดไฟประภาคาร เพื่อขับไล่หมอกพิษกัดกร่อนนี้ให้สิ้นซาก"
    };
    [TextArea(2, 4)]
    [SerializeField] private string[] karenIncompleteQuest = new string[] {
        "หัวใจเพลิงยังไม่ครบ... จงรีบไปเถิด ก่อนที่หมอกจะกลืนกินพวกเราทั้งหมด"
    };
    [TextArea(2, 4)]
    [SerializeField] private string[] karenCompleteQuest = new string[] {
        "ยอดเยี่ยมมาก... ในที่สุด 'หัวใจเพลิง' ทั้ง 4 ก็มารวมอยู่ที่นี่",
        "จงวางมันลงที่ประภาคารสิ... แล้วเปลวไฟนิรันดร์จะตื่นขึ้น..."
    };

    [Header("Dialogues: ยายจ๋า / ชาวบ้าน (Grace & Villagers - Plot Twist Clues)")]
    [TextArea(2, 4)]
    [SerializeField] private string[] crypticDialogues = new string[] {
        "ผ้าคลุมสีแดงผืนนั้น... มันดูคุ้นตาเหลือเกิน เหมือนกับเมื่อหลายร้อยปีก่อน...",
        "ทำไมกันนะ... ทำไมเธอถึงไม่เคยบาดเจ็บจากหมอกพิษเลยล่ะ?"
    };

    [Header("Dynamic Prompt (UI นูนขึ้น & Fade)")]
    [SerializeField] private SpriteRenderer promptSprite;
    [SerializeField] private Vector3 targetScale = new Vector3(1f, 1f, 1f);
    [SerializeField] private float animationSpeed = 8f;
    [SerializeField] private KeyCode interactKey = KeyCode.W;

    private bool isPlayerInRange = false;
    private Coroutine promptCoroutine;

    private void Start()
    {
        if (promptSprite != null)
        {
            SetPromptAlpha(0f);
            promptSprite.transform.localScale = Vector3.zero;
        }
    }

    private void Update()
    {
        if (isPlayerInRange && Input.GetKeyDown(interactKey))
        {
            if (DialogueUI.Instance != null && !DialogueUI.Instance.IsDialogueActive)
            {
                Interact();
            }
        }
    }

    public void Interact()
    {
        string[] selectedDialogue = GetCurrentDialogue();
        if (DialogueUI.Instance != null)
        {
            DialogueUI.Instance.StartDialogue(npcName, npcPortrait, selectedDialogue);
        }
    }

    private string[] GetCurrentDialogue()
    {
        if (npcType == NPCType.Karen)
        {
            int hearts = FlameHeartLedger.Instance != null ? FlameHeartLedger.Instance.CurrentFlameHearts : 0;
            if (hearts == 0) return karenInitialQuest;
            if (hearts < 4) return karenIncompleteQuest;
            return karenCompleteQuest;
        }
        else
        {
            // ชาวบ้าน/ยายจ๋า จะพูดประโยคสุ่มหรือคำใบ้แปลกๆ แฝงความจริง
            return crypticDialogues;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            AnimatePrompt(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            AnimatePrompt(false);
            if (DialogueUI.Instance != null && DialogueUI.Instance.IsDialogueActive)
            {
                DialogueUI.Instance.EndDialogue();
            }
        }
    }

    #region Dynamic Prompt Animation
    private void AnimatePrompt(bool show)
    {
        if (promptSprite == null) return;
        if (promptCoroutine != null) StopCoroutine(promptCoroutine);
        promptCoroutine = StartCoroutine(PromptRoutine(show));
    }

    private IEnumerator PromptRoutine(bool show)
    {
        float targetAlpha = show ? 1f : 0f;
        Vector3 finalScale = show ? targetScale : Vector3.zero;

        while (Mathf.Abs(promptSprite.color.a - targetAlpha) > 0.01f)
        {
            SetPromptAlpha(Mathf.Lerp(promptSprite.color.a, targetAlpha, Time.deltaTime * animationSpeed));
            promptSprite.transform.localScale = Vector3.Lerp(promptSprite.transform.localScale, finalScale, Time.deltaTime * (animationSpeed * 1.2f));
            yield return null;
        }

        SetPromptAlpha(targetAlpha);
        promptSprite.transform.localScale = finalScale;
    }

    private void SetPromptAlpha(float alpha)
    {
        Color c = promptSprite.color;
        c.a = alpha;
        promptSprite.color = c;
    }
    #endregion
}