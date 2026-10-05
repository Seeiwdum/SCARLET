using UnityEngine;

/// <summary>
/// <<MonoBehaviour>> NPC - dialogue interaction. Inherits InteractableBase template.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class NPC : InteractableBase
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

    [Header("Dialogues: ชาวบ้าน (Grace & Villagers - Plot Twist Clues)")]
    [TextArea(2, 4)]
    [SerializeField] private string[] crypticDialogues = new string[] {
        "ผ้าคลุมสีแดงผืนนั้น... มันดูคุ้นตาเหลือเกิน เหมือนกับเมื่อหลายร้อยปีก่อน...",
        "ทำไมกันนะ... ทำไมเธอถึงไม่เคยบาดเจ็บจากหมอกพิษเลยล่ะ?"
    };

    public override void Interact()
    {
        string[] selectedDialogue = GetCurrentDialogue();
        if (DialogueUI.Instance != null)
        {
            DialogueUI.Instance.StartDialogue(npcName, npcPortrait, selectedDialogue);
        }
        HidePrompt();
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
            return crypticDialogues;
        }
    }
}
