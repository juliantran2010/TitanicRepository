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

        // 3. Aufruf der statischen Funktion
        PositionUIToWorld(smoothedWorldPos, promptContainer, screenPixelOffset, mainCamera);
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
            case InteractionType.Open:
                OpenableObject obj = interactable as OpenableObject;
                iconSprite = useIcon;
                actionPrefix = (obj != null && obj.IsOpened) ? "Close " : "Open ";
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

    /// <summary>
    /// Projiziert eine 3D-Weltposition auf ein UI-Element im Canvas.
    /// Funktioniert sowohl mit Screen Space - Overlay als auch mit Screen Space - Camera.
    /// </summary>
    /// <param name="worldPosition">Die Position im 3D-Raum</param>
    /// <param name="targetUI">Das RectTransform, das positioniert werden soll</param>
    /// <param name="pixelOffset">Optionaler Versatz in Pixeln</param>
    /// <param name="camOverride">Optionale Kamera</param>
    /// <param name="clampToScreen">Wenn true, bleibt das Element am Bildschirmrand kleben, statt zu verschwinden</param>
    /// <param name="screenPadding">Randabstand in Pixeln beim Klemmen (z. B. 40f für Ring-Größe)</param>
    /// <returns>True, wenn das UI gezeichnet werden konnte; False bei Fehlern.</returns>
    public static bool PositionUIToWorld(
        Vector3 worldPosition,
        RectTransform targetUI,
        Vector2 pixelOffset = default,
        Camera camOverride = null,
        bool clampToScreen = false,
        float screenPadding = 40f)
    {
        if (targetUI == null)
        {
            Debug.LogError("[PositionUIToWorld] targetUI ist NULL!");
            return false;
        }

        Camera cam = camOverride != null ? camOverride : (Instance != null && Instance.mainCamera != null ? Instance.mainCamera : Camera.main);
        if (cam == null)
        {
            Debug.LogError("[PositionUIToWorld] Keine Kamera gefunden! (Camera.main ist null)");
            return false;
        }

        // 1. Projektion
        Vector3 screenPos = cam.WorldToScreenPoint(worldPosition);
        bool isBehindCamera = screenPos.z <= 0.05f;

        // Liegt der Punkt hinter der Kamera?
        if (isBehindCamera)
        {
            if (!clampToScreen)
            {
                return false;
            }

            // Wenn hinter der Kamera: Richtung spiegeln, damit er an die passende Kante wandert
            screenPos.x = Screen.width - screenPos.x;
            screenPos.y = Screen.height - screenPos.y;
        }

        // 2. Am Bildschirmrand festklemmen (Clamping)
        if (clampToScreen)
        {
            // Zentrieren zur Bildschirmmitte für saubere Projektion bei invertierten Vektoren
            Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            Vector2 fromCenter = (Vector2)screenPos - screenCenter;

            if (isBehindCamera)
            {
                // Bei Punkten hinter der Kamera schieben wir den Vektor bewusst über den Bildrand hinaus
                fromCenter = -fromCenter.normalized * Mathf.Max(Screen.width, Screen.height);
                screenPos = screenCenter + fromCenter;
            }

            // Auf sichtbare Bildschirmgrenzen begrenzen
            screenPos.x = Mathf.Clamp(screenPos.x, screenPadding, Screen.width - screenPadding);
            screenPos.y = Mathf.Clamp(screenPos.y, screenPadding, Screen.height - screenPadding);
        }
        else
        {
            // Ohne Clamping: Prüfen ob außerhalb des regulären Sichtfelds
            if (screenPos.x < 0 || screenPos.x > Screen.width || screenPos.y < 0 || screenPos.y > Screen.height)
            {
                return false;
            }
        }

        // Z für ScreenPointToLocalPoint auf 0 setzen
        screenPos.z = 0f;

        // 3. Parent & Canvas Validierung
        RectTransform parentRect = targetUI.parent as RectTransform;
        if (parentRect == null)
        {
            Debug.LogError($"[PositionUIToWorld] FEHLSCHLAG: Parent von {targetUI.name} ist KEIN RectTransform!");
            return false;
        }

        Canvas rootCanvas = targetUI.GetComponentInParent<Canvas>();
        if (rootCanvas == null)
        {
            Debug.LogError($"[PositionUIToWorld] FEHLSCHLAG: Kein Canvas über {targetUI.name} gefunden!");
            return false;
        }

        // 4. UI-Kamera & Umrechnung in lokale Koordinaten
        Camera uiCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPos, uiCamera, out Vector2 localPoint))
        {
            targetUI.localPosition = localPoint + pixelOffset;
            return true;
        }

        return false;
    }
}