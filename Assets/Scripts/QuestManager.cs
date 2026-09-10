using DG.Tweening;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    [Header("UI Referenzen")]
    [SerializeField] private Transform questContainer; // Das Objekt mit dem Vertical Layout Group
    [SerializeField] private GameObject questItemPrefab; // Dein QuestEntryPrefab

    public Action<string> OnQuestAdded;
    public Action<string> OnQuestCompleted;

    // Speichert aktive Quests mit einer ID/Titel
    private readonly Dictionary<string, GameObject> activeQuests = new Dictionary<string, GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Fügt dem Log eine neue Quest mit weichem Einblenden hinzu.
    /// </summary>
    public void AddQuest(string questId, string questDescription)
    {
        if (activeQuests.ContainsKey(questId)) return;

        // Neues Text-Element aus dem Prefab instanziieren
        GameObject newEntry = Instantiate(questItemPrefab, questContainer);

        // Text setzen
        if (newEntry.TryGetComponent<TMP_Text>(out var textComponent))
        {
            textComponent.text = $"<color=#FFCC00><b>(!)</b></color> {questDescription}";
        }

        // DOTween: Zarter Einblend-Effekt (Scale-In von 0 auf 1)
        newEntry.transform.localScale = Vector3.zero;
        newEntry.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);

        activeQuests.Add(questId, newEntry);
        OnQuestAdded?.Invoke(questId);
    }

    /// <summary>
    /// Markiert eine Quest als erledigt (streicht sie durch oder blendet sie aus).
    /// </summary>
    public void CompleteQuest(string questId, bool removeImmediately = false)
    {
        if (!activeQuests.TryGetValue(questId, out GameObject entry)) return;

        if (entry.TryGetComponent<TMP_Text>(out var textComponent))
        {
            // Text grün färben und durchstreichen
            textComponent.color = Color.gray;
            textComponent.text = $"<s>{textComponent.text}</s>";
        }

        if (removeImmediately)
        {
            RemoveQuest(questId);
        }
        else
        {
            // Nach 2 Sekunden weich ausblenden und zerstören
            entry.transform.DOScale(Vector3.zero, 0.4f)
                .SetDelay(2.5f)
                .SetEase(Ease.InBack)
                .OnComplete(() => RemoveQuest(questId));
        }
    }

    private void RemoveQuest(string questId)
    {
        if (activeQuests.TryGetValue(questId, out GameObject entry))
        {
            activeQuests.Remove(questId);
            Destroy(entry);
            OnQuestCompleted?.Invoke(questId);
        }
    }
}
