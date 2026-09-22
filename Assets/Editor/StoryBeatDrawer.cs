#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(StoryBeat))]
public class StoryBeatDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Ermittelt den Listenindex aus dem Property-Pfad "beats.Array.data[X]"
        string path = property.propertyPath;
        int index = -1;
        int startIndex = path.IndexOf("[") + 1;
        int endIndex = path.IndexOf("]");
        if (startIndex > 0 && endIndex > startIndex)
        {
            int.TryParse(path.Substring(startIndex, endIndex - startIndex), out index);
        }

        // Beat-Name auslesen
        SerializedProperty nameProp = property.FindPropertyRelative("beatName");
        SerializedProperty disabledProp = property.FindPropertyRelative("isDisabled");

        string beatName = (nameProp != null && !string.IsNullOrEmpty(nameProp.stringValue))
            ? nameProp.stringValue
            : "Element";

        string disabledTag = (disabledProp != null && disabledProp.boolValue) ? " (Disabled)" : "";

        // Header-Titel neu zusammensetzen
        string customLabel = index >= 0
            ? $"[{index}] {beatName}{disabledTag}"
            : $"{beatName}{disabledTag}";

        label.text = customLabel;

        // Standard-Inspector-Zeichnung mit neuem Label aufrufen
        EditorGUI.PropertyField(position, property, label, true);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }
}
#endif