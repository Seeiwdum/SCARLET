using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Scarlet.UI
{
    /// <summary>Exits Play Mode in the Editor and quits the built game.</summary>
    [RequireComponent(typeof(Button))]
    public sealed class QuitGameButton : MonoBehaviour
    {
        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            if (button == null) button = GetComponent<Button>();
            button.onClick.AddListener(QuitGame);
        }

        private void OnDisable()
        {
            if (button != null) button.onClick.RemoveListener(QuitGame);
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
