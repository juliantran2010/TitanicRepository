using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class CameraTourManager : MonoBehaviour
{
    public static CameraTourManager Instance { get; private set; }

    [System.Serializable]
    public class TourWaypoint
    {
        [Tooltip("Transform für Position und Blickrichtung")]
        public Transform pointTransform;
        [Tooltip("Optional: Ziel, das fixiert angeschaut werden soll")]
        public Transform lookAtTarget;
        [Tooltip("Dauer der Fahrt zu diesem Punkt")]
        public float moveDuration = 3f;
        public Ease ease = Ease.InOutSine;
    }

    [System.Serializable]
    public class CameraTour
    {
        public string tourName = "titanic";
        public List<TourWaypoint> waypoints = new List<TourWaypoint>();
    }

    [Header("Feste Tour-Kamera (wird immer genutzt)")]
    [SerializeField] private Camera tourCamera;

    private List<CameraTour> activeTours = new List<CameraTour>();
    private Camera playerCamera;
    public bool IsTourActive { get; private set; } = false;

    private Tween currentMoveTween;
    private Tween currentRotTween;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Kamera standardmäßig deaktiviert halten
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

    /// <summary>
    /// Wird vom lokalen Szenen-Objekt (z.B. CameraTourSceneBinding) beim Laden aufgerufen
    /// </summary>
    public void RegisterSceneTours(List<CameraTour> tours)
    {
        activeTours = tours;
    }

    public void UnregisterSceneTours()
    {
        if (IsTourActive) StopTour();
        activeTours.Clear();
    }

    /// <summary>
    /// Startet die Tour, schaltet den GameState um und wechselt die Kamera.
    /// </summary>
    private void StartTour(string tourName, int startingIndex = 0, Action onComplete = null)
    {
        if (IsTourActive) return;
        IsTourActive = true;

        // 1. GameState umschalten
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SetState(GameState.CameraTour);
        }

        // 3. Feste Tour-Kamera aktivieren
        if (tourCamera != null)
        {
            tourCamera.gameObject.SetActive(true);
        }

        // 4. Ersten Punkt anfahren
        MoveToWaypoint(tourName, startingIndex, onComplete);
    }

    /// <summary>
    /// Fährt einen Punkt an (z. B. #cam:titanic:1). Startet automatisch, falls Tour noch inaktiv.
    /// </summary>
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
        float duration = index == 0 ? 0f : wp.moveDuration;

        currentMoveTween?.Kill();
        currentRotTween?.Kill();

        // 1. Translation
        currentMoveTween = camTrans.DOMove(wp.pointTransform.position, duration)
            .SetEase(wp.ease)
            .OnComplete(() =>
            {
                onComplete?.Invoke();
            });

        // 2. Rotation
        if (wp.lookAtTarget != null)
        {
            currentRotTween = camTrans.DOLookAt(wp.lookAtTarget.position, duration)
                .SetEase(wp.ease);
        }
        else
        {
            currentRotTween = camTrans.DORotateQuaternion(wp.pointTransform.rotation, duration)
                .SetEase(wp.ease);
        }
    }

    /// <summary>
    /// Beendet die Tour, schaltet zur Spieler-Kamera zurück und setzt den GameState zurück.
    /// </summary>
    public void StopTour()
    {
        if (!IsTourActive) return;
        IsTourActive = false;

        currentMoveTween?.Kill();
        currentRotTween?.Kill();

        // 1. Tour-Kamera aus
        if (tourCamera != null)
        {
            tourCamera.gameObject.SetActive(false);
        }

        // 3. GameState zurücksetzen
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SetState(GameState.Gameplay);
        }
    }
}