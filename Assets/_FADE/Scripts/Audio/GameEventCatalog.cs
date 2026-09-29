using System;
using System.Collections.Generic;

namespace Scarlet.Audio
{
    /// <summary>Generated, typed connections to the game's public static event bus.</summary>
    public static partial class GameEventCatalog
    {
        public sealed class Entry
        {
            public string Key { get; }
            public Type[] ArgumentTypes { get; }
            private readonly Func<Action<object[]>, Action> connect;

            public Entry(string key, Type[] argumentTypes, Func<Action<object[]>, Action> connect)
            { Key = key; ArgumentTypes = argumentTypes; this.connect = connect; }

            // Returns the exact unsubscribe operation, so existing gameplay listeners are preserved.
            public Action Subscribe(Action<object[]> receiver) => connect(receiver);
        }

        private static readonly List<Entry> entries = CreateEntries();
        public static IReadOnlyList<Entry> Entries => entries;

        private static List<Entry> CreateEntries()
        {
            var result = new List<Entry>();
            AddGeneratedEntries(result);
            return result;
        }

        // The editor generates this implementation. Without the optional generated file,
        // the reusable audio system still compiles in a game without a GameEvents bus.
        static partial void AddGeneratedEntries(List<Entry> result);

        public static Entry Find(string key)
        {
            foreach (Entry entry in entries)
                if (entry.Key == key) return entry;
            return null;
        }
    }
}
