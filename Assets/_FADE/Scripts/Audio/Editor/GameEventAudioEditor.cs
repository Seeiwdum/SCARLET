using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Scarlet.Audio.EditorTools
{
    /// <summary>Finds event-bus members in the compiled game and writes ordinary typed C# subscriptions.</summary>
    [InitializeOnLoad]
    public static class GameEventAudioGenerator
    {
        private const string OutputPath = "Assets/_FADE/Scripts/Audio/GameEventCatalog.Generated.cs";

        public sealed class DiscoveredEvent
        {
            public string Key;
            public Type OwnerType;
            public string MemberName;
            public Type DelegateType;
            public ParameterInfo[] Parameters;
            public string Problem;
        }

        static GameEventAudioGenerator()
        {
            EditorApplication.delayCall += RefreshWhenReady;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall += RefreshWhenReady;
            };
        }

        private static void RefreshWhenReady()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            { EditorApplication.delayCall += RefreshWhenReady; return; }
            Refresh();
        }

        [MenuItem("Tools/SCARLET/Audio/Refresh Game Event List")]
        public static void Refresh()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            { Debug.LogWarning("Refresh the audio event list outside Play Mode."); return; }
            if (WriteIfChanged()) AssetDatabase.Refresh();
        }

        public static List<DiscoveredEvent> Discover()
        {
            var result = new List<DiscoveredEvent>();
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.IsDynamic || assembly.GetName().Name.StartsWith("Unity", StringComparison.Ordinal) ||
                    assembly.GetName().Name.StartsWith("System", StringComparison.Ordinal)) continue;
                Type[] types;
                try { types = assembly.GetTypes(); }
                catch (ReflectionTypeLoadException exception) { types = exception.Types.Where(type => type != null).ToArray(); }
                foreach (Type type in types)
                {
                    if (type.Name != "GameEvents" || !type.IsVisible || type.ContainsGenericParameters) continue;
                    const BindingFlags flags = BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly;
                    foreach (FieldInfo field in type.GetFields(flags))
                    {
                        if (!typeof(Delegate).IsAssignableFrom(field.FieldType)) continue;
                        Add(result, type, field.Name, field.FieldType, field.IsInitOnly || field.IsLiteral ? "Read-only delegate fields cannot be subscribed." : null);
                    }
                    foreach (EventInfo gameEvent in type.GetEvents(flags))
                    {
                        MethodInfo add = gameEvent.GetAddMethod();
                        MethodInfo remove = gameEvent.GetRemoveMethod();
                        Add(result, type, gameEvent.Name, gameEvent.EventHandlerType,
                            add == null || remove == null || !add.IsStatic || !remove.IsStatic ? "Needs public static add/remove methods." : null);
                    }
                }
            }
            foreach (var group in result.GroupBy(item => item.Key).Where(group => group.Count() > 1))
                foreach (DiscoveredEvent item in group) item.Problem = "Duplicate fully qualified event name across assemblies.";
            return result.OrderBy(item => item.Key, StringComparer.Ordinal).ToList();
        }

        private static void Add(List<DiscoveredEvent> result, Type owner, string name, Type delegateType, string problem)
        {
            MethodInfo invoke = delegateType.GetMethod("Invoke");
            ParameterInfo[] parameters = invoke.GetParameters();
            if (invoke.ReturnType != typeof(void)) problem = "Audio listens to announcements returning void.";
            if (!CanName(delegateType) || parameters.Any(parameter => !CanName(parameter.ParameterType)))
                problem = "Uses an inaccessible, ref/out, pointer, or unboxable parameter type.";
            result.Add(new DiscoveredEvent { Key = owner.FullName + "." + name, OwnerType = owner,
                MemberName = name, DelegateType = delegateType, Parameters = parameters, Problem = problem });
        }

        private static bool CanName(Type type)
        {
            if (type.IsByRef || type.IsPointer || type.ContainsGenericParameters || !type.IsVisible ||
                type.GetCustomAttributesData().Any(attribute => attribute.AttributeType.FullName == "System.Runtime.CompilerServices.IsByRefLikeAttribute")) return false;
            if (type.IsArray) return CanName(type.GetElementType());
            if (type.IsNested && type.DeclaringType.IsGenericType) return false;
            return !type.IsGenericType || type.GetGenericArguments().All(CanName);
        }

        public static string TypeName(Type type)
        {
            if (type.IsArray) return TypeName(type.GetElementType()) + "[" + new string(',', type.GetArrayRank() - 1) + "]";
            Type definition = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
            string fullName = definition.FullName.Replace('+', '.');
            int tick = fullName.IndexOf('`');
            if (tick >= 0) fullName = fullName.Substring(0, tick);
            string name = "global::" + string.Join(".", fullName.Split('.').Select(part => "@" + part));
            return type.IsGenericType ? name + "<" + string.Join(", ", type.GetGenericArguments().Select(TypeName)) + ">" : name;
        }

        public static string BuildSource(IEnumerable<DiscoveredEvent> discovered)
        {
            var text = new StringBuilder();
            text.AppendLine("// Generated by FADE's GameEventAudioGenerator. Refresh after editing GameEvents; do not edit this file by hand.");
            text.AppendLine("using System;");
            text.AppendLine("using System.Collections.Generic;");
            text.AppendLine("namespace Scarlet.Audio");
            text.AppendLine("{");
            text.AppendLine("    public static partial class GameEventCatalog");
            text.AppendLine("    {");
            text.AppendLine("        static partial void AddGeneratedEntries(List<Entry> result)");
            text.AppendLine("        {");
            foreach (DiscoveredEvent item in discovered.Where(item => item.Problem == null))
            {
                string arguments = string.Join(", ", item.Parameters.Select((parameter, index) => "p" + index));
                string types = string.Join(", ", item.Parameters.Select(parameter => "typeof(" + TypeName(parameter.ParameterType) + ")"));
                string member = TypeName(item.OwnerType) + ".@" + item.MemberName;
                text.AppendLine("            result.Add(new Entry(\"" + item.Key + "\", new Type[] { " + types + " }, receiver =>");
                text.AppendLine("            {");
                text.AppendLine("                " + TypeName(item.DelegateType) + " listener = (" + arguments + ") => receiver(" +
                    (item.Parameters.Length == 0 ? "Array.Empty<object>()" : "new object[] { " + arguments + " }") + ");");
                text.AppendLine("                " + member + " += listener;");
                text.AppendLine("                return () => " + member + " -= listener;");
                text.AppendLine("            }));");
            }
            text.AppendLine("        }");
            text.AppendLine("    }");
            text.AppendLine("}");
            return text.ToString().Replace("\r\n", "\n");
        }

        public static bool WriteIfChanged()
        {
            string content = BuildSource(Discover());
            if (File.Exists(OutputPath) && File.ReadAllText(OutputPath).Replace("\r\n", "\n") == content) return false;
            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
            File.WriteAllText(OutputPath, content, new UTF8Encoding(false));
            return true;
        }

        // Remove stale typed references before compiling edited gameplay. Otherwise removing
        // an event would prevent the editor generator itself from loading the new event list.
        public static bool ResetBeforeCompilation(IEnumerable<string> changedPaths)
        {
            if (!changedPaths.Any(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(path.Replace('\\', '/'), OutputPath, StringComparison.OrdinalIgnoreCase))) return false;
            if (!File.Exists(OutputPath)) return false;
            string empty = BuildSource(Array.Empty<DiscoveredEvent>());
            if (File.ReadAllText(OutputPath).Replace("\r\n", "\n") == empty) return false;
            File.WriteAllText(OutputPath, empty, new UTF8Encoding(false));
            return true;
        }
    }

    public sealed class GameEventAudioSourceChanges : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (GameEventAudioGenerator.ResetBeforeCompilation(imported.Concat(deleted).Concat(moved).Concat(movedFrom)))
                AssetDatabase.Refresh();
        }
    }

    // A build must not silently ship an out-of-date event list.
    public sealed class GameEventAudioBuildCheck : IPreprocessBuildWithReport
    {
        public int callbackOrder => -1000;
        public void OnPreprocessBuild(BuildReport report)
        {
            if (!GameEventAudioGenerator.WriteIfChanged()) return;
            AssetDatabase.Refresh();
            throw new BuildFailedException("Audio event connections were refreshed. Let Unity finish compiling, then build again.");
        }
    }

    [CustomEditor(typeof(AudioSignal))]
    public sealed class AudioSignalEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            SerializedProperty source = serializedObject.FindProperty("source");
            EditorGUILayout.PropertyField(source, new GUIContent("Signal Source"));
            if (source.enumValueIndex == (int)AudioSignal.SignalSource.ExistingGameEvent)
            {
                List<GameEventAudioGenerator.DiscoveredEvent> events = GameEventAudioGenerator.Discover();
                var usable = events.Where(item => item.Problem == null).ToList();
                GameEventAudioGenerator.DiscoveredEvent selected = DrawEvent("Game Event", "gameEventKey", usable, false);
                DrawBoolean("booleanMatch", "booleanArgument", selected);
                if (selected != null && selected.Parameters.Any(parameter => IsNumber(parameter.ParameterType)))
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("numberMatch"), new GUIContent("Number Filter"));
                    if (serializedObject.FindProperty("numberMatch").enumValueIndex != 0)
                    {
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("numberArgument"), new GUIContent("Number Argument Index"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("skipFirstNumber"), new GUIContent("Skip First Value"));
                        ValidateIndex("numberArgument", selected, IsNumber, "number");
                        EditorGUILayout.HelpBox("The first value is normally setup, not an action. Later values are compared with the previous value. This does not prove a hit happened.", MessageType.Info);
                    }
                }
                else if (serializedObject.FindProperty("numberMatch").enumValueIndex != 0)
                    EditorGUILayout.HelpBox("A number filter is set, but this event supplies no number. Reset Number Filter to Any before changing event types.", MessageType.Warning);

                EditorGUILayout.PropertyField(serializedObject.FindProperty("followEventObject"), new GUIContent("Follow Object Supplied By Event"));
                if (selected != null && !selected.Parameters.Any(parameter => HasPosition(parameter.ParameterType)))
                    EditorGUILayout.HelpBox("This event supplies no object or position. Keep Distance Volume off on its SoundCue.", MessageType.Info);
                var stop = DrawEvent("Stop Event (optional)", "stopGameEventKey", usable, true);
                if (stop != null) DrawBoolean("stopBooleanMatch", "stopBooleanArgument", stop);
                EditorGUILayout.HelpBox("The enabled scene Bridge connects this signal to its SoundCue. Gameplay does not need new AudioSignal calls. Avoid also assigning the same cue through the legacy adapter.", MessageType.Info);
                foreach (var item in events.Where(item => item.Problem != null))
                    EditorGUILayout.HelpBox(item.Key + ": " + item.Problem, MessageType.Warning);
                if (GUILayout.Button("Refresh Game Event List")) GameEventAudioGenerator.Refresh();
            }
            else EditorGUILayout.HelpBox("Manual signals still work for UnityEvents or gameplay that explicitly raises the assigned asset.", MessageType.Info);
            serializedObject.ApplyModifiedProperties();
        }

        private GameEventAudioGenerator.DiscoveredEvent DrawEvent(string label, string propertyName,
            List<GameEventAudioGenerator.DiscoveredEvent> events, bool optional)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            var names = new List<string> { optional ? "None" : "Select an event" };
            names.AddRange(events.Select(item => item.Key + " (" + string.Join(", ", item.Parameters.Select(parameter => parameter.ParameterType.Name)) + ")"));
            int current = events.FindIndex(item => item.Key == property.stringValue) + 1;
            bool missing = current == 0 && !string.IsNullOrEmpty(property.stringValue);
            if (missing) { names.Add("Missing: " + property.stringValue); current = names.Count - 1; }
            int next = EditorGUILayout.Popup(label, current, names.ToArray());
            if (next != current)
            {
                property.stringValue = next > 0 && next <= events.Count ? events[next - 1].Key : string.Empty;
                // An old filter must not silently block a newly chosen event of another type.
                if (propertyName == "gameEventKey")
                {
                    serializedObject.FindProperty("booleanMatch").enumValueIndex = 0;
                    serializedObject.FindProperty("numberMatch").enumValueIndex = 0;
                    serializedObject.FindProperty("booleanArgument").intValue = 0;
                    serializedObject.FindProperty("numberArgument").intValue = 0;
                }
                else
                {
                    serializedObject.FindProperty("stopBooleanMatch").enumValueIndex = 0;
                    serializedObject.FindProperty("stopBooleanArgument").intValue = 0;
                }
            }
            if (missing) EditorGUILayout.HelpBox("The selected event was removed or renamed. Choose an existing event.", MessageType.Warning);
            if (!string.IsNullOrEmpty(property.stringValue) && GameEventCatalog.Find(property.stringValue) == null)
                EditorGUILayout.HelpBox("This event is not in the compiled audio catalog yet. Refresh and wait for Unity to finish compiling.", MessageType.Warning);
            return events.FirstOrDefault(item => item.Key == property.stringValue);
        }

        private void DrawBoolean(string matchName, string indexName, GameEventAudioGenerator.DiscoveredEvent selected)
        {
            if (selected != null && selected.Parameters.Any(parameter => parameter.ParameterType == typeof(bool)))
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(matchName), new GUIContent("Boolean Filter"));
                if (serializedObject.FindProperty(matchName).enumValueIndex != 0)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(indexName), new GUIContent("Boolean Argument Index"));
                    ValidateIndex(indexName, selected, type => type == typeof(bool), "bool");
                }
            }
            else if (serializedObject.FindProperty(matchName).enumValueIndex != 0)
                EditorGUILayout.HelpBox("A boolean filter is set but this event supplies no bool. Reselect the event to clear the old filter.", MessageType.Warning);
        }

        private void ValidateIndex(string name, GameEventAudioGenerator.DiscoveredEvent selected, Func<Type, bool> matches, string label)
        {
            int index = serializedObject.FindProperty(name).intValue;
            if (index < 0 || index >= selected.Parameters.Length || !matches(selected.Parameters[index].ParameterType))
                EditorGUILayout.HelpBox("Argument index must select a " + label + ". Index 0 is the first argument.", MessageType.Warning);
        }

        private static bool HasPosition(Type type) => typeof(Component).IsAssignableFrom(type) || typeof(GameObject).IsAssignableFrom(type) || type == typeof(Vector2) || type == typeof(Vector3);
        private static bool IsNumber(Type type) => type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) || type == typeof(ushort) || type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong) || type == typeof(float) || type == typeof(double) || type == typeof(decimal);
    }
}
