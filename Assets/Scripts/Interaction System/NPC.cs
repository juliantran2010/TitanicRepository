using DG.Tweening;
using Ink.Runtime;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Animator), typeof(NavMeshAgent))]
public class NPC : DialogueObject
{
    [Header("Looking at player")]
    [SerializeField] private float turnDuration = 0.5f;
    private Animator animator;
    [SerializeField] private float currentLookWeight = 0f;

    [Header("Wander")]
    [SerializeField] private Vector3 startPosition;
    [SerializeField] private bool shouldWander = false;   // Sollte der NPC zufällig rumlaufen?
    [SerializeField] private float wanderRadius = 2f;   // Wie weit sich der NPC vom Startpunkt/Standort entfernt
    [SerializeField] private float minWaitTime = 2f;    // Mindestwartezeit an einem Punkt (Sekunden)
    [SerializeField] private float maxWaitTime = 6f;    // Maximale Wartezeit an einem Punkt (Sekunden)
    private NavMeshAgent agent;
    private Tween waitTween;
    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    private void Awake()
    {
        startPosition = transform.position;
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
    }

    protected override void Start()
    {
        base.Start();
        if (shouldWander)
            SetNewDestination();
    }

    private void Reset()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.radius = 0.2f;
            agent.stoppingDistance = 0f;
            agent.speed = 0.2f;
            agent.acceleration = 6f;
            agent.height = 1f;
            agent.autoBraking= false;
            int walkableAreaIndex = NavMesh.GetAreaFromName("Walkable");
            agent.areaMask = 1 << walkableAreaIndex;
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        waitTween.Kill();
        SetStateValue("current_position", transform.position);
    }

    protected override void OnStateRestored()
    {
        base.OnStateRestored();
        transform.position = GetStateValue<Vector3>("current_position");
    }

    protected override void OnInteract()
    {
        base.OnInteract();
        if (dialogue == null || dialogue.inkJSON == null) return;
        if (shouldWander) StopWandering();
        DOTween.To(() => currentLookWeight, x => currentLookWeight = x, 1f, turnDuration);
    }

    private void OnAnimatorIK(int layerIndex)
    {
        animator.SetLookAtWeight(currentLookWeight, 0.3f, 0.8f, 1f);
        animator.SetLookAtPosition(Camera.main.transform.position);
    }

    protected override void OnDialogueEnd(Dialogue _dialogue, Story _story)
    {
        base.OnDialogueEnd(_dialogue, _story);
        DOTween.To(() => currentLookWeight, x => currentLookWeight = x, 0f, turnDuration)
            .OnComplete(() =>
            {
                if (shouldWander) ResumeWandering();
            });
    }

    private void SetNewDestination(bool resumePath = false)
    {
        // Neuer Zielpunkt auf dem NavMesh
        Vector3 target = GetRandomPoint();
        if (!resumePath) agent.SetDestination(target);
        //Animation auf Gehen stellen
        animator.SetFloat(SpeedHash, 1f);

        // DOTween wartet, bis die verbleibende Distanz klein genug ist
        waitTween = DOVirtual.Float(0, 1, 1f, _ => { })
            .OnUpdate(() =>
            {
                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                {
                    waitTween.Kill();
                    animator.SetFloat(SpeedHash, 0f);

                    // Pause einlegen, danach automatisch nächste Zielsuche
                    float pause = Random.Range(minWaitTime, maxWaitTime);
                    waitTween = DOVirtual.DelayedCall(pause, () => SetNewDestination());
                }
            }).SetLoops(-1);
    }

    private Vector3 GetRandomPoint()
    {
        for (int i = 0; i < 10; i++) // Bis zu 10 Versuche für einen gültigen Punkt
        {
            // 1. Nur X- und Z-Achse zufällig wählen (kein Y-Versatz in andere Etagen!)
            Vector2 randomCircle = Random.insideUnitCircle * wanderRadius;
            Vector3 randomDirection = new Vector3(randomCircle.x, 0f, randomCircle.y) + startPosition;

            // 2. Punkt auf dem NavMesh suchen
            if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                // 3. Echte Geh-Distanz auf dem Pfad berechnen
                NavMeshPath path = new NavMeshPath();
                if (agent.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete)
                {
                    float pathLength = GetPathLength(path);

                    // Wenn der tatsächliche Laufweg nicht zu lang ist, nimm den Punkt!
                    if (pathLength <= wanderRadius * 1.5f)
                    {
                        return hit.position;
                    }
                }
            }
        }

        return startPosition; // Fallback
    }

    // Hilfsmethode: Addiert alle Abschnitte des NavMesh-Pfads zusammen
    private float GetPathLength(NavMeshPath path)
    {
        float length = 0f;
        for (int i = 0; i < path.corners.Length - 1; i++)
        {
            length += Vector3.Distance(path.corners[i], path.corners[i + 1]);
        }
        return length;
    }

    // Für den Dialog
    public void StopWandering()
    {
        waitTween?.Kill(); // Stoppt sofort alle aktiven Timer/Checks
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        animator.SetFloat(SpeedHash, 0f);
    }

    public void ResumeWandering()
    {
        agent.isStopped = false;
        animator.SetFloat(SpeedHash, 1f);
        SetNewDestination(true);
    }
}
