using DG.Tweening;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class MesabaMinigameManager : MonoBehaviour
{
    public static MesabaMinigameManager Instance { get; private set; }

    [Header("Top / Objective (Optional)")]
    [SerializeField] private TextMeshProUGUI objectiveText;

    [Header("Right Choice Panel")]
    [SerializeField] private GameObject choicePanel;
    [SerializeField] private CanvasGroup choicePanelCanvasGroup;
    [SerializeField] private Transform choiceButtonContainer;
    [SerializeField] private GameObject choiceButtonPrefab;
    [SerializeField] private Button needHintButton;
    [SerializeField] private TextMeshProUGUI hintDisplayBox;

    [Header("Bottom Bar Controls")]
    [SerializeField] private Button backButton;
    [SerializeField] private Button checkMessageButton;
    [SerializeField] private TextMeshProUGUI bottomInstructionText;

    [Header("Settings")]
    [SerializeField] private float animDuration = 0.2f;

    // Dynamische Laufzeitdaten von der Note
    private List<MesabaGap> activeGaps = new List<MesabaGap>();
    private MesabaNote activeNote;
    private Action onSolvedCallback;
    private MesabaGap selectedGap;
    private CanvasGroup mainCanvasGroup;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        mainCanvasGroup = GetComponent<CanvasGroup>();

        if (choicePanel != null && choicePanelCanvasGroup == null)
        {
            choicePanelCanvasGroup = choicePanel.GetComponent<CanvasGroup>();
            if (choicePanelCanvasGroup == null) choicePanelCanvasGroup = choicePanel.AddComponent<CanvasGroup>();
        }

        // Button Listener
        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackClicked);
        }

        if (checkMessageButton != null)
        {
            checkMessageButton.onClick.AddListener(OnCheckMessageClicked);
        }

        if (needHintButton != null)
        {
            needHintButton.onClick.AddListener(OnNeedHintClicked);
        }

        gameObject.SetActive(false);
    }

    /// <summary>
    /// Wird von MinigameNote aufgerufen, sobald die Notiz vor der Kamera ist.
    /// </summary>
    public void OpenMinigame(List<MesabaGap> gaps, MesabaNote note, Action onSolved = null)
    {
        activeGaps = gaps;
        activeNote = note;
        onSolvedCallback = onSolved;
        selectedGap = null;
        Debug.Log($"[Check] Anzahl Gaps: {activeGaps.Count}");
        gameObject.SetActive(true);
        mainCanvasGroup.DOKill();
        mainCanvasGroup.alpha = 0f;
        mainCanvasGroup.DOFade(1f, animDuration);

        if (choicePanel != null) choicePanel.SetActive(false);
        if (hintDisplayBox != null) hintDisplayBox.gameObject.SetActive(false);

        if (checkMessageButton != null) checkMessageButton.interactable = false;
        if (bottomInstructionText != null)
        {
            bottomInstructionText.text = "Click on a coffee stain to choose the correct word.";
        }

        // Flecken des Zettels mit Klick-Event verknüpfen
        foreach (var gap in activeGaps)
        {
            gap.Setup(OnGapClicked);
        }
    }

    public void CloseMinigame()
    {
        mainCanvasGroup.DOKill();
        mainCanvasGroup.DOFade(0f, animDuration).OnComplete(() =>
        {
            if (choicePanel != null) choicePanel.SetActive(false);
            gameObject.SetActive(false);
            activeGaps.Clear();
            activeNote = null;
            selectedGap = null;
        });
    }

    private void OnGapClicked(MesabaGap gap)
    {
        selectedGap = gap;

        if (choicePanel != null)
        {
            choicePanel.SetActive(true);
            choicePanel.transform.DOKill();
            choicePanel.transform.localScale = Vector3.one * 0.95f;
            choicePanel.transform.DOScale(1f, animDuration).SetEase(Ease.OutBack);

            if (choicePanelCanvasGroup != null)
            {
                choicePanelCanvasGroup.alpha = 0f;
                choicePanelCanvasGroup.DOFade(1f, animDuration);
            }
        }

        if (hintDisplayBox != null) hintDisplayBox.gameObject.SetActive(false);

        // Alte Options-Buttons abräumen
        for (int i = choiceButtonContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(choiceButtonContainer.GetChild(i).gameObject);
        }

        // Optionen mischen (1 richtige + Ablenker)
        List<string> options = new List<string> { gap.CorrectWord };
        if (gap.Distractors != null)
        {
            options.AddRange(gap.Distractors);
        }

        for (int i = 0; i < options.Count; i++)
        {
            string temp = options[i];
            int rnd = UnityEngine.Random.Range(i, options.Count);
            options[i] = options[rnd];
            options[rnd] = temp;
        }

        // Auswahlbuttons instanziieren
        foreach (string option in options)
        {
            GameObject btnObj = Instantiate(choiceButtonPrefab, choiceButtonContainer);
            TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null) btnText.text = option;

            string wordChoice = option;
            btnObj.GetComponent<Button>().onClick.AddListener(() =>
            {
                selectedGap.SetChoice(wordChoice);
                if (choicePanel != null) choicePanel.SetActive(false);
                ValidateAllGapsFilled();
            });
        }
    }

    private void OnNeedHintClicked()
    {
        if (selectedGap == null || hintDisplayBox == null) return;

        hintDisplayBox.gameObject.SetActive(true);
        hintDisplayBox.text = selectedGap.HintText;
        hintDisplayBox.transform.DOKill();
        hintDisplayBox.transform.DOPunchScale(Vector3.one * 0.05f, 0.2f);
    }

    private void ValidateAllGapsFilled()
    {
        bool allFilled = true;
        foreach (var gap in activeGaps)
        {
            if (string.IsNullOrEmpty(gap.CurrentChoice))
            {
                allFilled = false;
                break;
            }
        }

        if (checkMessageButton != null)
        {
            checkMessageButton.interactable = allFilled;
        }
    }

    private void OnCheckMessageClicked()
    {
        bool allCorrect = true;
        foreach (var gap in activeGaps)
        {
            if (!gap.IsCorrect)
            {
                allCorrect = false;
                break;
            }
        }

        if (allCorrect)
        {
            if (bottomInstructionText != null)
            {
                bottomInstructionText.text = "<color=#7CFC00>Message successfully restored!</color>";
            }

            checkMessageButton.interactable = false;

            // Nach 1.2 Sekunden Zettel ablegen und Minigame beenden
            DOVirtual.DelayedCall(1.2f, () =>
            {
                onSolvedCallback?.Invoke();
                if (activeNote != null)
                {
                    activeNote.CloseNote();
                }
            });
        }
        else
        {
            if (bottomInstructionText != null)
            {
                bottomInstructionText.text = "<color=#FF6347>Some words are incorrect. Check again!</color>";
            }

            // Haptisches Rütteln des Buttons bei falscher Lösung
            checkMessageButton.transform.DOKill();
            checkMessageButton.transform.DOShakePosition(0.35f, strength: new Vector3(8f, 0, 0), vibrato: 15);
        }
    }

    private void OnBackClicked()
    {
        if (activeNote != null)
        {
            activeNote.CloseNote();
        }
    }
}