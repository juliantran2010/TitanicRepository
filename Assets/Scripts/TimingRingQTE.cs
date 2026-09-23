using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class TimingRingQTE : MonoBehaviour
{
    public static TimingRingQTE Instance { get; private set; }

    [Header("QTE Timing Config")]
    [Tooltip("Zeitspanne in Sekunden, bis der Ring von außen im Zentrum ankommt")]
    [SerializeField] private float duration = 1.2f;

    [Tooltip("Start-Skalierung des Rings (z. B. 2.5 für 250% Größe)")]
    [SerializeField] private float maxRingScale = 2.5f;

    [Tooltip("Trefferfenster Start (wenn Ring noch etwas größer ist als der Zielkreis)")]
    [Range(0f, 1f)][SerializeField] private float targetWindowStart = 0.40f;

    [Tooltip("Trefferfenster Ende (wenn Ring fast im Zentrum ist)")]
    [Range(0f, 1f)][SerializeField] private float targetWindowEnd = 0.15f;

    [Header("Visuelles Feedback")]
    [SerializeField] private Color initialTextColor;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hitZoneColor = new Color(1f, 0.92f, 0.016f); // Gelb: Jetzt drücken!
    [SerializeField] private Color successColor = new Color(0.2f, 1f, 0.3f);    // Grün
    [SerializeField] private Color failColor = new Color(1f, 0.25f, 0.25f);     // Rot
    [SerializeField] private float feedbackDuration = 0.2f;

    [Header("Direkte UI Referenzen")]
    [SerializeField] private GameObject rootContainer;
    [SerializeField] private RectTransform shrinkingRing;
    [SerializeField] private RectTransform targetZoneCircle;
    [SerializeField] private TextMeshProUGUI keyPromptText;

    [Header("State")]
    public bool IsRunning { get; private set; }
    public float CurrentProgress { get; private set; } // 1.0 -> 0.0
    public Key CurrentKey { get; private set; }

    // Actions für den Climber oder andere Systeme
    public Action OnSuccess;
    public Action OnFailed;

    private float timer;
    private Image shrinkingRingImage;
    private Coroutine feedbackCoroutine;
    private bool isInHitWindow = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (shrinkingRing != null)
        {
            shrinkingRingImage = shrinkingRing.GetComponent<Image>();
        }

        if (rootContainer != null)
        {
            rootContainer.SetActive(false);
        }

        initialTextColor = keyPromptText.color;
    }

    private void Update()
    {
        if (!IsRunning) return;

        timer -= Time.deltaTime;
        CurrentProgress = Mathf.Clamp01(timer / duration);

        // Ring schrumpfen lassen (Original-Formel unverändert)
        if (shrinkingRing != null)
        {
            float currentScale = Mathf.Lerp(0f, maxRingScale, CurrentProgress);
            shrinkingRing.localScale = new Vector3(currentScale, currentScale, 1f);
        }

        // Visueller Hinweis: Befindet man sich gerade im Zielfenster?
        bool insideNow = CurrentProgress <= targetWindowStart && CurrentProgress >= targetWindowEnd;
        if (insideNow != isInHitWindow)
        {
            isInHitWindow = insideNow;
            if (shrinkingRingImage != null)
            {
                shrinkingRingImage.color = isInHitWindow ? hitZoneColor : normalColor;
            }
        }

        // Tasteneingabe über das neue Input System abfragen
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.anyKey.wasPressedThisFrame)
        {
            KeyControl targetControl = keyboard[CurrentKey];

            if (targetControl != null && targetControl.wasPressedThisFrame)
            {
                if (insideNow)
                {
                    EndQTE(true);
                }
                else
                {
                    EndQTE(false); // Zu früh oder zu spät
                }
            }
            else
            {
                EndQTE(false); // Falsche Taste gedrückt
            }
            return;
        }

        // Zeit abgelaufen
        if (timer <= 0f)
        {
            EndQTE(false);
        }
    }

    public void StartQTE(Key keyToPress, float customDuration = -1f)
    {
        if (feedbackCoroutine != null) StopCoroutine(feedbackCoroutine);

        CurrentKey = keyToPress;
        duration = customDuration > 0 ? customDuration : duration;
        timer = duration;
        CurrentProgress = 1f;
        isInHitWindow = false;
        IsRunning = true;

        if (shrinkingRingImage != null)
        {
            shrinkingRingImage.color = normalColor;
        }

        if (keyPromptText != null)
        {
            keyPromptText.text = keyToPress.ToString();
            keyPromptText.color = initialTextColor;
        }

        // Original-Skalierung des Zielkreises beibehalten
        if (targetZoneCircle != null)
        {
            float targetScale = Mathf.Lerp(0f, maxRingScale, (targetWindowStart + targetWindowEnd) * 0.5f);
            targetZoneCircle.localScale = new Vector3(targetScale, targetScale, 1f);
        }

        if (rootContainer != null)
        {
            rootContainer.transform.localPosition = Vector3.zero;
            rootContainer.SetActive(true);
        }
    }

    public void CancelQTE()
    {
        if (feedbackCoroutine != null) StopCoroutine(feedbackCoroutine);
        IsRunning = false;
        if (rootContainer != null) rootContainer.SetActive(false);
    }

    private void EndQTE(bool success)
    {
        IsRunning = false;
        feedbackCoroutine = StartCoroutine(ShowFeedbackAndFinish(success));
    }

    private IEnumerator ShowFeedbackAndFinish(bool success)
    {
        Color outcomeColor = success ? successColor : failColor;

        if (shrinkingRingImage != null) shrinkingRingImage.color = outcomeColor;
        if (keyPromptText != null) keyPromptText.color = outcomeColor;

        Vector3 startScale = shrinkingRing != null ? shrinkingRing.localScale : Vector3.one;
        Vector3 basePos = rootContainer != null ? rootContainer.transform.localPosition : Vector3.zero;

        float elapsed = 0f;
        while (elapsed < feedbackDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / feedbackDuration;

            if (success)
            {
                // Kurzer Erfolgs-Pulse (leicht aufspringen)
                if (shrinkingRing != null)
                {
                    float bump = 1f + Mathf.Sin(t * Mathf.PI) * 0.18f;
                    shrinkingRing.localScale = startScale * bump;
                }
            }
            else
            {
                // Schütteln bei Fehlversuch
                if (rootContainer != null)
                {
                    float shake = Mathf.Sin(elapsed * 55f) * 10f * (1f - t);
                    rootContainer.transform.localPosition = basePos + new Vector3(shake, 0f, 0f);
                }
            }

            yield return null;
        }

        if (rootContainer != null)
        {
            rootContainer.transform.localPosition = basePos;
            rootContainer.SetActive(false);
        }

        if (success)
        {
            OnSuccess?.Invoke();
        }
        else
        {
            OnFailed?.Invoke();
        }
    }
}