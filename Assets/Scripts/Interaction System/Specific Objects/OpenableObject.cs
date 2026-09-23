using UnityEngine;
using DG.Tweening;
using UnityEngine.Events;

public class OpenableObject : InteractableObject
{
    public override InteractionType Type => InteractionType.Open;

    [Header("Bewegung (Relativ)")]
    [Tooltip("z. B. (0, 0, 0.3) für Schublade aufziehen. Auf 0 lassen wenn keine Verschiebung")]
    [SerializeField] private Vector3 moveBy = Vector3.zero;
    [Tooltip("z. B. (0, 90, 0) für Tür aufdrehen. Auf 0 lassen wenn keine Drehung")]
    [SerializeField] private Vector3 rotateBy = Vector3.zero;
    [SerializeField] private float duration = 0.5f;
    [SerializeField] private Ease ease = Ease.InOutQuad;

    private Vector3 startPos;
    private Vector3 startRot;
    public bool IsOpened { get; private set; } = false;
    public UnityEvent OnOpened;
    public UnityEvent OnClosed;

    protected override void Start()
    {
        base.Start();
        startPos = transform.localPosition;
        startRot = transform.localEulerAngles;
    }

    protected override void OnInteract()
    {
        IsOpened = !IsOpened;
        SetPersistentStateValue("is_opened", IsOpened);
        if (IsOpened)
            OnOpened?.Invoke();
        else
            OnClosed?.Invoke();

        // Zielwerte: Entweder Offset draufrechnen oder zurück auf Start
        Vector3 targetPos = IsOpened ? startPos + moveBy : startPos;
        Vector3 targetRot = IsOpened ? startRot + rotateBy : startRot;

        transform.DOKill();

        if (moveBy != Vector3.zero)
        {
            transform.DOLocalMove(targetPos, duration).SetEase(ease);
        }

        if (rotateBy != Vector3.zero)
        {
            transform.DOLocalRotate(targetRot, duration).SetEase(ease);
        }
    }

    protected override void OnStateRestored()
    {
        if (ContainsPersistentState("is_opened"))
        {
            IsOpened = GetPersistentStateValue("is_opened", false);
            if (IsOpened)
            {
                Vector3 targetPos = IsOpened ? startPos + moveBy : startPos;
                Vector3 targetRot = IsOpened ? startRot + rotateBy : startRot;
                transform.localPosition = targetPos;
                transform.localRotation = Quaternion.Euler(targetRot);
            }
        }
    }
}