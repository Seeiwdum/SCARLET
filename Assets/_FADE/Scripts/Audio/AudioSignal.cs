using System;
using System.Collections.Generic;
using UnityEngine;

namespace Scarlet.Audio
{
    /// <summary>A reusable announcement asset. Create another asset for any new action; no action enum edits.</summary>
    [CreateAssetMenu(menuName = "SCARLET/Audio/Audio Signal", fileName = "Audio Signal")]
    public sealed class AudioSignal : ScriptableObject
    {
        public enum SignalSource { ManualCalls, ExistingGameEvent }
        public enum BooleanMatch { Any, True, False }
        public enum NumberMatch { Any, Changed, Increased, Decreased }

        [SerializeField] private SignalSource source;
        [SerializeField] private string gameEventKey;
        [SerializeField] private BooleanMatch booleanMatch;
        [SerializeField, Min(0)] private int booleanArgument;
        [SerializeField] private NumberMatch numberMatch;
        [SerializeField, Min(0)] private int numberArgument;
        [SerializeField] private bool skipFirstNumber = true;
        [Tooltip("Follow the first scene object supplied by the event when it supplies no explicit Vector2/Vector3 position.")]
        [SerializeField] private bool followEventObject = true;
        [SerializeField] private string stopGameEventKey;
        [SerializeField] private BooleanMatch stopBooleanMatch;
        [SerializeField, Min(0)] private int stopBooleanArgument;

        private event Action<AudioSignalMessage> listeners;
        private static readonly HashSet<AudioSignal> connectedSignals = new HashSet<AudioSignal>();
        private Action disconnectEvent;
        private Action disconnectStopEvent;
        private bool hasPreviousNumber;
        private double previousNumber;

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
            bool firstListener = listeners == null;
            listeners += listener;
            connectedSignals.Add(this);
            if (firstListener) ConnectGameEvents();
        }

        internal void Unsubscribe(Action<AudioSignalMessage> listener)
        {
            listeners -= listener;
            if (listeners == null)
            {
                DisconnectGameEvents();
                connectedSignals.Remove(this);
            }
        }

        private void ConnectGameEvents()
        {
            hasPreviousNumber = false;
            if (source != SignalSource.ExistingGameEvent) return;
            GameEventCatalog.Entry entry = GameEventCatalog.Find(gameEventKey);
            if (entry == null)
            {
                Debug.LogWarning($"AudioSignal '{name}' cannot find event '{gameEventKey}'. Refresh the event list in its Inspector.", this);
                return;
            }
            disconnectEvent = entry.Subscribe(arguments => ReceiveGameEvent(arguments, false));
            if (string.IsNullOrEmpty(stopGameEventKey)) return;
            GameEventCatalog.Entry stopEntry = GameEventCatalog.Find(stopGameEventKey);
            if (stopEntry == null)
                Debug.LogWarning($"AudioSignal '{name}' cannot find its stop event '{stopGameEventKey}'.", this);
            else disconnectStopEvent = stopEntry.Subscribe(arguments => ReceiveGameEvent(arguments, true));
        }

        private void DisconnectGameEvents()
        {
            disconnectEvent?.Invoke();
            disconnectStopEvent?.Invoke();
            disconnectEvent = disconnectStopEvent = null;
            hasPreviousNumber = false;
        }

        private void OnValidate()
        {
            booleanArgument = Mathf.Max(0, booleanArgument);
            numberArgument = Mathf.Max(0, numberArgument);
            stopBooleanArgument = Mathf.Max(0, stopBooleanArgument);
            // Editing the selection during Play Mode must remove the old connection first.
            if (listeners != null)
            {
                DisconnectGameEvents();
                ConnectGameEvents();
            }
        }

        private void ReceiveGameEvent(object[] arguments, bool stopping)
        {
            BooleanMatch match = stopping ? stopBooleanMatch : booleanMatch;
            int index = stopping ? stopBooleanArgument : booleanArgument;
            if (match != BooleanMatch.Any &&
                (index >= arguments.Length || !(arguments[index] is bool value) || value != (match == BooleanMatch.True))) return;

            if (!stopping && numberMatch != NumberMatch.Any)
            {
                if (numberArgument >= arguments.Length || !TryNumber(arguments[numberArgument], out double number)) return;
                bool hadPrevious = hasPreviousNumber;
                double before = previousNumber;
                previousNumber = number;
                hasPreviousNumber = true;
                if (!hadPrevious)
                {
                    if (skipFirstNumber) return;
                }
                else if ((numberMatch == NumberMatch.Changed && number == before) ||
                         (numberMatch == NumberMatch.Increased && number <= before) ||
                         (numberMatch == NumberMatch.Decreased && number >= before)) return;
            }

            UnityEngine.Object owner = null;
            Transform target = null;
            bool hasPosition = false;
            Vector3 position = default;
            // Only information actually supplied by gameplay is used. LoreData is not a sender.
            foreach (object argument in arguments)
            {
                if (target == null && argument is Component component && component != null)
                { owner = component; target = component.transform; }
                else if (target == null && argument is GameObject gameObject && gameObject != null)
                { owner = gameObject; target = gameObject.transform; }
                if (!hasPosition && argument is Vector3 point) { position = point; hasPosition = true; }
                else if (!hasPosition && argument is Vector2 point2) { position = new Vector3(point2.x, point2.y, 0f); hasPosition = true; }
            }
            bool explicitPosition = hasPosition;
            if (!hasPosition && target != null) { position = target.position; hasPosition = true; }
            Send(new AudioSignalMessage(stopping, owner, hasPosition, position, followEventObject && !explicitPosition ? target : null));
        }

        private static bool TryNumber(object value, out double result)
        {
            result = 0d;
            if (!(value is byte || value is sbyte || value is short || value is ushort || value is int ||
                  value is uint || value is long || value is ulong || value is float || value is double || value is decimal)) return false;
            result = Convert.ToDouble(value);
            return !double.IsNaN(result);
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
                if (signal != null)
                {
                    signal.DisconnectGameEvents();
                    signal.listeners = null;
                }
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
