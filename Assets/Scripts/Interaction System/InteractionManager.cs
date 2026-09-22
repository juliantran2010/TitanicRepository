using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InteractionManager : MonoBehaviour
{
    public static InteractionManager Instance { get; private set; }
    private Camera mainCamera;
    private bool canInteract = true;
    private bool _shouldInteract = true;
    public bool ShouldInteract
    {
        get => _shouldInteract;
        set
        {
            _shouldInteract = value;
            currentInteractable = null;
            SetInteractionTarget(null);
        }
    }

    [Header("Einstellungen")]
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private LayerMask interactionLayerMask;
    [SerializeField] private Vector2 screenPixelOffset = new Vector2(0f, 30f);

    [Header("Smoothing")]
    [Tooltip("Dämpfung der 3D-Zielposition. Höhere Werte (20-30) folgen dem Objekt agil, dämpfen aber Mikrozittern ab.")]
    [SerializeField] private float smoothSpeed = 25f;

    [Header("Zentrales Fadenkreuz")]
    [SerializeField] private Image centerCrosshair;

    [Header("Floating Prompt Element")]
    [SerializeField] private RectTransform promptContainer;
    [SerializeField] private Image promptIcon;
    [SerializeField] private TextMeshProUGUI promptText;

    [Header("Aktions-Icons")]
    [SerializeField] private Sprite talkIcon;
    [SerializeField] private Sprite pickupIcon;
    [SerializeField] private Sprite inspectIcon;
    [SerializeField] private Sprite useIcon;
    [SerializeField] private Sprite readIcon;
    [SerializeField] private Sprite teleportIcon;

    public Action<InteractableObject> OnInteracted;
    private InteractableObject currentInteractable;

    private Vector3 smoothedWorldPos;
    private bool isFirstFrameActive = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (promptContainer != null)
        {
            promptContainer.gameObject.SetActive(false);
        }
    }

    private void Start()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnStateChanged += HandleStateChanged;
        }
        mainCamera = Camera.main;
    }

    private void OnDestroy()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnStateChanged -= HandleStateChanged;
        }
    }

    private void HandleStateChanged(GameState oldState, GameState newState)
    {
        canInteract = newState == GameState.Gameplay;
        if (!canInteract)
        {
            SetInteractionTarget(null);
        }

        if (centerCrosshair != null)
        {
            centerCrosshair.gameObject.SetActive(canInteract);
        }
    }

    private void Update()
    {
        if (!canInteract || !ShouldInteract) return;
        CheckForInteractable();
    }

    private void LateUpdate()
    {
        UpdatePromptPosition();
    }

    private void CheckForInteractable()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;

        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance, interactionLayerMask))
        {
            if (hit.collider.TryGetComponent(out InteractableObject interactable))
            {
                if ((interactable.CanInteract() && interactable.ShowLabel) || interactable.ShowLabelIfCannotInteract)
                {
                    currentInteractable = interactable;
                    SetInteractionTarget(interactable);
                }

                if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                {
                    interactable.Interact();
                    OnInteracted?.Invoke(interactable);
                }
                return;
            }
        }

        currentInteractable = null;
        SetInteractionTarget(null);
    }

    private void UpdatePromptPosition()
    {
        if (currentInteractable == null || promptContainer == null || !promptContainer.gameObject.activeSelf)
        {
            isFirstFrameActive = true;
            return;
        }

        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;

        // 1. Reale Zielposition im 3D-Raum bestimmen
        Vector3 rawTargetPos;
        if (currentInteractable.TryGetComponent<Collider>(out Collider col))
        {
            float clampedY = Mathf.Clamp(mainCamera.transform.position.y, col.bounds.min.y, col.bounds.max.y);
            rawTargetPos = new Vector3(col.bounds.center.x, clampedY, col.bounds.center.z);
        }
        else
        {
            rawTargetPos = currentInteractable.transform.position;
        }

        // 2. Nur die 3D-Weltposition glätten (verhindert Jitter durch NPC-Animationen)
        if (isFirstFrameActive)
        {
            smoothedWorldPos = rawTargetPos;
            isFirstFrameActive = false;
        }
        else
        {
            smoothedWorldPos = Vector3.Lerp(smoothedWorldPos, rawTargetPos, Time.deltaTime * smoothSpeed);
        }

        // 3. Sofortige Projektion auf den Screen (Kameradrehungen greifen ohne jeden Verzug!)
        Vector3 screenPos = mainCamera.WorldToScreenPoint(smoothedWorldPos);

        if (screenPos.z > 0)
        {
            screenPos.z = 0f;

            RectTransform parentRect = promptContainer.parent as RectTransform;
            Canvas rootCanvas = promptContainer.GetComponentInParent<Canvas>();

            if (parentRect != null && rootCanvas != null)
            {
                Camera uiCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;

                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPos, uiCamera, out Vector2 localPoint))
                {
                    promptContainer.localPosition = localPoint + screenPixelOffset;
                }
            }
        }
    }

    private void SetInteractionTarget(InteractableObject interactable)
    {
        if (interactable == null || interactable.Type == InteractionType.None)
        {
            if (promptContainer != null) promptContainer.gameObject.SetActive(false);
            isFirstFrameActive = true;
            return;
        }

        if (promptContainer != null && !promptContainer.gameObject.activeSelf)
        {
            isFirstFrameActive = true;
            promptContainer.gameObject.SetActive(true);
        }

        string objectName = string.IsNullOrEmpty(interactable.DisplayName) ? interactable.ObjectName : interactable.DisplayName;
        if (string.IsNullOrEmpty(objectName))
        {
            objectName = interactable.gameObject.name;
        }

        Sprite iconSprite = null;
        string actionPrefix = "";
        InteractionType type = interactable.UniqueInteractionType == InteractionType.Undefined ? interactable.Type : interactable.UniqueInteractionType;
        switch (type)
        {
            case InteractionType.Dialogue:
                iconSprite = talkIcon;
                actionPrefix = "Talk to ";
                break;
            case InteractionType.Move:
            case InteractionType.Pickup:
                iconSprite = pickupIcon;
                actionPrefix = "Pick up ";
                break;
            case InteractionType.Inspect:
                iconSprite = inspectIcon;
                actionPrefix = "Inspect ";
                break;
            case InteractionType.Use:
                iconSprite = useIcon;
                actionPrefix = "Use ";
                break;
            case InteractionType.Teleport:
                iconSprite = teleportIcon;
                actionPrefix = "Go to ";
                break;
            case InteractionType.Read:
                iconSprite = readIcon;
                actionPrefix = "Read ";
                break;
        }

        if (promptIcon != null)
        {
            promptIcon.sprite = iconSprite;
            promptIcon.gameObject.SetActive(iconSprite != null);
        }

        if (promptText != null)
        {
            promptText.text = !string.IsNullOrEmpty(interactable.UniqueInteractionLabel)
                ? interactable.UniqueInteractionLabel
                : actionPrefix + objectName;
        }
    }
}