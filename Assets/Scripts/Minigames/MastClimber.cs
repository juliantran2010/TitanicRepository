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

        TriggerNextQTE();
    }

    private void Update()
    {
        // Inputs blockieren, solange geklettert wird ODER man oben fest verankert ist
        if ((IsClimbingActive || isLockedAtTop) && starterInputs != null)
        {
            starterInputs.move = Vector2.zero;
            starterInputs.jump = false;
            starterInputs.sprint = false;
        }
    }

    private void LateUpdate()
    {
        // Fixiert die Position, solange man klettert ODER oben arretiert ist
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

        if (keepLockedAtTop)
        {
            // Oben arretiert lassen (nur Umschauen bleibt aktiv)
            isLockedAtTop = true;
        }
        else
        {
            isLockedAtTop = false;

            // Optional: Auf festen Boden im Korb versetzen
            if (topDismountPoint != null)
            {
                if (charController != null) charController.enabled = false;
                playerTransform.position = topDismountPoint.position;
                if (charController != null) charController.enabled = true;
            }
        }
    }

    /// <summary>
    /// Falls du den Spieler später über ein Event oder Script wieder freigeben möchtest (z.B. Leiter wieder runterklettern)
    /// </summary>
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

        // Wenn man bereits auf der untersten Sprosse (Stufe 0) ist:
        if (CurrentStep <= 0)
        {
            // Kein Rückschritt möglich, aber die QTE-Schleife muss weiterlaufen!
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

        TimingRingQTE.Instance.StartQTE(nextKey);
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