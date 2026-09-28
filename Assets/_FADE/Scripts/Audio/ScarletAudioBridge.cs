using System;
using System.Collections.Generic;
using UnityEngine;

namespace Scarlet.Audio
{
    /// <summary>Maps any announcement asset to a sound. It knows no player, enemy, quest, or action enum.</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("SCARLET/Audio/Scarlet Audio Bridge")]
    public sealed partial class ScarletAudioBridge : MonoBehaviour
    {
        [Serializable]
        private sealed class SignalBinding
        {
            public AudioSignal signal;
            public SoundCue cue;
            [Min(0f)] public float musicFadeSeconds = 1f;
            [Min(0f)] public float stopFadeSeconds = 0.1f;
        }

        [Header("Any signal can request any cue")]
        [SerializeField] private SignalBinding[] bindings = Array.Empty<SignalBinding>();
        [Tooltip("Ignore requests owned by objects in another scene. Send an owner with world requests.")]
        [SerializeField] private bool onlyOwnersInThisScene = true;

        private sealed class Connection
        {
            public AudioSignal Signal;
            public Action<AudioSignalMessage> Listener;
        }

        private readonly struct LoopKey : IEquatable<LoopKey>
        {
            public readonly AudioSignal Signal;
            public readonly int OwnerId;
            public LoopKey(AudioSignal signal, UnityEngine.Object owner)
            { Signal = signal; OwnerId = owner.GetInstanceID(); }
            public bool Equals(LoopKey other) => Signal == other.Signal && OwnerId == other.OwnerId;
            public override bool Equals(object value) => value is LoopKey other && Equals(other);
            public override int GetHashCode() => Signal.GetInstanceID() * 397 ^ OwnerId;
        }

        private readonly List<Connection> connections = new List<Connection>();
        private readonly Dictionary<LoopKey, SoundHandle> loops = new Dictionary<LoopKey, SoundHandle>();
        private readonly List<LoopKey> expired = new List<LoopKey>();
        private bool warnedMissingManager;

        // Optional compatibility file implements these for old SCARLET scenes.
        // They disappear at compile time when that file is omitted in another project.
        partial void EnableLegacyCompatibility();
        partial void DisableLegacyCompatibility();

        private void OnEnable()
        {
            EnableLegacyCompatibility();
            var assigned = new HashSet<AudioSignal>();
            if (bindings == null) return;
            foreach (SignalBinding binding in bindings)
            {
                if (binding == null || binding.signal == null || binding.cue == null) continue;
                if (!assigned.Add(binding.signal))
                {
                    Debug.LogWarning($"Signal '{binding.signal.name}' is assigned twice on this Bridge. The first binding is used.", this);
                    continue;
                }
                SignalBinding selected = binding;
                Action<AudioSignalMessage> listener = message => Receive(selected, message);
                binding.signal.Subscribe(listener);
                connections.Add(new Connection { Signal = binding.signal, Listener = listener });
            }
        }

        private void OnDisable()
        {
            foreach (Connection connection in connections)
                if (connection.Signal != null) connection.Signal.Unsubscribe(connection.Listener);
            connections.Clear();
            SoundManager manager = SoundManager.Instance;
            if (manager != null)
                foreach (SoundHandle ticket in loops.Values) manager.Stop(ticket);
            loops.Clear();
            expired.Clear();
            DisableLegacyCompatibility();
        }

        private void Update()
        {
            if (loops.Count == 0) return;
            SoundManager manager = SoundManager.Instance;
            expired.Clear();
            foreach (KeyValuePair<LoopKey, SoundHandle> loop in loops)
                if (manager == null || !manager.IsActive(loop.Value)) expired.Add(loop.Key);
            foreach (LoopKey key in expired) loops.Remove(key);
        }

        private void Receive(SignalBinding binding, AudioSignalMessage message)
        {
            if (!isActiveAndEnabled || !AcceptOwner(message.Owner)) return;
            SoundManager manager = SoundManager.Instance;
            if (manager == null || !manager.IsReady)
            {
                if (!warnedMissingManager)
                {
                    warnedMissingManager = true;
                    Debug.LogWarning("This Bridge needs a ready AudioSystem. Early sound requests are skipped rather than replayed later.", this);
                }
                return;
            }

            if (message.IsStop)
            {
                if (binding.cue.Category == SoundCategory.BGM)
                {
                    // An old request must not stop a different track selected afterwards.
                    if (manager.CurrentMusicCue == binding.cue)
                        manager.PlayMusic(null, binding.musicFadeSeconds);
                }
                else StopSignalLoops(binding, message.Owner, manager);
                return;
            }

            if (binding.cue.Category == SoundCategory.BGM)
            {
                manager.PlayMusic(binding.cue, binding.musicFadeSeconds);
                return;
            }

            UnityEngine.Object owner = message.Owner != null ? message.Owner : this;
            LoopKey key = new LoopKey(binding.signal, owner);
            // Repeated "start" announcements from one owner do not stack its continuous sound.
            if (binding.cue.Loop && loops.TryGetValue(key, out SoundHandle running) && manager.IsActive(running)) return;
            SoundHandle handle;
            if (message.FollowTarget != null)
                handle = manager.PlayFollowing(binding.cue, message.FollowTarget, owner);
            else if (message.HasPosition)
                handle = manager.PlayAt(binding.cue, message.Position, owner);
            else handle = manager.Play(binding.cue, owner);
            if (binding.cue.Loop && handle.IsValid) loops[key] = handle;
        }

        private bool AcceptOwner(UnityEngine.Object owner)
        {
            // A destroyed supplied owner is not a new global request.
            if (!ReferenceEquals(owner, null) && owner == null) return false;
            if (!onlyOwnersInThisScene || owner == null) return true;
            GameObject source = owner is GameObject go ? go : owner is Component component ? component.gameObject : null;
            return source == null || source.scene == gameObject.scene;
        }

        private void StopSignalLoops(SignalBinding binding, UnityEngine.Object owner, SoundManager manager)
        {
            expired.Clear();
            int ownerId = owner != null ? owner.GetInstanceID() : 0;
            foreach (KeyValuePair<LoopKey, SoundHandle> loop in loops)
            {
                if (loop.Key.Signal != binding.signal || (owner != null && loop.Key.OwnerId != ownerId)) continue;
                manager.Stop(loop.Value, Mathf.Max(0f, binding.stopFadeSeconds));
                expired.Add(loop.Key);
            }
            foreach (LoopKey key in expired) loops.Remove(key);
        }

        // Inspector/UnityEvent entry points: no new script is required for a button or animation callback.
        public void Raise(AudioSignal signal) { if (signal != null) signal.RaiseFrom(this); }
        public void RaiseAtThisObject(AudioSignal signal) { if (signal != null) signal.RaiseAt(transform.position, this); }
        public void Stop(AudioSignal signal) { if (signal != null) signal.Stop(); }
    }
}
