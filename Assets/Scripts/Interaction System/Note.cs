using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class Note : InteractableObject
{
    public override InteractionType Type => InteractionType.Read;

    public override string ObjectName => "Note";

    [SerializeField] private TextMeshProUGUI noteDisplay;
    [TextArea(5, 10)]
    [SerializeField] private string noteText;

    [Header("Inspect Settings")]
    [SerializeField] private float distanceInFront = 0.5f; // Abstand vor der Linse
    [SerializeField] private float moveDuration = 0.4f;
    [SerializeField] private ParticleSystem interactionParticles;

    private InputAction escapeAction;
    private InputAction clickAction;
    private bool isReading = false;
    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;
    private Transform originalParent;


    private void Awake()
    {
        escapeAction = new InputAction(type: InputActionType.Button, binding: "<Keyboard>/escape");
        clickAction = new InputAction(type: InputActionType.Button, binding: "<Mouse>/leftButton");
    }

    protected override void Start()
    {
        base.Start();
        noteDisplay.text = noteText;
        originalLocalPosition = transform.localPosition;
        originalLocalRotation = transform.localRotation;
        originalParent = transform.parent;
    }

    protected override void OnStateRestored()
    {
        if (HasInteracted)
        {
            interactionParticles.Stop();
        }
    }

    protected override void OnInteract()
    {
        if (!isReading)
        {
            interactionParticles.Stop();
            OpenNote();
        }
    }

    private void OpenNote()
    {
        isReading = true;
        GameStateManager.Instance.SetState(GameState.Inspect);

        Transform camTransform = Camera.main.transform;
        transform.SetParent(camTransform, true);

        // zur kamera bewegen
        transform.DOKill();
        Vector3 targetLocalPos = new Vector3(0f, 0f, distanceInFront);
        transform.DOLocalMove(targetLocalPos, moveDuration).SetEase(Ease.OutCubic);
        transform.DOLocalRotate(Vector3.zero, moveDuration).SetEase(Ease.OutCubic);

        escapeAction.Enable();
        escapeAction.performed += OnEscapePressed;
        clickAction.Enable();
        clickAction.performed += OnClickPressed;
    }

    private void OnEscapePressed(InputAction.CallbackContext context)
    {
        CloseNote();
    }

    private void CloseNote()
    {
        if (!isReading) return;
        isReading = false;

        escapeAction.performed -= OnEscapePressed;
        escapeAction.Disable();
        clickAction.performed -= OnClickPressed;
        clickAction.Disable();

        transform.DOKill();
        transform.SetParent(originalParent, true);
        transform.DOLocalMove(originalLocalPosition, moveDuration).SetEase(Ease.OutCubic);
        transform.DOLocalRotateQuaternion(originalLocalRotation, moveDuration).SetEase(Ease.OutCubic);
        DOVirtual.DelayedCall(0f, () =>
        {
            //Erst im nächsten Frame den State zurücksetzen, damit die Note nicht direkt wieder geöffnet wird
            GameStateManager.Instance.SetState(GameState.Gameplay);
        });
    }

    private void OnClickPressed(InputAction.CallbackContext context)
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // Wenn wir direkt auf dieses Dokument geklickt haben, NICHT schließen
            if (hit.transform == transform)
            {
                return;
            }
        }

        // Ansonsten (ins Leere geklickt oder anderes Objekt): Schließen!
        CloseNote();
    }
}
