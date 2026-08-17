using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{

    private Camera mainCamera;
    private DialogueManager dialogueManager;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void Start()
    {
        dialogueManager = DialogueManager.Instance;
    }

    private void Update()
    {
        if (dialogueManager.isInDialogue) return;
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            PerformRaycast();
        }
    }

    private void PerformRaycast()
    {
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Ray ray = mainCamera.ScreenPointToRay(mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.TryGetComponent(out DialogueTrigger trigger))
            {
                Debug.Log("Interacted with: " + hit.collider.name);
                StartCoroutine(StartDialogueInNextFrame(trigger)); //start dialogue in next frame to avoid skipping the first sentence
                return;
            }
        }
    }

    private IEnumerator StartDialogueInNextFrame(DialogueTrigger trigger)
    {
        yield return null; // Wait for the next frame
        trigger.TriggerDialogue();
    }
}
