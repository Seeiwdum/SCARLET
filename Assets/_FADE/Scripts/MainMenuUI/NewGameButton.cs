using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
#endif

namespace Scarlet.UI
{
    /// <summary>Fades to black, then loads an enabled build scene selected in the Inspector.</summary>
    [RequireComponent(typeof(Button))]
    public sealed class NewGameButton : MonoBehaviour
    {
        [SerializeField] private string scenePath;
        [SerializeField] private Image blackOverlay;
        [SerializeField, Min(0f)] private float fadeDuration = 1f;

        private Button button;
        private CanvasGroup overlayGroup;
        private bool isStarting;
        private Coroutine transition;
        private bool sceneLoadStarted;

        private void Awake()
        {
            button = GetComponent<Button>();
            PrepareOverlay();
        }

        private void OnEnable()
        {
            if (button == null) button = GetComponent<Button>();
            button.onClick.AddListener(StartNewGame);
        }

        private void OnDisable()
        {
            if (button != null) button.onClick.RemoveListener(StartNewGame);
            if (transition != null)
            {
                StopCoroutine(transition);
                transition = null;
            }
            if (!sceneLoadStarted && overlayGroup != null)
            {
                overlayGroup.alpha = 0f;
                overlayGroup.interactable = false;
                overlayGroup.blocksRaycasts = false;
                if (blackOverlay != null) blackOverlay.raycastTarget = false;
                isStarting = false;
            }
        }

        /// <summary>Automatically called by this Button's OnClick listener.</summary>
        public void StartNewGame()
        {
            if (isStarting) return;

            if (!PrepareOverlay())
            {
                Debug.LogError("NewGameButton needs a separate active Overlay Image directly under a root Screen Space - Overlay Canvas.", this);
                return;
            }
            if (string.IsNullOrWhiteSpace(scenePath))
            {
                Debug.LogError("NewGameButton has no enabled destination scene selected.", this);
                return;
            }

            int buildIndex = SceneUtility.GetBuildIndexByScenePath(scenePath);
            if (buildIndex < 0)
            {
                Debug.LogError("NewGameButton scene is not included in the active build configuration: " + scenePath, this);
                return;
            }

            isStarting = true;
            blackOverlay.raycastTarget = true;
            overlayGroup.interactable = false;
            overlayGroup.blocksRaycasts = true;
            transition = StartCoroutine(FadeAndLoad(buildIndex));
        }

        private bool PrepareOverlay()
        {
            if (blackOverlay == null || !blackOverlay.isActiveAndEnabled || !blackOverlay.gameObject.activeInHierarchy)
                return false;

            if (blackOverlay.gameObject == gameObject || transform.IsChildOf(blackOverlay.transform)) return false;
            Canvas canvas = blackOverlay.canvas;
            if (canvas == null || blackOverlay.transform.parent != canvas.transform || canvas.rootCanvas != canvas || !canvas.isActiveAndEnabled || canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                return false;

            overlayGroup = blackOverlay.GetComponent<CanvasGroup>();
            if (overlayGroup == null) overlayGroup = blackOverlay.gameObject.AddComponent<CanvasGroup>();

            RectTransform rect = blackOverlay.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
            blackOverlay.sprite = null;
            blackOverlay.overrideSprite = null;
            blackOverlay.material = null;
            blackOverlay.maskable = false;
            blackOverlay.type = Image.Type.Simple;
            blackOverlay.color = Color.black;
            blackOverlay.raycastTarget = false;
            blackOverlay.transform.SetAsLastSibling();

            overlayGroup.alpha = 0f;
            overlayGroup.interactable = false;
            overlayGroup.blocksRaycasts = false;
            return true;
        }

        private IEnumerator FadeAndLoad(int buildIndex)
        {
            float elapsed = 0f;
            if (fadeDuration <= 0f)
            {
                overlayGroup.alpha = 1f;
            }
            else
            {
                while (elapsed < fadeDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    overlayGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
                    yield return null;
                }
                overlayGroup.alpha = 1f;
            }

            // Let one complete frame render the fully opaque overlay before any scene-load call.
            yield return null;
            transition = null;
            sceneLoadStarted = true;
            SceneManager.LoadScene(buildIndex, LoadSceneMode.Single);
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(NewGameButton))]
    internal sealed class NewGameButtonInspector : Editor
    {
        private const string ScenePathProperty = "scenePath";

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, ScenePathProperty);

            SerializedProperty scenePath = serializedObject.FindProperty(ScenePathProperty);
            List<string> paths = new List<string>();
            List<string> labels = new List<string> { "Select an enabled scene..." };
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (!scene.enabled || AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path) == null)
                    continue;
                paths.Add(scene.path);
                labels.Add(Path.GetFileNameWithoutExtension(scene.path) + " — " + scene.path);
            }

            int selected = paths.IndexOf(scenePath.stringValue) + 1;
            EditorGUILayout.LabelField("Destination", EditorStyles.boldLabel);
            int next = EditorGUILayout.Popup("Enabled Build Scene", selected, labels.ToArray());
            if (next != selected)
                scenePath.stringValue = next > 0 && next <= paths.Count ? paths[next - 1] : string.Empty;

            if (paths.Count == 0)
                EditorGUILayout.HelpBox("There are no enabled scenes with existing scene assets in the active build configuration.", MessageType.Error);
            else if (string.IsNullOrEmpty(scenePath.stringValue))
                EditorGUILayout.HelpBox("Choose a destination from the enabled scenes above.", MessageType.Info);
            else if (!paths.Contains(scenePath.stringValue))
                EditorGUILayout.HelpBox("The selected scene is no longer enabled in the active build configuration. Choose another scene.", MessageType.Error);

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}



