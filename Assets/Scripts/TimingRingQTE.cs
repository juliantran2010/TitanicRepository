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
    [SerializeField] private Color hitZoneColor = new Color(1f, 0.92f, 0.016f); // Gelb
    [SerializeField] private Color successColor = new Color(0.2f, 1f, 0.3f);    // Grün
    [SerializeField] private Color failColor = new Color(1f, 0.25f, 0.25f);     // Rot
    [SerializeField] private float feedbackDuration = 0.2f;

    [Header("Container & UI")]
    [Tooltip("Das ganz übergeordnete Overlay (bleibt während der gesamten Kletter-Session aktiv)")]
    [SerializeField] private GameObject mainOverlayObject;

    [Tooltip("Unterobjekt, das NUR die Ringe und Tasten-Prompt enthält (geht pro Stufe an/aus)")]
    [SerializeField] private RectTransform ringContainer;

    [Header("Screen Offset")]
    [Tooltip("Optionaler Pixelversatz auf dem Bildschirm (z. B. 0, 20)")]
    [SerializeField] private Vector2 screenPixelOffset = Vector2.zero;

    [Header("Ring Referenzen")]
    [SerializeField] private RectTransform shrinkingRing;
    [SerializeField] private RectTransform targetZoneCircle;
    [SerializeField] private TextMeshProUGUI keyPromptText;

    [Header("State")]
    public bool IsPromptRunning { get; private set; }
    public float CurrentProgress { get; private set; }
    public Key CurrentKey { get; private set; }

    public Action OnSuccess;
    public Action OnFailed;

    private float timer;
    private Image shrinkingRingImage;
    private Coroutine feedbackCoroutine;
    private bool isInHitWindow = false;

    // Welt-Positionierung
    private Vector3? targetWorldPos = null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (ringContainer != null)
        {
            ringContainer.gameObject.SetActive(false);
        }

        if (shrinkingRing != null)
        {
            shrinkingRingImage = shrinkingRing.GetComponent<Image>();
        }

        if (keyPromptText != null)
        {
            initialTextColor = keyPromptText.color;
        }

        if (mainOverlayObject != null)
        {
            mainOverlayObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (!IsPromptRunning) return;

        timer -= Time.deltaTime;
        CurrentProgress = Mathf.Clamp01(timer / duration);

        if (shrinkingRing != null)
        {
            float currentScale = Mathf.Lerp(0f, maxRingScale, CurrentProgress);
            shrinkingRing.localScale = new Vector3(currentScale, currentScale, 1f);
        }

        bool insideNow = CurrentProgress <= targetWindowStart && CurrentProgress >= targetWindowEnd;
        if (insideNow != isInHitWindow)
        {
            isInHitWindow = insideNow;
            if (shrinkingRingImage != null)
            {
                shrinkingRingImage.color = isInHitWindow ? hitZoneColor : normalColor;
            }
        }

        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.anyKey.wasPressedThisFrame)
        {
            KeyControl targetControl = keyboard[CurrentKey];

            if (targetControl != null && targetControl.wasPressedThisFrame)
            {
                EndPrompt(insideNow);
            }
            else
            {
                EndPrompt(false);
            }
            return;
        }

        if (timer <= 0f)
        {
            EndPrompt(false);
        }
    }

    private void LateUpdate()
    {
        if (!targetWorldPos.HasValue || ringContainer == null) return;

        // clampToScreen = true, padding = 60f (damit der Ring nicht am Rand abgeschnitten wird)
        bool isVisible = InteractionManager.PositionUIToWorld(
            targetWorldPos.Value,
            ringContainer,
            screenPixelOffset,
            null,
            clampToScreen: true,
            screenPadding: 60f
        );

        bool shouldBeActive = isVisible && (IsPromptRunning || feedbackCoroutine != null);
        if (ringContainer.gameObject.activeSelf != shouldBeActive)
        {
            ringContainer.gameObject.SetActive(shouldBeActive);
        }
    }

    // --- SESSION CONTROL ---

    public void BeginQTESession()
    {
        if (mainOverlayObject != null)
        {
            mainOverlayObject.SetActive(true);
        }
        if (ringContainer != null)
        {
            ringContainer.gameObject.SetActive(false);
        }
    }

    public void EndQTESession()
    {
        if (feedbackCoroutine != null) StopCoroutine(feedbackCoroutine);
        feedbackCoroutine = null;
        IsPromptRunning = false;
        targetWorldPos = null;

        if (ringContainer != null) ringContainer.gameObject.SetActive(false);
        if (mainOverlayObject != null) mainOverlayObject.SetActive(false);
    }

    // --- PROMPT CONTROL ---

    /// <summary>
    /// Startet das QTE an einer konkreten 3D-Weltposition (z. B. Leitersprosse).
    /// </summary>
    public void StartQTE(Key keyToPress, Vector3 worldPosition, float customDuration = -1f)
    {
        Debug.Log($"[TimingRingQTE] StartQTE aufgerufen für Taste {keyToPress} an Pos {worldPosition}!");
        targetWorldPos = worldPosition;
        SetupAndRunPrompt(keyToPress, customDuration);

        // Position direkt im ersten Frame setzen
        if (ringContainer != null)
        {
            bool isVisible = InteractionManager.PositionUIToWorld(worldPosition, ringContainer, screenPixelOffset, clampToScreen: true);
            ringContainer.gameObject.SetActive(isVisible);
        }
    }

    /// <summary>
    /// Fallback: Startet das QTE ohne Weltpositionierung (bleibt an fester Bildschirm-Position).
    /// </summary>
    public void StartQTE(Key keyToPress, float customDuration = -1f)
    {
        targetWorldPos = null;
        SetupAndRunPrompt(keyToPress, customDuration);

        if (ringContainer != null)
        {
            ringContainer.gameObject.SetActive(true);
        }
    }

    private void SetupAndRunPrompt(Key keyToPress, float customDuration)
    {
        if (feedbackCoroutine != null) StopCoroutine(feedbackCoroutine);
        feedbackCoroutine = null;

        if (mainOverlayObject != null && !mainOverlayObject.activeSelf)
        {
            mainOverlayObject.SetActive(true);
        }

        CurrentKey = keyToPress;
        duration = customDuration > 0 ? customDuration : duration;
        timer = duration;
        CurrentProgress = 1f;
        isInHitWindow = false;
        IsPromptRunning = true;

        if (shrinkingRingImage != null)
        {
            shrinkingRingImage.color = normalColor;
        }

        if (keyPromptText != null)
        {
            keyPromptText.text = keyToPress.ToString();
            keyPromptText.color = initialTextColor;
        }

        if (targetZoneCircle != null)
        {
            float targetScale = Mathf.Lerp(0f, maxRingScale, (targetWindowStart + targetWindowEnd) * 0.5f);
            targetZoneCircle.localScale = new Vector3(targetScale, targetScale, 1f);
        }
    }

    public void CancelQTE()
    {
        EndQTESession();
    }

    private void EndPrompt(bool success)
    {
        IsPromptRunning = false;
        feedbackCoroutine = StartCoroutine(ShowFeedbackAndFinish(success));
    }

    private IEnumerator ShowFeedbackAndFinish(bool success)
    {
        Color outcomeColor = success ? successColor : failColor;

        if (shrinkingRingImage != null) shrinkingRingImage.color = outcomeColor;
        if (keyPromptText != null) keyPromptText.color = outcomeColor;

        Vector3 startScale = shrinkingRing != null ? shrinkingRing.localScale : Vector3.one;

        float elapsed = 0f;
        while (elapsed < feedbackDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / feedbackDuration;

            if (success)
            {
                if (shrinkingRing != null)
                {
                    float bump = 1f + Mathf.Sin(t * Mathf.PI) * 0.18f;
                    shrinkingRing.localScale = startScale * bump;
                }
            }
            else
            {
                // Schüttelt den inneren Text/Ring minimal, ohne die Weltprojektion des Containers zu zerschießen
                if (keyPromptText != null)
                {
                    float shake = Mathf.Sin(elapsed * 55f) * 8f * (1f - t);
                    keyPromptText.rectTransform.localPosition = new Vector3(shake, 0f, 0f);
                }
            }

            yield return null;
        }

        if (keyPromptText != null)
        {
            keyPromptText.rectTransform.localPosition = Vector3.zero;
        }

        targetWorldPos = null;
        feedbackCoroutine = null;

        if (ringContainer != null)
        {
            ringContainer.gameObject.SetActive(false);
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