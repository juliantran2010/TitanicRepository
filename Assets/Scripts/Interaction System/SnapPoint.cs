using System;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class SnapPoint : MonoBehaviour
{
    [Tooltip("Optionaler Filter: Welche Kategorie passt hierhin? (Leer = alles erlaubt)")]
    [SerializeField] private string acceptedCategory = "";

    [Tooltip("Optional: Feinausrichtung. Wenn leer, wird dieser Transform genutzt.")]
    [SerializeField] private Transform snapTransform;

    public MovableObject CurrentOccupant { get; private set; }
    public bool IsOccupied => CurrentOccupant != null;

    public event Action<MovableObject> OnObjectPlaced;
    public event Action<MovableObject> OnObjectRemoved;

    public Vector3 TargetPosition => snapTransform != null ? snapTransform.position : transform.position;
    public Quaternion TargetRotation => snapTransform != null ? snapTransform.rotation : transform.rotation;

    private BoxCollider col;

    private void Reset()
    {
        // Wird automatisch beim Hinzufügen der Komponente ausgeführt
        col = GetComponent<BoxCollider>();
        if (col != null)
        {
            col.isTrigger = true;
            col.size = new Vector3(0.2f, 0.05f, 0.3f);
        }
    }

    private void Awake()
    {
        col = GetComponent<BoxCollider>();
        col.isTrigger = true;
    }

    public bool CanAccept(MovableObject obj)
    {
        if (IsOccupied) return false;
        if (string.IsNullOrEmpty(acceptedCategory)) return true;
        return obj.Category == acceptedCategory;
    }

    public void Place(MovableObject obj)
    {
        CurrentOccupant = obj;
        OnObjectPlaced?.Invoke(obj);
    }

    public void Release()
    {
        if (CurrentOccupant != null)
        {
            var oldOccupant = CurrentOccupant;
            CurrentOccupant = null;
            OnObjectRemoved?.Invoke(oldOccupant);
        }
    }

    private void OnDrawGizmos()
    {
        if (col == null) col = GetComponent<BoxCollider>();
        if (col == null) return;

        // Position und Rotation des Objekts für das Gizmo übernehmen
        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);

        // Zeichnet exakt die Collider-Form (Rot = belegt, Cyan = frei)
        Gizmos.color = IsOccupied ? new Color(1f, 0f, 0f, 0.6f) : new Color(0f, 1f, 1f, 0.6f);
        Gizmos.DrawWireCube(col.center, col.size);

        // Ausrichtungs-Pfeile am Snap-Ziel
        Gizmos.color = Color.blue; // Vorwärts
        Gizmos.DrawRay(col.center, Vector3.forward * (col.size.z * 0.5f));
        Gizmos.color = Color.green; // Oben
        Gizmos.DrawRay(col.center, Vector3.up * 0.05f);

        Gizmos.matrix = oldMatrix;
    }
}