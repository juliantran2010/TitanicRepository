using DG.Tweening;
using Ink.Runtime;
using UnityEngine;

public class NPC : DialogueObject
{
    [Header("Looking at player")]
    [SerializeField] private int turnOffset = 25;
    private Quaternion initialRotation;
    [SerializeField] private float turnDuration = 0.5f;
    protected override void OnInteract()
    {
        base.OnInteract();
        initialRotation = transform.rotation;
        Vector3 direction = PersistentPlayer.Instance.transform.position - transform.position;
        Quaternion targetRotation = Quaternion.LookRotation(direction);

        // Um 25 Grad auf der Y-Achse drehen (npc schauen etwas schief)
        targetRotation *= Quaternion.Euler(0f, turnOffset, 0f);

        transform.DORotateQuaternion(targetRotation, turnDuration);
    }

    protected override void OnDialogueEnd(Dialogue _dialogue, Story _story)
    {
        base.OnDialogueEnd(_dialogue, _story);
        transform.DORotateQuaternion(initialRotation, turnDuration);
    }
}
