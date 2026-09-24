using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(LineRenderer))]
public class PathGuideManager : MonoBehaviour
{
    public static PathGuideManager Instance { get; private set; }

    [Header("Settings")]
    [Tooltip("Höhenabstand über dem Boden gegen Z-Fighting")]
    [SerializeField] private float heightOffset = 0.08f;

    [Tooltip("Wie viele Meter vor dem Spieler die Linie anfangen soll")]
    [SerializeField] private float startForwardDistance = 1.2f;

    [Tooltip("Soll die Linie automatisch ausgeblendet werden, wenn der Spieler nah genug am Ziel ist?")]
    [SerializeField] private bool autoHideOnArrival = true;

    [Tooltip("Distanz zum Ziel in Metern, ab der ausgeblendet wird (nur aktiv wenn autoHideOnArrival true ist)")]
    [SerializeField] private float arrivalDistance = 1.8f;

    [Tooltip("Wie oft das NavMesh neu berechnet wird (in Sekunden)")]
    [SerializeField] private float updateInterval = 0.2f;

    private readonly Dictionary<string, Transform> sceneTargets = new Dictionary<string, Transform>();

    private string currentTargetId;
    private Transform activeTarget;
    private Transform playerTransform;
    private LineRenderer lineRenderer;
    private NavMeshPath navMeshPath;
    private Coroutine activeGuideRoutine;

    private Vector3[] pathCorners = System.Array.Empty<Vector3>();
    private bool hasValidPath = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.enabled = false;
        navMeshPath = new NavMeshPath();
    }

    private void LateUpdate()
    {
        if (!hasValidPath || lineRenderer == null || !lineRenderer.enabled || pathCorners.Length < 2)
            return;

        EnsurePlayerRef();
        if (playerTransform == null) return;

        Vector3 playerPos = playerTransform.position + (Vector3.up * heightOffset);
        Vector3 nextTargetCorner = pathCorners[1] + (Vector3.up * heightOffset);

        // Richtung zur nächsten Kurve
        Vector3 dir = nextTargetCorner - playerPos;
        float dist = dir.magnitude;

        // Startpunkt sauber vor den Spieler setzen, maximal bis kurz vor die Kurve
        float forwardAmount = Mathf.Min(startForwardDistance, Mathf.Max(0f, dist - 0.2f));
        Vector3 dynamicStartPos = playerPos + (dir.normalized * forwardAmount);

        lineRenderer.SetPosition(0, dynamicStartPos);
    }

    // ========================================================================
    // HAUPT-FUNKTIONEN
    // ========================================================================

    public void ShowPathToTarget(string targetId)
    {
        currentTargetId = targetId;

        if (sceneTargets.TryGetValue(targetId, out Transform target) && target != null)
        {
            activeTarget = target;
            StartGuideLoop();
        }
        else
        {
            Debug.LogWarning($"[PathGuide] Ziel '{targetId}' nicht gefunden oder Transform ist null!");
        }
    }

    public void HidePath()
    {
        currentTargetId = null;
        activeTarget = null;
        hasValidPath = false;

        if (activeGuideRoutine != null)
        {
            StopCoroutine(activeGuideRoutine);
            activeGuideRoutine = null;
        }

        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 0;
            lineRenderer.enabled = false;
        }
    }

    private void StartGuideLoop()
    {
        if (!gameObject.activeInHierarchy)
        {
            gameObject.SetActive(true);
        }

        EnsurePlayerRef();
        if (playerTransform == null) return;

        if (activeGuideRoutine != null)
        {
            StopCoroutine(activeGuideRoutine);
            activeGuideRoutine = null;
        }

        activeGuideRoutine = StartCoroutine(GuideFollowRoutine());
    }

    private IEnumerator GuideFollowRoutine()
    {
        var wait = new WaitForSecondsRealtime(updateInterval);

        while (activeTarget != null)
        {
            if (playerTransform == null)
            {
                EnsurePlayerRef();
                if (playerTransform == null)
                {
                    yield return wait;
                    continue;
                }
            }

            // Distanz-Prüfung nur ausführen, wenn die Option aktiv ist
            if (autoHideOnArrival)
            {
                float distToTarget = Vector3.Distance(playerTransform.position, activeTarget.position);
                if (distToTarget <= arrivalDistance)
                {
                    HidePath();
                    yield break;
                }
            }

            Vector3 startPos = playerTransform.position;
            Vector3 endPos = activeTarget.position;

            if (NavMesh.SamplePosition(startPos, out NavMeshHit hitStart, 2.0f, NavMesh.AllAreas))
                startPos = hitStart.position;

            if (NavMesh.SamplePosition(endPos, out NavMeshHit hitTarget, 2.0f, NavMesh.AllAreas))
                endPos = hitTarget.position;

            if (NavMesh.CalculatePath(startPos, endPos, NavMesh.AllAreas, navMeshPath) &&
                navMeshPath.corners.Length >= 2)
            {
                pathCorners = navMeshPath.corners;
                hasValidPath = true;

                lineRenderer.positionCount = pathCorners.Length;

                for (int i = 1; i < pathCorners.Length; i++)
                {
                    lineRenderer.SetPosition(i, pathCorners[i] + (Vector3.up * heightOffset));
                }

                lineRenderer.enabled = true;
            }
            else
            {
                hasValidPath = false;
                lineRenderer.enabled = false;
            }

            yield return wait;
        }

        HidePath();
    }

    // ========================================================================
    // REGISTRY & HELPER
    // ========================================================================

    public void RegisterTarget(string id, Transform targetTransform)
    {
        if (string.IsNullOrEmpty(id) || targetTransform == null) return;
        sceneTargets[id] = targetTransform;

        if (currentTargetId == id)
        {
            activeTarget = targetTransform;
            StartGuideLoop();
        }
    }

    public void UnregisterTarget(string id)
    {
        if (sceneTargets.ContainsKey(id))
        {
            if (activeTarget == sceneTargets[id])
            {
                HidePath();
            }
            sceneTargets.Remove(id);
        }
    }

    private void EnsurePlayerRef()
    {
        if (playerTransform != null) return;

        if (PersistentPlayer.Instance != null)
        {
            playerTransform = PersistentPlayer.Instance.transform;
        }
        else
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }
    }
}