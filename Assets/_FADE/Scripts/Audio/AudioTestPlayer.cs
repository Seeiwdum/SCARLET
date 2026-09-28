using UnityEngine;

namespace Scarlet.Audio
{
    // Optional manual test tool. Remove its GameObject after testing.
    [AddComponentMenu("SCARLET/Audio/Audio Test Player")]
    public sealed class AudioTestPlayer : MonoBehaviour
    {
        [SerializeField] private SoundCue cue;
        [Tooltip("Use this object's position if left empty.")]
        [SerializeField] private Transform soundOrigin;
        [Tooltip("Enabled: volume follows the origin's current position. Disabled: use its starting position.")]
        [SerializeField] private bool followOrigin;
        private SoundHandle handle;
        private bool playingMusic;

        [ContextMenu("Play Test Sound (Play Mode)")]
        public void PlayTestSound()
        {
            if (!Application.isPlaying || SoundManager.Instance == null || !SoundManager.Instance.IsReady)
            {
                Debug.LogWarning("Enter Play Mode with an AudioSystem before testing a sound.", this);
                return;
            }
            if (cue == null) { Debug.LogWarning("Assign a test SoundCue first.", this); return; }
            StopTestSound();
            Transform origin = soundOrigin != null ? soundOrigin : transform;
            if (cue.Category == SoundCategory.BGM)
            {
                SoundManager.Instance.PlayMusic(cue);
                playingMusic = true;
            }
            else handle = followOrigin
                ? SoundManager.Instance.PlayFollowing(cue, origin, this)
                : SoundManager.Instance.PlayAt(cue, origin.position, this);
        }

        [ContextMenu("Stop Test Sound (Play Mode)")]
        public void StopTestSound()
        {
            if (!Application.isPlaying || SoundManager.Instance == null) return;
            SoundManager.Instance.Stop(handle);
            handle = default;
            if (!playingMusic) return;
            playingMusic = false;
            if (SceneAudio.Active != null)
                SceneAudio.Active.ReapplyMusicSelection();
            else
                SoundManager.Instance.PlayMusic(null);
        }

        private void OnDisable()
        {
            if (Application.isPlaying)
                StopTestSound();
        }
    }
}
