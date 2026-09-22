using DG.Tweening;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class SentenceMinigameController : MonoBehaviour
{
    public static SentenceMinigameController Instance { get; private set; }

    [Header("Datenquelle")]
    [SerializeField] private SentenceOrderingData minigameData;

    [Header("UI Spalten (3 Stück: 0, 1, 2)")]
    [SerializeField] private List<Transform> optionsPoolContainers;
    [SerializeField] private List<TextMeshProUGUI> columnHeaderTexts;
    [SerializeField] private List<SentenceDropSlot> dropSlots;

    [Header("Prefabs & Buttons")]
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private Button submitButton;
    [SerializeField] private Button closeButton; // Optional: Schließen-Button (X)

    [Header("Animation & Sound")]
    [SerializeField] private float fadeDuration = 0.35f;

    // Events für externe Skripte (StoryDirector, SoundManager, etc.)
    public event Action OnMinigameStarted;
    public event Action OnMinigameCompleted;
    public event Action OnMinigameFailedAttempt;

    private CanvasGroup canvasGroup;
    private Action onCompleteCallback;
    private readonly string[] letters = new string[] { "A.", "B.", "C.", "D.", "E." };

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        canvasGroup = GetComponent<CanvasGroup>();

        submitButton.onClick.AddListener(OnSubmitClicked);

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(() => CloseMinigame(false));
        }

        // Zu Beginn unsichtbar & nicht interaktiv schalten
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        //gameObject.SetActive(false);
    }

    private void Start()
    {
        if (StoryDirector.Instance != null)
        {
            StoryDirector.Instance.OnStoryEventTriggered += HandleStoryTrigger;
        }
    }

    private void OnDestroy()
    {
        if (StoryDirector.Instance != null)
        {
            StoryDirector.Instance.OnStoryEventTriggered -= HandleStoryTrigger;
        }
    }

    private void HandleStoryTrigger(string triggerName)
    {
        if (triggerName == "sentence_minigame")
        {
            Debug.Log("starting minigame");
            OpenMinigame(null, () =>
            {
                if (DialogueManager.Instance != null)
                {
                    DialogueManager.Instance.ResumeDialogue();
                }
                Debug.Log("Minigame won");
            });
        }
    }

    /// <summary>
    /// Öffnet das Minigame mit den angegebenen Daten (oder den voreingestellten)
    /// </summary>
    public void OpenMinigame(SentenceOrderingData customData = null, Action callback = null)
    {
        if (customData != null) minigameData = customData;
        onCompleteCallback = callback;

        // 1. GameState sperren (Mauszeiger frei)
        GameStateManager.Instance?.SetState(GameState.Minigame);

        // 2. Karten aufbauen & UI leeren
        BuildMinigame();

        // 3. UI sichtbar machen & einblenden
        gameObject.SetActive(true);
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;

        canvasGroup.DOKill();
        canvasGroup.DOFade(1f, fadeDuration).SetUpdate(true);

        OnMinigameStarted?.Invoke();
    }

    /// <summary>
    /// Schließt das Minigame und stellt den GameState wieder her
    /// </summary>
    public void CloseMinigame(bool completed)
    {
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        canvasGroup.DOKill();
        canvasGroup.DOFade(0f, fadeDuration).SetUpdate(true).OnComplete(() =>
        {
            gameObject.SetActive(false);
            GameStateManager.Instance?.SetState(GameState.Gameplay);

            if (completed)
            {
                onCompleteCallback?.Invoke();
            }
        });
    }

    public void BuildMinigame()
    {
        if (minigameData == null) return;

        // Alte Karten aus den Pools löschen
        foreach (var container in optionsPoolContainers)
        {
            foreach (Transform child in container) Destroy(child.gameObject);
        }

        // Alte Slots leeren
        foreach (var slot in dropSlots)
        {
            slot.ClearSlot();
        }

        // Neue Karten instanziieren
        for (int i = 0; i < minigameData.slots.Count && i < dropSlots.Count; i++)
        {
            var slotData = minigameData.slots[i];

            if (i < columnHeaderTexts.Count && columnHeaderTexts[i] != null)
            {
                columnHeaderTexts[i].text = slotData.headerTitle;
            }

            Transform pool = optionsPoolContainers[i];

            for (int optIndex = 0; optIndex < slotData.options.Count; optIndex++)
            {
                GameObject cardObj = Instantiate(cardPrefab, pool);
                var card = cardObj.GetComponent<DraggableSentenceCard>();
                card.Setup(letters[optIndex], slotData.options[optIndex]);
                card.OriginalParent = pool;
            }
        }

        NotifySelectionChanged();
    }

    public void NotifySelectionChanged()
    {
        bool allFilled = true;
        foreach (var slot in dropSlots)
        {
            if (slot.CurrentCard == null)
            {
                allFilled = false;
                break;
            }
        }

        submitButton.interactable = allFilled;
    }

    private void OnSubmitClicked()
    {
        bool allCorrect = true;

        foreach (var slot in dropSlots)
        {
            if (slot.CurrentCard == null || !slot.CurrentCard.Data.isCorrect)
            {
                allCorrect = false;
                break;
            }
        }

        if (allCorrect)
        {
            Debug.Log("[Minigame] Richtig gelöst!");
            submitButton.interactable = false;

            // 1. Quest abschließen
            if (QuestManager.Instance != null && !string.IsNullOrEmpty(minigameData.completedQuestId))
            {
                QuestManager.Instance.CompleteQuest(minigameData.completedQuestId);
            }

            // 2. StoryDirector Event auslösen
            StoryDirector.Instance?.TriggerEvent("sentence_minigame_won");

            // 3. Event feuern
            OnMinigameCompleted?.Invoke();

            // 4. Nach kurzer Pause sanft ausblenden
            DOVirtual.DelayedCall(0.8f, () => CloseMinigame(true));
        }
        else
        {
            Debug.Log("[Minigame] Falsche Zuordnung!");
            OnMinigameFailedAttempt?.Invoke();

            // Rüttel-Effekt für den Button
            submitButton.transform.DOKill(true);
            submitButton.transform.DOPunchPosition(new Vector3(15f, 0, 0), 0.4f, 20);
        }
    }
}