using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MonoBehaviour), true)]
[CanEditMultipleObjects]
public class DictionaryDebugInspector : Editor
{
    private static readonly Dictionary<string, bool> Foldouts = new Dictionary<string, bool>();

    public override void OnInspectorGUI()
    {
        // 1. Ganz normalen Standard-Inspector für alles andere zeichnen
        DrawDefaultInspector();

        // 2. Dictionaries existieren mit echten Werten nur im PlayMode
        if (!Application.isPlaying) return;

        MonoBehaviour mb = target as MonoBehaviour;
        if (mb == null) return;

        // Hole alle Felder (public, private, protected)
        FieldInfo[] fields = mb.GetType().GetFields(
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic |
            BindingFlags.DeclaredOnly);

        bool hasDrawnHeader = false;

        foreach (FieldInfo field in fields)
        {
            Type fieldType = field.FieldType;

            // Prüfen, ob das Feld ein IDictionary ist (z. B. Dictionary<string, int>)
            if (typeof(IDictionary).IsAssignableFrom(fieldType))
            {
                IDictionary dict = field.GetValue(mb) as IDictionary;

                if (!hasDrawnHeader)
                {
                    EditorGUILayout.Space(12);
                    EditorGUILayout.LabelField("Live Dictionaries (Runtime Debug)", EditorStyles.boldLabel);
                    hasDrawnHeader = true;
                }

                DrawDictionary(field.Name, dict);
            }
        }
    }

    private void DrawDictionary(string fieldName, IDictionary dict)
    {
        string foldoutKey = target.GetInstanceID() + "_" + fieldName;
        if (!Foldouts.ContainsKey(foldoutKey)) Foldouts[foldoutKey] = true;

        int count = dict != null ? dict.Count : 0;
        string labelText = $"{fieldName} ({count} Einträge)";

        Foldouts[foldoutKey] = EditorGUILayout.Foldout(Foldouts[foldoutKey], labelText, true);

        if (!Foldouts[foldoutKey]) return;

        EditorGUI.indentLevel++;

        if (dict == null)
        {
            EditorGUILayout.LabelField("null", EditorStyles.miniLabel);
            EditorGUI.indentLevel--;
            return;
        }

        if (count == 0)
        {
            EditorGUILayout.LabelField("(Leer)", EditorStyles.miniLabel);
            EditorGUI.indentLevel--;
            return;
        }

        // Tabelle mit Key -> Value zeichnen
        foreach (DictionaryEntry entry in dict)
        {
            string keyStr = entry.Key != null ? entry.Key.ToString() : "null";
            string valStr = entry.Value != null ? entry.Value.ToString() : "null";

            EditorGUILayout.BeginHorizontal();

            // Key (links)
            EditorGUILayout.LabelField(keyStr, GUILayout.Width(130));

            // Value (rechts) – wenn es ein Unity-Objekt ist, hebe es hervor
            if (entry.Value is UnityEngine.Object unityObj)
            {
                EditorGUILayout.ObjectField(unityObj, typeof(UnityEngine.Object), true);
            }
            else
            {
                EditorGUILayout.SelectableLabel(valStr, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUI.indentLevel--;
    }
}