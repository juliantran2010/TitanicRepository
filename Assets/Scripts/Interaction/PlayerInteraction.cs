using System.Collections;
using System.Drawing;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerInteraction : MonoBehaviour
{

    private Camera mainCamera;
    private DialogueManager dialogueManager;

    [Header("Einstellungen")]
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private LayerMask interactionLayerMask;

    [Header("UI Elemente")]
    [SerializeField] private Vector2 defaultSize = new Vector2(10f, 10f); // Kleiner Punkt
    [SerializeField] private Vector2 interactIconSize = new Vector2(48f, 48f); // Größeres Icon für Hand, Sprechblase etc.
    [SerializeField] private Image crosshairImage;
    [SerializeField] private RectTransform crosshairRectTransform;
    [SerializeField] private Sprite defaultIcon;
    [SerializeField] private Sprite talkIcon;
    [SerializeField] private Sprite pickupIcon;
    [SerializeField] private Sprite inspectIcon;
    [SerializeField] private Sprite useIcon;



    private void Awake()
    {
        mainCamera = Camera.main;
        crosshairRectTransform = crosshairImage.GetComponent<RectTransform>();
    }

    private void Start()
    {
        dialogueManager = DialogueManager.Instance;
    }

    private void Update()
    {
        if (dialogueManager.isInDialogue) return;
        CheckForInteractable();
    }

    private void CheckForInteractable()
    {
        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance))
        {
            if (hit.collider.TryGetComponent<IInteractable>(out IInteractable interactable))
            {
                SetCrosshairIcon(interactable.Type, interactIconSize);

                if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                {
                    StartCoroutine(InteractInNextFrame(interactable));
                }
            }
        }
        else
        {
            SetCrosshairIcon(InteractionType.None, defaultSize);
        }
    }

    /// <summary>
    /// Start Dialogue in next frame to avoid skipping the first sentence
    /// </summary>
    private IEnumerator InteractInNextFrame(IInteractable interactable)
    {
        yield return null; // Wait for the next frame
        interactable.Interact();
    }

    private void SetCrosshairIcon(InteractionType type, Vector2 size)
    {
        switch (type)
        {
            case InteractionType.Dialogue:
                crosshairImage.sprite = talkIcon;
                break;
            case InteractionType.Pickup:
                crosshairImage.sprite = pickupIcon;
                break;
            case InteractionType.Inspect:
                crosshairImage.sprite = inspectIcon;
                break;
            case InteractionType.Use:
                crosshairImage.sprite = useIcon;
                break;
            default:
                crosshairImage.sprite = defaultIcon;
                break;
        }
        crosshairRectTransform.sizeDelta = size;

    }

}
