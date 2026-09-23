using System.Collections;
using StarterAssets;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class MastClimber : InteractableObject
{
    public override InteractionType Type => InteractionType.Move;

    [Header("Waypoints entlang der Neigung")]
    [SerializeField] private Transform startPoint;
    [SerializeField] private Transform underBasketPoint;
    [SerializeField] private Transform topLookoutPoint;

    [Header("Ende & Ausguck")]
    [Tooltip("Soll der Spieler am oberen Punkt fixiert bleiben (nur Umschauen möglich, kein Herunterfallen)?")]
    [SerializeField] private bool keepLockedAtTop = true;

    [Tooltip("Optional: Punkt mit festem Boden im Korb. Wenn zugewiesen, wird der Spieler dorthin versetzt.")]
    [SerializeField] private Transform topDismountPoint;

    [Header("Stufen-Einstellungen")]
    [SerializeField] private int totalSteps = 10;
    [Range(0.5f, 0.95f)]
    [SerializeField] private float basketTransitionThreshold = 0.8f;
    [SerializeField] private float stepDuration = 0.35f;

    [Header("QTE Timing Loop")]
    [SerializeField] private float delayBetweenChecks = 1.2f;
    [SerializeField] private Key[] stepKeys = { Key.Q, Key.E };
    [SerializeField] private Key finalStepKey = Key.Space;

    [Header("QTE World-Position Offset")]
    [Tooltip("Abstand von der Leitersprosse nach vorne in Blickrichtung der Leiter (z. B. 0.45m vor dem Mast)")]
    [SerializeField] private float rungForwardOffset = 0.45f;
    [Tooltip("Zusätzliche Höhe der Sprosse (z. B. 0.2m leicht nach oben versetzt)")]
    [SerializeField] private float rungHeightOffset = 0.2f;

    [Header("Events")]
    public UnityEvent<int, int> onStepChanged;
    public UnityEvent onClimbCompleted;

    public int CurrentStep { get; private set; } = 0;
    public bool IsClimbingActive { get; private set; } = false;
    public bool IsMoving { get; private set; } = false;

    private Transform playerTransform;
    private int keyIndex = 0;

    private FirstPersonController fpsController;
    private CharacterController charController;
    private StarterAssetsInputs starterInputs;

    private Vector3 lockedClimbPosition;
    private bool isLockedAtTop = false;

    private void Awake()
    {
        if (stepKeys == null || stepKeys.Length == 0 || stepKeys[0] == Key.None || stepKeys[0] > Key.Z)
        {
            stepKeys = new Key[] { Key.Q, Key.E };
        }
    }

    private void OnEnable()
    {
        if (TimingRingQTE.Instance != null)
        {
            TimingRingQTE.Instance.OnSuccess += HandleQTESuccess;
            TimingRingQTE.Instance.OnFailed += HandleQTEFailed;
        }
    }

    private void OnDisable()
    {
        if (TimingRingQTE.Instance != null)
        {
            TimingRingQTE.Instance.OnSuccess -= HandleQTESuccess;
            TimingRingQTE.Instance.OnFailed -= HandleQTEFailed;
        }
    }

    public void StartClimbing(Transform player)
    {
        if (TimingRingQTE.Instance != null)
        {
            TimingRingQTE.Instance.OnSuccess -= HandleQTESuccess;
            TimingRingQTE.Instance.OnFailed -= HandleQTEFailed;
            TimingRingQTE.Instance.OnSuccess += HandleQTESuccess;
            TimingRingQTE.Instance.OnFailed += HandleQTEFailed;
        }

        playerTransform = player;
        CurrentStep = 0;
        IsClimbingActive = true;
        isLockedAtTop = false;

        fpsController = playerTransform.GetComponent<FirstPersonController>();
        charController = playerTransform.GetComponent<CharacterController>();
        starterInputs = playerTransform.GetComponent<StarterAssetsInputs>();

        lockedClimbPosition = EvaluateStepTransform(0).pos;
        playerTransform.position = lockedClimbPosition;
        TimingRingQTE.Instance.BeginQTESession();

        TriggerNextQTE();
    }

    private void Update()
    {
        if ((IsClimbingActive || isLockedAtTop) && starterInputs != null)
        {
            starterInputs.move = Vector2.zero;
            starterInputs.jump = false;
            starterInputs.sprint = false;
        }
    }

    private void LateUpdate()
    {
        if ((IsClimbingActive || isLockedAtTop) && !IsMoving && playerTransform != null)
        {
            playerTransform.position = lockedClimbPosition;
        }
    }

    public void StopClimbing()
    {
        IsClimbingActive = false;
        TimingRingQTE.Instance?.CancelQTE();
        StopAllCoroutines();
        TimingRingQTE.Instance?.EndQTESession();

        if (keepLockedAtTop)
        {
            isLockedAtTop = true;
        }
        else
        {
            isLockedAtTop = false;

            if (topDismountPoint != null)
            {
                if (charController != null) charController.enabled = false;
                playerTransform.position = topDismountPoint.position;
                if (charController != null) charController.enabled = true;
            }
        }
    }

    public void UnlockPlayerFromTop()
    {
        isLockedAtTop = false;
    }

    public void TakeStepForward()
    {
        if (IsMoving || CurrentStep >= totalSteps) return;

        CurrentStep++;
        onStepChanged?.Invoke(CurrentStep, totalSteps);

        StartCoroutine(AnimateToStep(CurrentStep, () =>
        {
            if (CurrentStep >= totalSteps)
            {
                StopClimbing();
                onClimbCompleted?.Invoke();
            }
            else if (IsClimbingActive)
            {
                TriggerNextQTE();
            }
        }));
    }

    public void TakeStepBackward()
    {
        if (IsMoving) return;

        if (CurrentStep <= 0)
        {
            if (IsClimbingActive)
            {
                TriggerNextQTE();
            }
            return;
        }

        CurrentStep--;
        onStepChanged?.Invoke(CurrentStep, totalSteps);

        StartCoroutine(AnimateToStep(CurrentStep, () =>
        {
            if (IsClimbingActive)
            {
                TriggerNextQTE();
            }
        }));
    }

    private void HandleQTESuccess()
    {
        if (!IsClimbingActive) return;
        TakeStepForward();
    }

    private void HandleQTEFailed()
    {
        if (!IsClimbingActive) return;
        TakeStepBackward();
    }

    private void TriggerNextQTE()
    {
        if (!IsClimbingActive || TimingRingQTE.Instance == null) return;
        StartCoroutine(WaitAndLaunchQTE());
    }

    private IEnumerator WaitAndLaunchQTE()
    {
        yield return new WaitForSeconds(delayBetweenChecks);
        if (!IsClimbingActive) yield break;

        Key nextKey;
        if (CurrentStep == totalSteps - 1)
        {
            nextKey = finalStepKey;
        }
        else
        {
            nextKey = stepKeys[keyIndex % stepKeys.Length];
            keyIndex++;
        }

        // 1. Hole genau den aktuellen Schritt (wo der Spieler gerade steht)
        var stepData = EvaluateStepTransform(CurrentStep);
        Vector3 stepFeetPos = stepData.pos;
        Quaternion stepRot = stepData.rot;

        // 2. Q links, E rechts
        float lateralOffset = (nextKey == Key.Q) ? -0.18f : (nextKey == Key.E ? 0.18f : 0f);

        // 3. Fester Weltpunkt:
        // - Start bei den Füßen der aktuellen Stufe (stepFeetPos)
        // - rungHeightOffset nach oben (zur Brust-/Griffhöhe)
        // - rungForwardOffset nach vorne in den Mast
        // - lateralOffset zur Seite
        Vector3 rungWorldPos = stepFeetPos
                             + (Vector3.up * rungHeightOffset)
                             + (stepRot * Vector3.forward * rungForwardOffset)
                             + (stepRot * Vector3.right * lateralOffset);

        TimingRingQTE.Instance.StartQTE(nextKey, rungWorldPos);
    }

    private (Vector3 pos, Quaternion rot) EvaluateStepTransform(int step)
    {
        float t = Mathf.Clamp01((float)step / totalSteps);

        if (t <= basketTransitionThreshold)
        {
            float segmentT = t / basketTransitionThreshold;
            Vector3 pos = Vector3.Lerp(startPoint.position, underBasketPoint.position, segmentT);
            Quaternion rot = Quaternion.Slerp(startPoint.rotation, underBasketPoint.rotation, segmentT);
            return (pos, rot);
        }
        else
        {
            float segmentT = (t - basketTransitionThreshold) / (1f - basketTransitionThreshold);
            Vector3 pos = Vector3.Lerp(underBasketPoint.position, topLookoutPoint.position, segmentT);
            Quaternion rot = Quaternion.Slerp(underBasketPoint.rotation, topLookoutPoint.rotation, segmentT);
            return (pos, rot);
        }
    }

    private IEnumerator AnimateToStep(int targetStep, System.Action onComplete)
    {
        IsMoving = true;
        Vector3 startP = lockedClimbPosition;
        Vector3 targetP = EvaluateStepTransform(targetStep).pos;

        float elapsed = 0f;
        while (elapsed < stepDuration)
        {
            elapsed += Time.deltaTime;
            float factor = Mathf.SmoothStep(0f, 1f, elapsed / stepDuration);

            lockedClimbPosition = Vector3.Lerp(startP, targetP, factor);
            playerTransform.position = lockedClimbPosition;

            yield return null;
        }

        lockedClimbPosition = targetP;
        playerTransform.position = lockedClimbPosition;
        IsMoving = false;

        onComplete?.Invoke();
    }

    protected override void OnInteract()
    {
        StartClimbing(PersistentPlayer.Instance.gameObject.transform);
        IsInteractable = false;
    }
}