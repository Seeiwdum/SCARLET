using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // ใช้ TextMeshPro เพื่อความคมชัดของฟอนต์ภาษาไทย

public class DialogueUI : MonoBehaviour
{
    public static DialogueUI Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI speakerNameText;
    [SerializeField] private TextMeshProUGUI dialogueContentText;
    [SerializeField] private Image speakerPortrait; // (Optional) รูปหน้า NPC

    [Header("Typing Effect Settings")]
    [SerializeField] private float typingSpeed = 0.03f;
    [SerializeField] private KeyCode nextLineKey = KeyCode.W;

    private Queue<string> sentenceQueue = new Queue<string>();
    private bool isTyping = false;
    private string currentSentence = "";
    private Coroutine typingCoroutine;

    public bool IsDialogueActive { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (dialoguePanel != null) dialoguePanel.SetActive(false);
    }

    private void Update()
    {
        if (IsDialogueActive && Input.GetKeyDown(nextLineKey))
        {
            if (isTyping)
            {
                // ถ้ายังพิมพ์ไม่เสร็จ แล้วกดย้ำ ให้แสดงข้อความเต็มประโยคทันที
                StopCoroutine(typingCoroutine);
                dialogueContentText.text = currentSentence;
                isTyping = false;
            }
            else
            {
                DisplayNextSentence();
            }
        }
    }

    public void StartDialogue(string speakerName, Sprite portrait, string[] sentences)
    {
        if (sentences == null || sentences.Length == 0) return;

        IsDialogueActive = true;
        if (dialoguePanel != null) dialoguePanel.SetActive(true);

        if (speakerNameText != null) speakerNameText.text = speakerName;
        if (speakerPortrait != null)
        {
            speakerPortrait.gameObject.SetActive(portrait != null);
            speakerPortrait.sprite = portrait;
        }

        sentenceQueue.Clear();
        foreach (string sentence in sentences)
        {
            sentenceQueue.Enqueue(sentence);
        }

        GameEvents.OnDialogueStarted?.Invoke();
        DisplayNextSentence();
    }

    public void DisplayNextSentence()
    {
        if (sentenceQueue.Count == 0)
        {
            EndDialogue();
            return;
        }

        currentSentence = sentenceQueue.Dequeue();
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeSentenceRoutine(currentSentence));
    }

    private IEnumerator TypeSentenceRoutine(string sentence)
    {
        isTyping = true;
        dialogueContentText.text = "";

        foreach (char letter in sentence.ToCharArray())
        {
            dialogueContentText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
    }

    public void EndDialogue()
    {
        IsDialogueActive = false;
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        GameEvents.OnDialogueEnded?.Invoke();
    }
}