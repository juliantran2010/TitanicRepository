using DG.Tweening;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
            textComponent.ForceMeshUpdate(); // Erzwingt ein Update, um die Größe korrekt zu berechnen
        }

        // DOTween: Zarter Einblend-Effekt (Scale-In von 0 auf 1)
        newEntry.transform.localScale = Vector3.zero;
        newEntry.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);

        LayoutRebuilder.ForceRebuildLayoutImmediate(newEntry.GetComponent<RectTransform>());
        LayoutRebuilder.ForceRebuildLayoutImmediate(questContainer.GetComponent<RectTransform>());

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
            completedQuests.Add(questId);
            OnQuestCompleted?.Invoke(questId);
        }
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
}
