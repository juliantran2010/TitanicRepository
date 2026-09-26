using UnityEngine;
using DG.Tweening;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class CameraFocusMover : MonoBehaviour
{
    [Header("Camera & Controls")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private MonoBehaviour movementScript; // Walk/Run
    [SerializeField] private MonoBehaviour mouseLookScript; // Look/Mouse-Aim

    [Header("Animation Settings")]
    [SerializeField] private float travelDuration = 0.8f;
    [SerializeField] private Ease travelEase = Ease.InOutCubic;

    // Gespeicherte Originaldaten im Spieler-Kopf
    private Transform originalParent;
    private Vector3 originalLocalPos;
    private Quaternion originalLocalRot;

    private bool isZoomed = false;
    public bool IsZoomed => isZoomed;

    private Sequence moveSequence;

    public static CameraFocusMover Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureReferences();
    }

    private void Update()
    {
        if (!isZoomed) return;

        bool cancelPressed = false;
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) cancelPressed = true;
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame) cancelPressed = true;
#else
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1)) cancelPressed = true;
#endif

        if (cancelPressed)
        {
            ZoomOut();
        }
    }

    public void ZoomIn(Transform targetTransform)
    {
        if (isZoomed || targetTransform == null) return;

        EnsureReferences();
        if (playerCamera == null) return;

        isZoomed = true;

        // 1. Steuerung abschalten
        SetPlayerControls(false);

        // 2. Zustand im Kopf merken
        originalParent = playerCamera.transform.parent;
        originalLocalPos = playerCamera.transform.localPosition;
        originalLocalRot = playerCamera.transform.localRotation;

        // 3. WICHTIG: Kamera vom Spieler entkoppeln!
        // So kann kein Parent-Skript oder Controller mehr dazwischenfunken
        playerCamera.transform.SetParent(null);

        moveSequence?.Kill();
        moveSequence = DOTween.Sequence();

        // 4. Im echten World-Space genau an Position und Rotation des Targets fahren
        moveSequence.Append(playerCamera.transform.DOMove(targetTransform.position, travelDuration).SetEase(travelEase));
        moveSequence.Join(playerCamera.transform.DORotate(targetTransform.eulerAngles, travelDuration).SetEase(travelEase));
    }

    public void ZoomOut()
    {
        if (!isZoomed) return;

        EnsureReferences();
        if (playerCamera == null)
        {
            isZoomed = false;
            return;
        }

        moveSequence?.Kill();
        moveSequence = DOTween.Sequence();

        // Zielposition im World-Space berechnen (wo der Kopf des Spielers gerade ist)
        Vector3 targetWorldPos = originalParent != null
            ? originalParent.TransformPoint(originalLocalPos)
            : playerCamera.transform.position;

        Quaternion targetWorldRot = originalParent != null
            ? originalParent.rotation * originalLocalRot
            : playerCamera.transform.rotation;

        moveSequence.Append(playerCamera.transform.DOMove(targetWorldPos, travelDuration * 0.8f).SetEase(Ease.OutSine));
        moveSequence.Join(playerCamera.transform.DORotate(targetWorldRot.eulerAngles, travelDuration * 0.8f).SetEase(Ease.OutSine));

        moveSequence.OnComplete(() =>
        {
            // Wieder zurück an den Kopf hängen
            if (originalParent != null)
            {
                playerCamera.transform.SetParent(originalParent);
                playerCamera.transform.localPosition = originalLocalPos;
                playerCamera.transform.localRotation = originalLocalRot;
            }

            SetPlayerControls(true);
            isZoomed = false;
        });
    }

    private void SetPlayerControls(bool enabledState)
    {
        if (movementScript != null) movementScript.enabled = enabledState;
        if (mouseLookScript != null) mouseLookScript.enabled = enabledState;

        Cursor.lockState = enabledState ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !enabledState;
    }

    private void EnsureReferences()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
    }

    private void OnDestroy()
    {
        moveSequence?.Kill();
    }
}