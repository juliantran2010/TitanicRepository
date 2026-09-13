using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Quest
{
    public string id;
    public string description;
    public Quest nextQuest = null;

    public GameObject questEntryObject = null; // Referenz auf das zugehörige UI-Element
}
public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    [Header("UI Referenzen")]
    [SerializeField] private Transform questContainer; // Das Objekt mit dem Vertical Layout Group
    [SerializeField] private GameObject questItemPrefab; // Dein QuestEntryPrefab

    public Action<string> OnQuestAdded;
    public Action<string> OnQuestCompleted;

    // Speichert aktive Quests mit einer ID/Titel
    private readonly Dictionary<string, Quest> activeQuests = new Dictionary<string, Quest>();
    private readonly List<string> completedQuests = new List<string>();
    private readonly Dictionary<string, object> globalVariables = new Dictionary<string, object>();

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

    private void OnVariableChanged(string varName, object value)
    {
        if (varName.StartsWith("has_") && value is bool hasValue && hasValue)
        {
            CompleteQuest("pickup_" + varName.Substring(4));
        }
    }

    public void AddQuest(string questId, string description)
    {
        Quest newQuest = new Quest { id = questId, description = description };
        AddQuest(newQuest);
    }

    /// <summary>
    /// Fügt dem Log eine neue Quest mit weichem Einblenden hinzu.
    /// </summary>
    public void AddQuest(Quest quest)
    {
        if (activeQuests.ContainsKey(quest.id)) return;

        // Neues Text-Element aus dem Prefab instanziieren
        GameObject newEntry = Instantiate(questItemPrefab, questContainer);

        // Text setzen
        if (newEntry.TryGetComponent<TMP_Text>(out var textComponent))
        {
            textComponent.text = $"<color=#FFCC00><b>(!)</b></color> {quest.description}";
            textComponent.ForceMeshUpdate(); // Erzwingt ein Update, um die Größe korrekt zu berechnen
        }

        // DOTween: Zarter Einblend-Effekt (Scale-In von 0 auf 1)
        newEntry.transform.localScale = Vector3.zero;
        newEntry.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);

        LayoutRebuilder.ForceRebuildLayoutImmediate(newEntry.GetComponent<RectTransform>());
        LayoutRebuilder.ForceRebuildLayoutImmediate(questContainer.GetComponent<RectTransform>());
        quest.questEntryObject = newEntry; // Referenz auf das UI-Element speichern

        activeQuests.Add(quest.id, quest);
        OnQuestAdded?.Invoke(quest.id);
    }

    /// <summary>
    /// Markiert eine Quest als erledigt (streicht sie durch oder blendet sie aus).
    /// </summary>
    public void CompleteQuest(string questId)
    {
        if (!activeQuests.TryGetValue(questId, out Quest quest)) return;

        GameObject entry = quest.questEntryObject;
        if (entry == null) return;

        if (entry.TryGetComponent<TMP_Text>(out var textComponent))
        {
            // Text grün färben und durchstreichen
            textComponent.color = Color.gray;
            textComponent.text = $"<s>{textComponent.text}</s>";
        }

        activeQuests.Remove(questId);
        completedQuests.Add(questId);
        OnQuestCompleted?.Invoke(questId);
        if (quest.nextQuest != null)
            AddQuest(quest.nextQuest); // Nächste Quest hinzufügen, falls vorhanden

        // Nach 2 Sekunden weich ausblenden und zerstören
        entry.transform.DOScale(Vector3.zero, 0.4f)
            .SetDelay(2.5f)
            .SetEase(Ease.InBack)
            .OnComplete(() => {
                Destroy(quest.questEntryObject);
            });
    }

    public void SetVariable(string varName, object value)
    {
        globalVariables[varName] = value;
        OnVariableChanged(varName, value);
    }

    public bool TryGetVariable(string varName, out object value)
    {
        return globalVariables.TryGetValue(varName, out value);
    }

    public void HandleVariableTag(string name, string valStr)
    {
        if (bool.TryParse(valStr, out bool boolVal))
            SetVariable(name, boolVal);
        else if (int.TryParse(valStr, out int intVal))
            SetVariable(name, intVal);
        else
            SetVariable(name, valStr);
    }

    public bool IsQuestCompleted(string questId)
    {
        return completedQuests.Contains(questId);
    }

    public Quest GetOldestActiveQuest()
    {
        if (activeQuests.Count == 0) return null;
        // Die erste hinzugefügte Quest zurückgeben
        return activeQuests.Values.First();
    }
}
