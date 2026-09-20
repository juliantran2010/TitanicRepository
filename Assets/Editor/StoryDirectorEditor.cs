using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(StoryDirector))]
public class StoryDirectorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Zeichnet alle normalen Inspector-Felder (Drehbuch, startAtIndex, etc.)
        DrawDefaultInspector();

        StoryDirector director = (StoryDirector)target;

        // Visueller Abstand
        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("Debug Tools", EditorStyles.boldLabel);

        // Buttons nur aktivieren, wenn das Spiel auch läuft
        GUI.enabled = Application.isPlaying;

        if (GUILayout.Button("Force Trigger Current Beat", GUILayout.Height(32)))
        {
            director.DebugForceTriggerCurrentBeat();
        }

        if (GUILayout.Button("Skip To Next Beat", GUILayout.Height(24)))
        {
            director.DebugSkipToNextBeat();
        }

        GUI.enabled = true;

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Buttons sind nur im PlayMode aktiv.", MessageType.Info);
        }
    }
}