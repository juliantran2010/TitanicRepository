using UnityEngine;
using UnityEditor;

public class MailGridFiller : EditorWindow
{
    private int columns = 8;
    private int rows = 7;

    [Header("Fach-Dimensionen (Weltmaße)")]
    private float gridWidth = 1.6f;
    private float gridHeight = 1.3f;

    [Header("Boxen-Größe")]
    [Range(0.1f, 1.2f)] private float widthFactor = 0.75f;
    [Range(0.05f, 1.2f)] private float heightFactor = 0.45f;
    [Range(0f, 0.5f)] private float heightVariation = 0.15f;
    private float boxDepth = 0.2f;

    [Header("Flache Drehung (Bleibt auf dem Boden)")]
    [Tooltip("Dreht die Boxen nur auf der Stellfläche nach links/rechts (ohne Kippen)")]
    [Range(0f, 25f)] private float randomYawAngle = 8f;

    [Header("Farbgestaltung")]
    private Color baseColor = new Color(0.72f, 0.62f, 0.48f);
    [Range(0f, 0.5f)] private float colorVariation = 0.12f;

    [Header("Rotation & Achsen-Korrektur")]
    private bool swapAxes = true;
    private Vector3 gridRotationEuler = Vector3.zero;

    [Header("Verschiebung / Offset")]
    private bool useWorldSpaceOffset = true;
    private Vector3 manualOffset = Vector3.zero;

    [Range(0f, 1f)]
    private float fillChance = 0.65f;

    [MenuItem("Tools/Mail Grid Filler")]
    public static void ShowWindow()
    {
        GetWindow<MailGridFiller>("Mail Grid Filler");
    }

    private void OnGUI()
    {
        GUILayout.Label("Brieffach Raster Befüller", EditorStyles.boldLabel);

        columns = EditorGUILayout.IntField("Spalten (Columns)", columns);
        rows = EditorGUILayout.IntField("Reihen (Rows)", rows);

        EditorGUILayout.Space();
        gridWidth = EditorGUILayout.FloatField("Gesamtbreite Raster", gridWidth);
        gridHeight = EditorGUILayout.FloatField("Gesamthöhe Raster", gridHeight);

        EditorGUILayout.Space();
        GUILayout.Label("Boxen-Größe:", EditorStyles.boldLabel);
        widthFactor = EditorGUILayout.Slider("Breiten-Faktor Fach", widthFactor, 0.1f, 1.2f);
        heightFactor = EditorGUILayout.Slider("Höhen-Faktor Fach", heightFactor, 0.05f, 1.2f);
        heightVariation = EditorGUILayout.Slider("Zufällige Höhen-Varianz", heightVariation, 0f, 0.5f);
        boxDepth = EditorGUILayout.FloatField("Tiefe der Boxen", boxDepth);

        EditorGUILayout.Space();
        GUILayout.Label("Organische Unordnung:", EditorStyles.boldLabel);
        randomYawAngle = EditorGUILayout.Slider("Drehung auf Boden (°)", randomYawAngle, 0f, 25f);

        EditorGUILayout.Space();
        GUILayout.Label("Farbe & Material:", EditorStyles.boldLabel);
        baseColor = EditorGUILayout.ColorField("Grundfarbe", baseColor);
        colorVariation = EditorGUILayout.Slider("Farb-/Helligkeits-Varianz", colorVariation, 0f, 0.5f);

        EditorGUILayout.Space();
        GUILayout.Label("Rotation & Ausrichtung:", EditorStyles.boldLabel);
        swapAxes = EditorGUILayout.Toggle("Achsen vertauschen (X <-> Y)", swapAxes);
        gridRotationEuler = EditorGUILayout.Vector3Field("Rotation (Euler)", gridRotationEuler);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+90° Y")) gridRotationEuler.y = (gridRotationEuler.y + 90f) % 360f;
        if (GUILayout.Button("+90° Z (Uhrzeiger)")) gridRotationEuler.z = (gridRotationEuler.z - 90f) % 360f;
        if (GUILayout.Button("+90° X")) gridRotationEuler.x = (gridRotationEuler.x + 90f) % 360f;
        if (GUILayout.Button("Reset Rot")) gridRotationEuler = Vector3.zero;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        GUILayout.Label("Offset Verschiebung:", EditorStyles.boldLabel);
        useWorldSpaceOffset = EditorGUILayout.Toggle("Offset nutzt Welt-Achsen", useWorldSpaceOffset);
        manualOffset = EditorGUILayout.Vector3Field("Verschiebung (X, Y, Z in m)", manualOffset);

        fillChance = EditorGUILayout.Slider("Füll-Wahrscheinlichkeit", fillChance, 0f, 1f);

        GUILayout.Space(12);

        if (GUILayout.Button("Selektierte Regale füllen", GUILayout.Height(30)))
        {
            FillSelectedShelves();
        }

        if (GUILayout.Button("Erzeugte Boxen wieder löschen"))
        {
            ClearSpawnedBoxes();
        }
    }

    private void FillSelectedShelves()
    {
        Shader defaultShader = Shader.Find("Universal Render Pipeline/Lit");
        if (defaultShader == null) defaultShader = Shader.Find("Standard");

        foreach (GameObject shelf in Selection.gameObjects)
        {
            Undo.RegisterFullObjectHierarchyUndo(shelf, "Fill Mail Shelves");

            Transform existingContainer = shelf.transform.Find("Generated_MailItems");
            if (existingContainer != null)
            {
                DestroyImmediate(existingContainer.gameObject);
            }

            GameObject container = new GameObject("Generated_MailItems");
            container.transform.SetParent(shelf.transform, false);

            Renderer rend = shelf.GetComponentInChildren<Renderer>();
            Vector3 centerWorld = rend != null ? rend.bounds.center : shelf.transform.position;

            Quaternion baseRotation = shelf.transform.rotation * Quaternion.Euler(gridRotationEuler);
            Vector3 rightDir = baseRotation * Vector3.right;
            Vector3 upDir = baseRotation * Vector3.up;
            Vector3 forwardDir = baseRotation * Vector3.forward;

            Vector3 finalOffsetWorld = useWorldSpaceOffset
                ? manualOffset
                : (rightDir * manualOffset.x) + (upDir * manualOffset.y) + (forwardDir * manualOffset.z);

            float activeGridWidth = swapAxes ? gridHeight : gridWidth;
            float activeGridHeight = swapAxes ? gridWidth : gridHeight;
            int activeCols = swapAxes ? rows : columns;
            int activeRows = swapAxes ? columns : rows;

            float cellWidth = activeGridWidth / activeCols;
            float cellHeight = activeGridHeight / activeRows;

            for (int r = 0; r < activeRows; r++)
            {
                for (int c = 0; c < activeCols; c++)
                {
                    if (Random.value > fillChance) continue;

                    float offsetX = ((c + 0.5f) - (activeCols * 0.5f)) * cellWidth;
                    float offsetY = ((r + 0.5f) - (activeRows * 0.5f)) * cellHeight;

                    Vector3 worldPos = centerWorld
                                       + rightDir * offsetX
                                       + upDir * offsetY
                                       + finalOffsetWorld;

                    GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    box.name = $"Mail_{r}_{c}";

                    Collider col = box.GetComponent<Collider>();
                    if (col != null) DestroyImmediate(col);

                    box.transform.position = worldPos;

                    // Basis-Ausrichtung anwenden
                    box.transform.rotation = baseRotation;

                    // Flache Drehung NUR um die echte Vertikale (Welt-Oben)
                    // Verhindert jedes Kippen oder Durchdringen des Fachbodens
                    if (randomYawAngle > 0f)
                    {
                        float yaw = Random.Range(-randomYawAngle, randomYawAngle);
                        box.transform.Rotate(Vector3.up, yaw, Space.World);
                    }

                    float chosenWidthFactor = swapAxes ? heightFactor : widthFactor;
                    float chosenHeightFactor = swapAxes ? widthFactor : heightFactor;

                    float actualW = cellWidth * chosenWidthFactor;
                    float actualH = cellHeight * (chosenHeightFactor + Random.Range(-heightVariation, heightVariation));
                    actualH = Mathf.Max(actualH, 0.02f);

                    Vector3 targetScaleWorld = new Vector3(actualW, actualH, boxDepth);

                    box.transform.SetParent(container.transform, true);
                    SetWorldScale(box.transform, targetScaleWorld);

                    // Material mit Farbnuancen
                    Renderer boxRend = box.GetComponent<Renderer>();
                    if (boxRend != null && defaultShader != null)
                    {
                        Material mat = new Material(defaultShader);
                        float shade = Random.Range(-colorVariation, colorVariation);
                        Color variedColor = new Color(
                            Mathf.Clamp01(baseColor.r + shade),
                            Mathf.Clamp01(baseColor.g + shade * 0.9f),
                            Mathf.Clamp01(baseColor.b + shade * 0.8f)
                        );
                        mat.color = variedColor;
                        boxRend.sharedMaterial = mat;
                    }
                }
            }
        }
    }

    private void SetWorldScale(Transform t, Vector3 worldScale)
    {
        Vector3 parentLossy = t.parent != null ? t.parent.lossyScale : Vector3.one;
        t.localScale = new Vector3(
            parentLossy.x != 0 ? worldScale.x / parentLossy.x : worldScale.x,
            parentLossy.y != 0 ? worldScale.y / parentLossy.y : worldScale.y,
            parentLossy.z != 0 ? worldScale.z / parentLossy.z : worldScale.z
        );
    }

    private void ClearSpawnedBoxes()
    {
        foreach (GameObject shelf in Selection.gameObjects)
        {
            Transform existingContainer = shelf.transform.Find("Generated_MailItems");
            if (existingContainer != null)
            {
                Undo.DestroyObjectImmediate(existingContainer.gameObject);
            }
        }
    }
}