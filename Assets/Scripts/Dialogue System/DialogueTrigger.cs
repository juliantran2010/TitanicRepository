using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    public Dialogue dialogue;

    public void TriggerDialogue()
    {
        DialogueManager manager = DialogueManager.Instance;
        manager.StartDialogue(dialogue);
    }
}
