using DG.Tweening;
using Ink.Runtime;
using UnityEngine;

public class NPC : DialogueObject
{
    [Header("Looking at player")]
    [SerializeField] private int turnOffset = 25;
    private Quaternion initialRotation;
    [SerializeField] private float turnDuration = 0.5f;

    private Animator animator;
    [SerializeField] private float currentLookWeight = 0f;

    protected override void Start()
    {
        base.Start();
        animator = GetComponent<Animator>();
    }

    protected override void OnInteract()
    {
        base.OnInteract();
        DOTween.To(() => currentLookWeight, x => currentLookWeight = x, 1f, turnDuration);
    }

    private void OnAnimatorIK(int layerIndex)
    {
        Animator animator = GetComponent<Animator>();
        animator.SetLookAtWeight(currentLookWeight, 0.3f, 0.8f, 1f);
        animator.SetLookAtPosition(Camera.main.transform.position);
    }

    protected override void OnDialogueEnd(Dialogue _dialogue, Story _story)
    {
        base.OnDialogueEnd(_dialogue, _story);
        DOTween.To(() => currentLookWeight, x => currentLookWeight = x, 0f, turnDuration);
    }
}
