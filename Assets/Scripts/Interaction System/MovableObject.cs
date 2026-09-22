using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

public class MovableObject : InteractableObject
{
    public override InteractionType Type => InteractionType.Move;

    [Header("Movable Settings")]
    [Tooltip("Kategorie für SnapPoint- oder Flächenfilter (z. B. 'Default', 'Tool', 'Document')")]
    [SerializeField] private string category = "Default";
    public string Category => category;

    [Tooltip("Layer der Flächen, auf denen abgelegt werden darf")]
    [SerializeField] private LayerMask placementLayerMask = ~0;

    [Tooltip("Maximale Reichweite beim Tragen und Platzieren")]
    [SerializeField] private float maxReachDistance = 3.5f;

    [Header("Dynamisches Heranziehen")]
    [Tooltip("Prozentsatz der ursprünglichen Distanz (z. B. 0.55 = wird 45% näher zum Spieler gezogen)")]
    [Range(0.2f, 0.9f)]
    [SerializeField] private float pullRatio = 0.55f;

    [Tooltip("Absoluter Mindestabstand zur Kamera, damit nichts in die Linse clippt")]
    [SerializeField] private float minCameraDistance = 0.35f;

    [Tooltip("Maximaler Schwebabstand, falls man beim Greifen sehr weit weg stand")]
    [SerializeField] private float maxCameraDistance = 1.3f;

    [Header("Placement Offset & Clipping Schutz")]
    [Tooltip("Kleiner technischer Abstand zur Zielfläche gegen Z-Fighting")]
    [SerializeField] private float surfaceOffset = 0.005f;

    [Tooltip("Zusätzlicher manueller Abstand zur Ablagestelle entlang der Flächennormale (z. B. 0.02f gegen Einsinken in den Boden)")]
    [SerializeField] private float dropSurfacePadding = 0.01f;

    [Tooltip("Berechnet automatisch den halben Collider-Umfang zur Ablagefläche dazu, damit der Pivot nicht im Boden landet")]
    [SerializeField] private bool autoCalculatePaddingFromCollider = false;

    [Header("Follow & Animation Settings")]
    [Tooltip("Geschwindigkeit der Nachführung beim Tragen")]
    [SerializeField] private float followSpeed = 20f;

    [Tooltip("Dauer der Ablage-Animation")]
    [SerializeField] private float dropAnimationDuration = 0.25f;

    [Tooltip("Easing-Kurve für das Ablegen")]
    [SerializeField] private Ease dropEase = Ease.OutQuad;

    [Header("Snap Settings")]
    [Tooltip("Darf ein SnapPoint belegt werden, wenn dort bereits ein anderes Objekt liegt (Ersetzt/Tauscht das bisherige Objekt)?")]
    [SerializeField] private bool allowDropOnOccupied = true;

    [Tooltip("Zusätzlicher Höhenversatz beim Hovern über einem BEREITS BELEGTEN Slot")]
    [SerializeField] private float occupiedHoverHeight = 0.04f;

    [Tooltip("Darf das Objekt NUR an SnapPoints abgelegt werden (kein freies Ablegen auf Böden/Tischen)?")]
    [SerializeField] private bool snapPointOnly = false;

    public bool IsHeld { get; private set; } = false;
    public bool IsDropping { get; private set; } = false;
    public SnapPoint CurrentSnapPoint { get; private set; }

    private Camera mainCam;
    private Collider objCollider;
    private Rigidbody rb;

    // Gespeicherte Werte beim Aufheben
    private Quaternion originalRotation;
    private Quaternion initialRotation;
    private float dynamicHoldDistance;
    private Tween currentDropTween;

    protected override void Start()
    {
        base.Start();
        mainCam = Camera.main;
        objCollider = GetComponent<Collider>();
        rb = GetComponent<Rigidbody>();
        originalRotation = transform.rotation;
    }

    protected override void OnInteract()
    {
        if (!IsHeld && !IsDropping)
        {
            PickUp();
        }
    }

    private void Update()
    {
        if (!IsHeld || IsDropping) return;

        UpdateHeldPosition();

        bool leftClick = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        if (leftClick && !InteractionIsOnCooldown)
        {
            TryDrop();
        }
    }

    public void SetCollider(bool active)
    {
        if (objCollider != null)
        {
            objCollider.enabled = active;
        }
    }

    public virtual void PickUp()
    {
        if (currentDropTween != null && currentDropTween.IsActive())
        {
            currentDropTween.Kill();
        }

        IsDropping = false;
        IsHeld = true;

        if (mainCam == null) mainCam = Camera.main;

        if (CurrentSnapPoint != null)
        {
            initialRotation = originalRotation;
            CurrentSnapPoint.Release();
            CurrentSnapPoint = null;
        }
        else
        {
            initialRotation = transform.rotation;
        }

        if (mainCam != null)
        {
            float currentDist = Vector3.Distance(mainCam.transform.position, transform.position);
            dynamicHoldDistance = Mathf.Clamp(currentDist * pullRatio, minCameraDistance, maxCameraDistance);
        }
        else
        {
            dynamicHoldDistance = 0.7f;
        }

        if (rb != null) rb.isKinematic = true;
        if (objCollider != null) objCollider.enabled = false;
        InteractionManager.Instance.ShouldInteract = false;
    }

    private SnapPoint FindTargetSnapPoint(RaycastHit[] hits, out RaycastHit snapHit)
    {
        snapHit = default;
        foreach (var hit in hits)
        {
            if (hit.collider.TryGetComponent<SnapPoint>(out var point))
            {
                if (!point.CanAccept(this)) continue;

                // Falls der Slot belegt ist, aber kein Ablegen auf belegte Slots erlaubt ist -> überspringen
                if (!allowDropOnOccupied && point.IsOccupied && point.CurrentOccupant != this)
                {
                    continue;
                }

                snapHit = hit;
                return point;
            }
        }
        return null;
    }

    private void UpdateHeldPosition()
    {
        if (mainCam == null)
        {
            mainCam = Camera.main;
            if (mainCam == null) return;
        }

        Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 targetPos;
        Quaternion targetRot = initialRotation;

        RaycastHit[] hits = Physics.RaycastAll(ray, maxReachDistance, placementLayerMask);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        SnapPoint targetSnapPoint = FindTargetSnapPoint(hits, out RaycastHit snapHit);

        if (targetSnapPoint != null)
        {
            targetRot = targetSnapPoint.TargetRotation;

            if (targetSnapPoint.IsOccupied && targetSnapPoint.CurrentOccupant != this)
            {
                Vector3 toCamera = (mainCam.transform.position - targetSnapPoint.TargetPosition).normalized;
                targetPos = targetSnapPoint.TargetPosition + (toCamera * occupiedHoverHeight);
            }
            else
            {
                targetPos = targetSnapPoint.TargetPosition;
            }
        }
        else
        {
            float targetDistance = dynamicHoldDistance;

            if (hits.Length > 0)
            {
                // Erster physischer Treffer (z. B. Tisch oder Wand)
                RaycastHit firstHit = hits[0];
                if (firstHit.distance < dynamicHoldDistance)
                {
                    targetDistance = Mathf.Max(firstHit.distance - surfaceOffset, minCameraDistance);
                }
            }

            targetPos = ray.origin + ray.direction * targetDistance;
            targetRot = initialRotation;
        }

        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * followSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * followSpeed);
    }

    private void TryDrop()
    {
        if (mainCam == null) return;

        Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        RaycastHit[] hits = Physics.RaycastAll(ray, maxReachDistance, placementLayerMask);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        SnapPoint point = FindTargetSnapPoint(hits, out _);

        // 1. Priorität: SnapPoint gefunden
        if (point != null)
        {
            AnimateDrop(point.TargetPosition, point.TargetRotation, () =>
            {
                SnapToPoint(point);
                FinishDrop();
            });
            return;
        }

        // 2. Freie Fläche (falls erlaubt)
        if (snapPointOnly || hits.Length == 0) return;

        RaycastHit surfaceHit = hits[0];

        float totalPadding = surfaceOffset + dropSurfacePadding;

        if (autoCalculatePaddingFromCollider && objCollider != null)
        {
            Vector3 extents = objCollider.bounds.extents;
            float colliderExtentAlongNormal = Mathf.Abs(Vector3.Dot(extents, surfaceHit.normal));
            totalPadding += colliderExtentAlongNormal;
        }

        Vector3 surfaceTargetPos = surfaceHit.point + (surfaceHit.normal * totalPadding);
        Quaternion surfaceTargetRot = initialRotation;

        AnimateDrop(surfaceTargetPos, surfaceTargetRot, () =>
        {
            FinishDrop();
        });
    }

    private void AnimateDrop(Vector3 targetPos, Quaternion targetRot, System.Action onComplete)
    {
        IsHeld = false;
        IsDropping = true;

        Sequence dropSeq = DOTween.Sequence();
        dropSeq.Join(transform.DOMove(targetPos, dropAnimationDuration).SetEase(dropEase));
        dropSeq.Join(transform.DORotateQuaternion(targetRot, dropAnimationDuration).SetEase(dropEase));
        dropSeq.OnComplete(() =>
        {
            IsDropping = false;
            onComplete?.Invoke();
        });

        currentDropTween = dropSeq;
    }

    public void SnapToPoint(SnapPoint point)
    {
        CurrentSnapPoint = point;
        transform.position = point.TargetPosition;
        transform.rotation = point.TargetRotation;
        point.Place(this);
    }

    private void FinishDrop()
    {
        IsHeld = false;
        IsDropping = false;
        lastInteractionTime = Time.time;

        if (objCollider != null) objCollider.enabled = true;
        if (rb != null) rb.isKinematic = true;
        InteractionManager.Instance.ShouldInteract = true;

        SetPersistentStateValue("pos_x", transform.position.x);
        SetPersistentStateValue("pos_y", transform.position.y);
        SetPersistentStateValue("pos_z", transform.position.z);

        SetPersistentStateValue("rot_x", transform.rotation.x);
        SetPersistentStateValue("rot_y", transform.rotation.y);
        SetPersistentStateValue("rot_z", transform.rotation.z);
        SetPersistentStateValue("rot_w", transform.rotation.w);

        OnDropped();
    }

    protected virtual void OnDropped() { }

    protected override void OnStateRestored()
    {
        base.OnStateRestored();

        if (ContainsPersistentState("pos_x"))
        {
            float px = GetPersistentStateValue<float>("pos_x");
            float py = GetPersistentStateValue<float>("pos_y");
            float pz = GetPersistentStateValue<float>("pos_z");
            transform.position = new Vector3(px, py, pz);
        }

        if (ContainsPersistentState("rot_x"))
        {
            float rx = GetPersistentStateValue<float>("rot_x");
            float ry = GetPersistentStateValue<float>("rot_y");
            float rz = GetPersistentStateValue<float>("rot_z");
            float rw = GetPersistentStateValue<float>("rot_w");
            transform.rotation = new Quaternion(rx, ry, rz, rw);
        }
    }
}