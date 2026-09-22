using UnityEditor;
using UnityEngine;

public class BatchMaterialAssigner : EditorWindow
{
    private Material targetMaterial;

    [MenuItem("Tools/Assign Material To Selection & Children")]
    public static void ShowWindow()
    {
        GetWindow<BatchMaterialAssigner>("Material Assigner");
    }

    private void OnGUI()
    {
        GUILayout.Label("Material zuweisen", EditorStyles.boldLabel);
        targetMaterial = (Material)EditorGUILayout.ObjectField("Material", targetMaterial, typeof(Material), false);

        if (GUILayout.Button("Auf Auswahl und alle Children anwenden"))
        {
            if (targetMaterial == null)
            {
                Debug.LogWarning("Bitte zuerst ein Material auswählen!");
                return;
            }

            int count = 0;
            foreach (GameObject root in Selection.gameObjects)
            {
                MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>(true);
                foreach (MeshRenderer rend in renderers)
                {
                    Undo.RecordObject(rend, "Assign Material");
                    rend.sharedMaterial = targetMaterial;
                    count++;
                }
            }

            Debug.Log($"Material '{targetMaterial.name}' an {count} MeshRenderer zugewiesen.");
        }
    }
}