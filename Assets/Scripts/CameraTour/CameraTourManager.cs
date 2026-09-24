using System;
using System.Collections.Generic;
using UnityEngine;

public class CameraTourManager : MonoBehaviour
{
    public static CameraTourManager Instance { get; private set; }

    [System.Serializable]
    public class TourWaypoint
    {
        public Transform pointTransform;
        public Transform lookAtTarget;
        [Tooltip("Ungefähre Flugzeit zum Punkt in Sekunden")]
        public float flightTime = 2.5f;
        [Tooltip("Bei wie viel Prozent der gefahrenen Teilstrecke soll 'onComplete' auslösen? (0.85 = bei 85% des Weges)")]
        [Range(0.1f, 0.98f)]
        public float arrivalThreshold = 0.85f;
    }

    [System.Serializable]
    public class CameraTour
    {
        public string tourName = "titanic";
        public List<TourWaypoint> waypoints = new List<TourWaypoint>();
    }

    [Header("Feste Tour-Kamera")]
    [SerializeField] private Camera tourCamera;

    [Header("Drift & Übergänge")]

    [Tooltip("Mindest-Restdistanz in Metern, ab der alternativ ausgelöst wird (besonders wichtig bei kurzen Schwenks)")]
    [SerializeField] private float arrivalDistanceFallback = 2.0f;

    private List<CameraTour> activeTours = new List<CameraTour>();
    public bool IsTourActive { get; private set; } = false;

    private Vector3 currentVelocity = Vector3.zero;
    private Vector3 targetPosition;
    private float currentFlightTime = 2f;
    private float currentArrivalThreshold = 0.85f;
    private bool isMoving = false;
    private Action currentOnCompleteCallback;

    private Transform currentLookAtTarget;
    private Quaternion targetRotation;
    private float totalSegmentDistance = 0f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (tourCamera != null)
            {
                tourCamera.gameObject.SetActive(false);
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void LateUpdate()
    {
        if (!IsTourActive) return;

        Transform camTrans = tourCamera != null ? tourCamera.transform : transform;

        // 1. Butterweiche Positionsbewegung über SmoothDamp
        if (isMoving)
        {
            camTrans.position = Vector3.SmoothDamp(
                camTrans.position,
                targetPosition,
                ref currentVelocity,
                currentFlightTime
            );

            float remainingDistance = Vector3.Distance(camTrans.position, targetPosition);
            float progress = totalSegmentDistance > 0.01f ? 1f - (remainingDistance / totalSegmentDistance) : 1f;

            // Löst frühzeitig aus: Sobald die Prozentmarke ODER die Fallback-Distanz erreicht ist
            if (currentOnCompleteCallback != null && (progress >= currentArrivalThreshold || remainingDistance <= arrivalDistanceFallback))
            {
                var callback = currentOnCompleteCallback;
                currentOnCompleteCallback = null; // Verhindert doppelte Ausführung
                callback?.Invoke();
            }

            // Vollständig zum Stillstand gekommen
            if (currentVelocity.sqrMagnitude < 0.0001f && remainingDistance < 0.02f)
            {
                isMoving = false;
                currentVelocity = Vector3.zero;
            }
        }

        // 2. Weiche Rotation zum Ziel oder zur Wegpunkt-Ausrichtung
        if (currentLookAtTarget != null)
        {
            Vector3 dir = currentLookAtTarget.position - camTrans.position;
            if (dir.sqrMagnitude > 0.001f)
            {
                Quaternion lookRot = Quaternion.LookRotation(dir, Vector3.up);
                camTrans.rotation = Quaternion.Slerp(camTrans.rotation, lookRot, Time.deltaTime * 3.5f);
            }
        }
        else
        {
            camTrans.rotation = Quaternion.Slerp(camTrans.rotation, targetRotation, Time.deltaTime * 3.5f);
        }
    }

    public bool TourIsRegistered(string tourName)
    {
        return activeTours.Find(tour => tour.tourName == tourName) != null;
    }

    public void RegisterSceneTours(List<CameraTour> tours)
    {
        activeTours = tours;
    }

    public void UnregisterSceneTours()
    {
        if (IsTourActive) StopTour();
        activeTours.Clear();
    }

    private void StartTour(string tourName, int startingIndex = 0, Action onComplete = null)
    {
        if (IsTourActive) return;
        IsTourActive = true;

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SetState(GameState.CameraTour);
        }

        if (tourCamera != null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                tourCamera.transform.position = mainCam.transform.position;
                tourCamera.transform.rotation = mainCam.transform.rotation;
            }
            tourCamera.gameObject.SetActive(true);
        }

        MoveToWaypoint(tourName, startingIndex, onComplete);
    }

    public void MoveToWaypoint(string tourName, int index, Action onComplete = null)
    {
        if (!IsTourActive)
        {
            StartTour(tourName, index, onComplete);
            return;
        }

        var tour = activeTours.Find(t => t.tourName.Equals(tourName, StringComparison.OrdinalIgnoreCase));
        if (tour == null || index < 0 || index >= tour.waypoints.Count)
        {
            Debug.LogWarning($"[CameraTourManager] Tour '{tourName}' oder Index {index} existiert nicht!");
            onComplete?.Invoke();
            return;
        }

        TourWaypoint wp = tour.waypoints[index];
        if (wp.pointTransform == null)
        {
            Debug.LogError($"[CameraTourManager] Transform für Punkt {index} in Tour '{tourName}' fehlt!");
            onComplete?.Invoke();
            return;
        }

        Transform camTrans = tourCamera != null ? tourCamera.transform : transform;

        targetPosition = wp.pointTransform.position;
        targetRotation = wp.pointTransform.rotation;
        currentFlightTime = Mathf.Max(0.1f, wp.flightTime);
        currentArrivalThreshold = wp.arrivalThreshold;
        currentLookAtTarget = wp.lookAtTarget;

        // Gesamtstrecke ermitteln, um Fortschritt präzise zu messen
        totalSegmentDistance = Vector3.Distance(camTrans.position, targetPosition);

        currentOnCompleteCallback = onComplete;
        isMoving = true;
    }

    public void StopTour()
    {
        if (!IsTourActive) return;
        IsTourActive = false;
        isMoving = false;
        currentVelocity = Vector3.zero;
        currentLookAtTarget = null;
        currentOnCompleteCallback = null;

        if (tourCamera != null)
        {
            tourCamera.gameObject.SetActive(false);
        }

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SetState(GameState.Gameplay);
        }
    }
}