using UnityEngine;

namespace Scarlet.Audio
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    [AddComponentMenu("SCARLET/Audio/Music Zone")]
    public sealed class MusicZone : MonoBehaviour
    {
        [SerializeField] private SoundCue music;
        [Tooltip("Higher numbers win when zones overlap.")]
        [SerializeField] private int priority;
        private Collider2D zoneCollider;

        public SoundCue Music => music;
        public int Priority => priority;

        private void Awake() { zoneCollider = GetComponent<Collider2D>(); }
        private void OnEnable() { SceneAudio.Active?.RefreshZones(); }
        private void OnDisable() { SceneAudio.Active?.RefreshZones(); }

        public bool Contains(Transform player)
        {
            if (!isActiveAndEnabled || player == null) return false;
            if (zoneCollider == null) zoneCollider = GetComponent<Collider2D>();
            if (zoneCollider == null || !zoneCollider.enabled || !zoneCollider.isTrigger) return false;
            // Test the player's root position, including spawn, warp and teleport.
            // This avoids missed exits and multiple-player-collider bookkeeping.
            return zoneCollider.OverlapPoint((Vector2)player.position);
        }

        private void OnValidate()
        {
            Collider2D area = GetComponent<Collider2D>();
            if (area != null && !area.isTrigger)
                Debug.LogWarning("Enable Is Trigger on the MusicZone's Collider2D.", this);
            if (music != null && music.Category != SoundCategory.BGM)
                Debug.LogWarning("A MusicZone needs a BGM cue. An empty cue intentionally selects silence.", this);
        }
    }
}
