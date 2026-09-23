using UnityEngine;
using DG.Tweening;
using UnityEngine.Events;

public class OpenableObject : InteractableObject
{
    public override InteractionType Type => InteractionType.Open;

    [Header("Bewegung (Relativ)")]
    [Tooltip("z. B. (0, 0, 0.3) für Schublade aufziehen. Auf 0 lassen wenn keine Verschiebung")]
    [SerializeField] private Vector3 moveBy = Vector3.zero;
    [Tooltip("z. B. (0, 90, 0) für Tür aufdrehen. Auf 0 lassen wenn keine Drehung")]
    [SerializeField] private Vector3 rotateBy = Vector3.zero;
    [SerializeField] private float duration = 0.5f;
    [SerializeField] private Ease ease = Ease.InOutQuad;

    [Header("Editor Preview")]
    [SerializeField] private bool showGizmo = true;
    [SerializeField] private Color gizmoColor = new Color(0f, 1f, 0.5f, 0.6f);

    private Vector3 startPos;
    private Vector3 startRot;
    public bool IsOpened { get; private set; } = false;
    public UnityEvent OnOpened;
    public UnityEvent OnClosed;

    protected override void Start()
    {
        base.Start();
        startPos = transform.localPosition;
        startRot = transform.localEulerAngles;
    }

    protected override void OnInteract()
    {
        IsOpened = !IsOpened;
        SetPersistentStateValue("is_opened", IsOpened);
        if (IsOpened)
            OnOpened?.Invoke();
        else
            OnClosed?.Invoke();

        // Zielwerte: Entweder Offset draufrechnen oder zurück auf Start
        Vector3 targetPos = IsOpened ? startPos + moveBy : startPos;
        Vector3 targetRot = IsOpened ? startRot + rotateBy : startRot;

        transform.DOKill();

        if (moveBy != Vector3.zero)
        {
            transform.DOLocalMove(targetPos, duration).SetEase(ease);
        }

        if (rotateBy != Vector3.zero)
        {
            transform.DOLocalRotate(targetRot, duration).SetEase(ease);
        }
    }

    protected override void OnStateRestored()
    {
        if (ContainsPersistentState("is_opened"))
        {
            IsOpened = GetPersistentStateValue("is_opened", false);
            if (IsOpened)
            {
                Vector3 targetPos = IsOpened ? startPos + moveBy : startPos;
                Vector3 targetRot = IsOpened ? startRot + rotateBy : startRot;
                transform.localPosition = targetPos;
                transform.localRotation = Quaternion.Euler(targetRot);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!showGizmo) return;
        if (moveBy == Vector3.zero && rotateBy == Vector3.zero) return;

        // Start-Werte berechnen: Wenn das Spiel läuft, nutzen wir startPos/startRot, im Editor transform.local
        Vector3 baseLocalPos = Application.isPlaying ? startPos : (IsOpened ? startPos : transform.localPosition);
        Vector3 baseLocalRot = Application.isPlaying ? startRot : (IsOpened ? startRot : transform.localEulerAngles);

        Vector3 targetLocalPos = baseLocalPos + moveBy;
        Quaternion targetLocalRot = Quaternion.Euler(baseLocalRot + rotateBy);

        // In Welt-Koordinaten umwandeln
        Vector3 targetWorldPos;
        Quaternion targetWorldRot;

        if (transform.parent != null)
        {
            targetWorldPos = transform.parent.TransformPoint(targetLocalPos);
            targetWorldRot = transform.parent.rotation * targetLocalRot;
        }
        else
        {
            targetWorldPos = targetLocalPos;
            targetWorldRot = targetLocalRot;
        }

        Vector3 currentWorldPos = Application.isPlaying && IsOpened && transform.parent != null
            ? transform.parent.TransformPoint(baseLocalPos)
            : transform.position;

        // Pfadlinie von Start zur Zielposition
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(currentWorldPos, targetWorldPos);

        // Gizmo-Matrix für Ausrichtung und Skalierung der geöffneten Position setzen
        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(targetWorldPos, targetWorldRot, transform.lossyScale);

        // Geöffnete Form zeichnen
        Gizmos.color = gizmoColor;

        var boxCol = GetComponent<BoxCollider>();
        var meshFilter = GetComponent<MeshFilter>();

        if (boxCol != null)
        {
            // Wenn ein BoxCollider da ist: Exakte Box am Zielort anzeigen
            Gizmos.DrawWireCube(boxCol.center, boxCol.size);
        }
        else if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            // Wenn kein BoxCollider da ist, aber ein Mesh: Mesh-Bounds am Zielort zeichnen
            Gizmos.DrawWireCube(meshFilter.sharedMesh.bounds.center, meshFilter.sharedMesh.bounds.size);
        }
        else
        {
            // Fallback
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one * 0.2f);
        }

        // Richtungs-Pfeile an der Zielposition (Blau = Vorwärts, Grün = Oben)
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(Vector3.zero, Vector3.forward * 0.25f);
        Gizmos.color = Color.green;
        Gizmos.DrawRay(Vector3.zero, Vector3.up * 0.15f);

        Gizmos.matrix = oldMatrix;
    }
}