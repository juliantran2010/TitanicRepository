using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InteractionManager : MonoBehaviour
{
    public static InteractionManager Instance { get; private set; }
    private Camera mainCamera;
    private bool canInteract = true;


    [Header("Einstellungen")]
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private LayerMask interactionLayerMask;

    [Header("UI Elemente")]
    [SerializeField] private Vector2 defaultSize = new Vector2(10f, 10f); // Kleiner Punkt
    [SerializeField] private Vector2 interactIconSize = new Vector2(48f, 48f); // Größeres Icon für Hand, Sprechblase etc.
    [SerializeField] private Image crosshairImage;
    [SerializeField] private RectTransform crosshairRectTransform;
    [SerializeField] private TextMeshProUGUI crosshairDescription;

    [Header("Crosshair Icons")]
    [SerializeField] private Sprite defaultIcon;
    [SerializeField] private Sprite talkIcon;
    [SerializeField] private Sprite pickupIcon;
    [SerializeField] private Sprite inspectIcon;
    [SerializeField] private Sprite useIcon;
    [SerializeField] private Sprite readIcon;
    [SerializeField] private Sprite teleportIcon;



    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        mainCamera = Camera.main;
        crosshairRectTransform = crosshairImage.GetComponent<RectTransform>();
    }

    private void Start()
    {
        if (GameStateManager.Instance is not null)
        {
            GameStateManager.Instance.OnStateChanged += HandleStateChanged;
        }
    }
    private void OnDestroy()
    {
        if (GameStateManager.Instance is not null)
        {
            GameStateManager.Instance.OnStateChanged -= HandleStateChanged;
        }
    }

    private void HandleStateChanged(GameState state)
    {
        canInteract = state == GameState.Gameplay;
        crosshairImage.gameObject.SetActive(canInteract);
    }

    private void Update()
    {
        if (!canInteract) return;
        CheckForInteractable();
    }

    private void CheckForInteractable()
    {
        SetCrosshairIcon(null);
        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance))
        {
            if (hit.collider.TryGetComponent<InteractableObject>(out InteractableObject interactable))
            {
                SetCrosshairIcon(interactable);

                if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                {
                    interactable.Interact();
                }
            }
        }
    }

    private void SetCrosshairIcon(InteractableObject interactable)
    {
        if (interactable == null || interactable.Type == InteractionType.None)
        {
            crosshairImage.sprite = defaultIcon;
            crosshairDescription.text = "";
            crosshairRectTransform.sizeDelta = defaultSize;
            return;
        }
        crosshairRectTransform.sizeDelta = interactIconSize;
        switch (interactable.Type)
        {
            case InteractionType.Dialogue:
                crosshairImage.sprite = talkIcon;
                crosshairDescription.text = "Talk to " + interactable.ObjectName;
                break;
            case InteractionType.Pickup:
                crosshairImage.sprite = pickupIcon;
                crosshairDescription.text = "Pick up " + interactable.ObjectName;
                break;
            case InteractionType.Inspect:
                crosshairImage.sprite = inspectIcon;
                crosshairDescription.text = "Inspect " + interactable.ObjectName;
                break;
            case InteractionType.Use:
                crosshairImage.sprite = useIcon;
                crosshairDescription.text = "Use " + interactable.ObjectName;
                break;
            case InteractionType.Teleport:
                crosshairImage.sprite = teleportIcon;
                crosshairDescription.text = "Go to " + interactable.ObjectName;
                break;
            case InteractionType.Read:
                crosshairImage.sprite = readIcon;
                crosshairDescription.text = "Read " + interactable.ObjectName;
                break;
            default:
                crosshairImage.sprite = defaultIcon;
                crosshairDescription.text = "";
                break;
        }
    }

}
