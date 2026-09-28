using System;

namespace Scarlet.Audio
{
    /// <summary>A safe reference to one playback. Old handles cannot stop a reused source.</summary>
    public readonly struct SoundHandle : IEquatable<SoundHandle>
    {
        internal readonly int Id;
        internal readonly int Generation;

        internal SoundHandle(int id, int generation)
        {
            Id = id;
            Generation = generation;
        }

        public bool IsValid => Id != 0;

        public bool Equals(SoundHandle other) => Id == other.Id && Generation == other.Generation;
        public override bool Equals(object obj) => obj is SoundHandle other && Equals(other);
        public override int GetHashCode() => (Id * 397) ^ Generation;
        public static bool operator ==(SoundHandle left, SoundHandle right) => left.Equals(right);
        public static bool operator !=(SoundHandle left, SoundHandle right) => !left.Equals(right);
    }
}
