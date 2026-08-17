using UnityEngine;

public class NPC : MonoBehaviour, IInteractable
{
    [SerializeField] private string npcName;
    public string ObjectName => npcName;

    [SerializeField] private Dialogue dialogue;
    public InteractionType Type => InteractionType.Dialogue;
    public void Interact()
    {
        Debug.Log($"Interacting with {npcName}");
        DialogueManager.Instance.StartDialogue(dialogue);
    }
}
