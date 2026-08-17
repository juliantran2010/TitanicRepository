using System.Collections;
using System.Drawing;
using TMPro;
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
    [SerializeField] private TextMeshProUGUI crosshairDescription;

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
                SetCrosshairIcon(interactable);

                if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                {
                    StartCoroutine(InteractInNextFrame(interactable));
                }
            }
        }
        else
        {
            SetCrosshairIcon(null);
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

    private void SetCrosshairIcon(IInteractable interactable)
    {
        if (interactable == null)
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
            default:
                crosshairImage.sprite = defaultIcon;
                crosshairDescription.text = "";
                break;
        }
    }

}
