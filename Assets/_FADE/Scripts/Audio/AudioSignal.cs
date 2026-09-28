using System;
using System.Collections.Generic;
using UnityEngine;

namespace Scarlet.Audio
{
    /// <summary>A reusable announcement asset. Create another asset for any new action; no action enum edits.</summary>
    [CreateAssetMenu(menuName = "SCARLET/Audio/Audio Signal", fileName = "Audio Signal")]
    public sealed class AudioSignal : ScriptableObject
    {
        private event Action<AudioSignalMessage> listeners;
        private static readonly HashSet<AudioSignal> connectedSignals = new HashSet<AudioSignal>();

        // No position: useful for UI, global feedback, or a scene-owned background loop.
        public void Raise() => Send(new AudioSignalMessage(false, null, false, default, null));
        public void RaiseFrom(UnityEngine.Object owner) => Send(new AudioSignalMessage(false, owner, false, default, null));

        // The sound stays at its starting location, but hearing distance follows the player.
        public void RaiseAt(Vector3 position, UnityEngine.Object owner = null)
            => Send(new AudioSignalMessage(false, owner, true, position, null));

        // The source position follows this object as it moves.
        public void RaiseFollowing(Transform source, UnityEngine.Object owner = null)
        {
            if (source != null)
                Send(new AudioSignalMessage(false, owner != null ? owner : source, true, source.position, source));
        }

        // Single-argument forms can be wired through Unity's Inspector callbacks.
        public void RaiseAtObject(GameObject source)
        {
            if (source != null) RaiseAt(source.transform.position, source);
        }
        public void RaiseFollowingObject(GameObject source)
        {
            if (source != null) RaiseFollowing(source.transform, source);
        }

        // Stop this signal's owned loops; no owner means all its loops in the receiving Bridge.
        public void Stop() => Send(new AudioSignalMessage(true, null, false, default, null));
        public void StopFrom(UnityEngine.Object owner) => Send(new AudioSignalMessage(true, owner, false, default, null));

        internal void Subscribe(Action<AudioSignalMessage> listener)
        {
            listeners += listener;
            connectedSignals.Add(this);
        }

        internal void Unsubscribe(Action<AudioSignalMessage> listener)
        {
            listeners -= listener;
            if (listeners == null) connectedSignals.Remove(this);
        }

        private void Send(AudioSignalMessage message)
        {
            // Assets keep no playback state. Each enabled Bridge manages its own requests.
            listeners?.Invoke(message);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetListeners()
        {
            foreach (AudioSignal signal in connectedSignals)
                if (signal != null) signal.listeners = null;
            connectedSignals.Clear();
        }
    }

    // Information carried by the announcement; it contains no sound selection or game rules.
    internal readonly struct AudioSignalMessage
    {
        public readonly bool IsStop;
        public readonly UnityEngine.Object Owner;
        public readonly bool HasPosition;
        public readonly Vector3 Position;
        public readonly Transform FollowTarget;

        public AudioSignalMessage(bool isStop, UnityEngine.Object owner, bool hasPosition, Vector3 position, Transform followTarget)
        {
            IsStop = isStop;
            Owner = owner;
            HasPosition = hasPosition;
            Position = position;
            FollowTarget = followTarget;
        }
    }
}
