using UnityEngine;
using UnityEngine.UI;

namespace Scarlet.UI
{
    /// <summary>Toggles a UI panel and animates its independent opening and closing transitions.</summary>
    [RequireComponent(typeof(Button))]
    public sealed class MainMenuPanelToggle : MonoBehaviour
    {
        public enum Motion { Fade, FromLeft, FromRight, FromTop, FromBottom }

        [Header("References")]
        [SerializeField] private GameObject panel;
        [SerializeField] private MainMenuPromptFade prompt;
        [SerializeField] private CanvasGroup panelCanvasGroup;
        [Header("Open animation")]
        [SerializeField] private Motion openMotion = Motion.Fade;
        [SerializeField, Min(0f)] private float openDuration = 0.35f;
        [Header("Close animation")]
        [SerializeField] private Motion closeMotion = Motion.Fade;
        [SerializeField, Min(0f)] private float closeDuration = 0.25f;
        [Header("Optional button sound")]
        [SerializeField] private Scarlet.Audio.SoundCue clickCue;

        private Button button;
        private RectTransform panelRect;
        private RectTransform parentRect;
        private RectTransform canvasRect;
        private Vector2 restingPosition;
        private Vector2 startPosition;
        private Vector2 targetPosition;
        private float startAlpha;
        private float targetAlpha;
        private float elapsed;
        private float activeDuration;
        private bool isOpen;
        private bool transitioning;

        private void Awake()
        {
            button = GetComponent<Button>();
            if (panel == null || panel == gameObject || transform.IsChildOf(panel.transform))
            {
                Debug.LogWarning("Assign a separate panel that does not contain the MainMenuPanelToggle Button.", this);
                enabled = false;
                return;
            }

            panelRect = panel.GetComponent<RectTransform>();
            panelCanvasGroup = panelCanvasGroup != null ? panelCanvasGroup : panel.GetComponent<CanvasGroup>();
            if (panelCanvasGroup == null) panelCanvasGroup = panel.AddComponent<CanvasGroup>();
            parentRect = panelRect != null ? panelRect.parent as RectTransform : null;
            Canvas canvas = panel.GetComponentInParent<Canvas>();
            canvasRect = canvas != null ? canvas.rootCanvas.transform as RectTransform : null;
            isOpen = panel.activeSelf;
            if (panelRect != null) restingPosition = panelRect.anchoredPosition;
            if (HasSlide(openMotion) || HasSlide(closeMotion))
            {
                if (panelRect == null || parentRect == null)
                {
                    Debug.LogWarning("Slide animations need a panel with a RectTransform inside a UI RectTransform.", this);
                    enabled = false;
                    return;
                }
            }
            panelCanvasGroup.interactable = isOpen;
            panelCanvasGroup.blocksRaycasts = isOpen;
            if (prompt != null) prompt.SetVisible(!isOpen);
        }

        private void OnEnable()
        {
            if (button != null) button.onClick.AddListener(Toggle);
        }

        private void OnDisable()
        {
            if (button != null) button.onClick.RemoveListener(Toggle);
            if (transitioning) Settle(isOpen);
        }

        public void Toggle()
        {
            if (panel == null) return;
            bool opening = !isOpen;
            bool wasActive = panel.activeSelf;
            isOpen = opening;
            if (opening)
            {
                panel.SetActive(true);
                Canvas.ForceUpdateCanvases();
                if (panelRect != null && !wasActive) restingPosition = panelRect.anchoredPosition;
                panelCanvasGroup.interactable = true;
                panelCanvasGroup.blocksRaycasts = true;
                if (prompt != null) prompt.SetVisible(false);
            }

            Motion motion = opening ? openMotion : closeMotion;
            activeDuration = opening ? openDuration : closeDuration;
            Vector2 currentPosition = panelRect != null ? panelRect.anchoredPosition : restingPosition;
            float currentAlpha = panelCanvasGroup.alpha;
            if (opening && !wasActive)
            {
                currentPosition = HasSlide(motion) ? GetOffscreenPosition(motion) : restingPosition;
                currentAlpha = motion == Motion.Fade ? 0f : 1f;
            }

            startPosition = currentPosition;
            startAlpha = currentAlpha;
            if (panelRect != null) panelRect.anchoredPosition = startPosition;
            panelCanvasGroup.alpha = startAlpha;
            targetPosition = opening || motion == Motion.Fade ? restingPosition : GetOffscreenPosition(motion);
            if (motion == Motion.Fade) targetAlpha = opening ? 1f : 0f;
            else targetAlpha = opening ? 1f : currentAlpha;
            elapsed = 0f;
            transitioning = true;
            if (activeDuration <= 0f) CompleteTransition();
            PlayClickSound();
        }

        private void Update()
        {
            if (!transitioning) return;
            elapsed += Time.unscaledDeltaTime;
            float t = activeDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / activeDuration);
            t = Mathf.SmoothStep(0f, 1f, t);
            if (panelRect != null) panelRect.anchoredPosition = Vector2.LerpUnclamped(startPosition, targetPosition, t);
            panelCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            if (elapsed >= activeDuration) CompleteTransition();
        }

        private void CompleteTransition()
        {
            transitioning = false;
            if (isOpen)
            {
                if (panelRect != null) panelRect.anchoredPosition = restingPosition;
                panelCanvasGroup.alpha = 1f;
                panelCanvasGroup.interactable = true;
                panelCanvasGroup.blocksRaycasts = true;
            }
            else
            {
                Settle(false);

            }
        }

        private void Settle(bool open)
        {
            transitioning = false;
            isOpen = open;
            panel.SetActive(open);
            if (panelRect != null) panelRect.anchoredPosition = restingPosition;
            panelCanvasGroup.alpha = 1f;
            panelCanvasGroup.interactable = open;
            panelCanvasGroup.blocksRaycasts = open;
            if (prompt != null) prompt.SetVisible(!open);
        }

        private Vector2 GetOffscreenPosition(Motion motion)
        {
            if (panelRect == null || parentRect == null || canvasRect == null) return panelRect != null ? panelRect.anchoredPosition : restingPosition;
            Vector3[] canvasCorners = new Vector3[4];
            Vector3[] panelCorners = new Vector3[4];
            canvasRect.GetWorldCorners(canvasCorners);
            panelRect.GetWorldCorners(panelCorners);
            float canvasMinX = float.PositiveInfinity, canvasMaxX = float.NegativeInfinity;
            float canvasMinY = float.PositiveInfinity, canvasMaxY = float.NegativeInfinity;
            float panelMinX = float.PositiveInfinity, panelMaxX = float.NegativeInfinity;
            float panelMinY = float.PositiveInfinity, panelMaxY = float.NegativeInfinity;
            for (int i = 0; i < 4; i++)
            {
                Vector3 c = parentRect.InverseTransformPoint(canvasCorners[i]);
                Vector3 p = parentRect.InverseTransformPoint(panelCorners[i]);
                canvasMinX = Mathf.Min(canvasMinX, c.x); canvasMaxX = Mathf.Max(canvasMaxX, c.x);
                canvasMinY = Mathf.Min(canvasMinY, c.y); canvasMaxY = Mathf.Max(canvasMaxY, c.y);
                panelMinX = Mathf.Min(panelMinX, p.x); panelMaxX = Mathf.Max(panelMaxX, p.x);
                panelMinY = Mathf.Min(panelMinY, p.y); panelMaxY = Mathf.Max(panelMaxY, p.y);
            }
            const float padding = 2f;
            float delta = 0f;
            switch (motion)
            {
                case Motion.FromLeft: delta = canvasMinX - panelMaxX - padding; return panelRect.anchoredPosition + Vector2.right * delta;
                case Motion.FromRight: delta = canvasMaxX - panelMinX + padding; return panelRect.anchoredPosition + Vector2.right * delta;
                case Motion.FromTop: delta = canvasMaxY - panelMinY + padding; return panelRect.anchoredPosition + Vector2.up * delta;
                case Motion.FromBottom: delta = canvasMinY - panelMaxY - padding; return panelRect.anchoredPosition + Vector2.up * delta;
                default: return restingPosition;
            }
        }

        private static bool HasSlide(Motion motion) => motion != Motion.Fade;

        private void PlayClickSound()
        {
            Scarlet.Audio.SoundManager manager = Scarlet.Audio.SoundManager.Instance;
            if (clickCue == null || manager == null || !manager.IsReady) return;
            if (clickCue.Category != Scarlet.Audio.SoundCategory.UI)
            {
                Debug.LogWarning("Main menu click cues must use the UI category.", this);
                return;
            }
            manager.Play(clickCue);
        }
    }
}


