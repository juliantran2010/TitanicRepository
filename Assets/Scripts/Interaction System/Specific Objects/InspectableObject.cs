using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class InspectableObject : InteractableObject
{
    public override InteractionType Type => InteractionType.Inspect;


    [Header("Inspect Settings")]
    [SerializeField] private Dialogue dialogueAfterLooking;
    [SerializeField] private float distanceInFront = 0.5f;
    [SerializeField] private float moveDuration = 0.4f;
    [SerializeField] private ParticleSystem interactionParticles;
    [SerializeField] private bool showParticles = true;

    protected InputAction escapeAction;
    protected InputAction clickAction;
    protected bool isReading = false;
    protected Vector3 originalLocalPosition;
    protected Quaternion originalLocalRotation;
    protected Transform originalParent;
    protected bool hasBeenInspected = false;
    private bool clickPending = false;

    protected virtual void Awake()
    {
        escapeAction = new InputAction(type: InputActionType.Button, binding: "<Keyboard>/escape");
        clickAction = new InputAction(type: InputActionType.Button, binding: "<Mouse>/leftButton");
        ToggleParticles(showParticles);
    }

    protected override void Start()
    {
        base.Start();
        originalLocalPosition = transform.localPosition;
        originalLocalRotation = transform.localRotation;
        originalParent = transform.parent;
    }

    protected virtual void Update()
    {
        if (!isReading || !clickPending) return;
        clickPending = false;

        // UI-Klicks ignorieren (jetzt mit aktuellem EventSystem-Stand)
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if (Camera.main == null) return;

        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // Klick auf die Notiz selbst schließt sie nicht
            if (hit.transform == transform || hit.transform.IsChildOf(transform))
            {
                return;
            }
        }

        CloseNote();
    }

    protected override void OnStateRestored()
    {
        if (HasInteracted && interactionParticles != null)
        {
            interactionParticles.Stop();
        }
        hasBeenInspected = GetPersistentStateValue("has_been_inspected", false);
    }

    protected override void OnInteract()
    {
        if (!isReading)
        {
            if (interactionParticles != null) interactionParticles.Stop();
            OpenNote();
        }
    }

    public void ToggleParticles(bool active)
    {
        if (interactionParticles == null) return;
        var emission = interactionParticles.emission;
        emission.enabled = active;
    }

    public virtual void OpenNote()
    {
        isReading = true;
        clickPending = false;
        GameStateManager.Instance.SetState(GameState.Inspect);

        Transform camTransform = Camera.main.transform;
        transform.SetParent(camTransform, true);

        transform.DOKill();
        Vector3 targetLocalPos = new Vector3(0f, 0f, distanceInFront);
        transform.DOLocalMove(targetLocalPos, moveDuration).SetEase(Ease.OutCubic);
        transform.DOLocalRotate(Vector3.zero, moveDuration).SetEase(Ease.OutCubic).OnComplete(OnOpenComplete);

        escapeAction.Enable();
        escapeAction.performed += OnEscapePressed;
        clickAction.Enable();
        clickAction.performed += OnClickPressed;
    }

    protected virtual void OnOpenComplete()
    {
        // Standard-Notiz macht hier nichts weiter
    }

    protected virtual void OnEscapePressed(InputAction.CallbackContext context)
    {
        CloseNote();
    }

    public virtual void CloseNote()
    {
        if (!isReading) return;
        isReading = false;
        clickPending = false;

        escapeAction.performed -= OnEscapePressed;
        escapeAction.Disable();
        clickAction.performed -= OnClickPressed;
        clickAction.Disable();

        OnCloseStarted();

        transform.DOKill();
        transform.SetParent(originalParent, true);
        transform.DOLocalMove(originalLocalPosition, moveDuration).SetEase(Ease.OutCubic);
        transform.DOLocalRotateQuaternion(originalLocalRotation, moveDuration).SetEase(Ease.OutCubic);

        DOVirtual.DelayedCall(moveDuration, () =>
        {
            GameStateManager.Instance.SetState(GameState.Gameplay);
            if (!hasBeenInspected && dialogueAfterLooking != null)
            {
                DialogueManager.Instance.StartDialogue(dialogueAfterLooking);
            }
            hasBeenInspected = true;
            SetPersistentStateValue("has_been_inspected", true);
        });
    }

    protected virtual void OnCloseStarted()
    {
        // Hook für Unterklassen
    }

    protected virtual void OnClickPressed(InputAction.CallbackContext context)
    {
        // Nur vormerken – die Auswertung erfolgt im nächsten Update-Schritt
        clickPending = true;
    }
}