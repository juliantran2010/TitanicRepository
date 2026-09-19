using UnityEngine;

public class IdentifiableObject : InteractableObject
{
    public override InteractionType Type => InteractionType.Inspect;

    [Header("Identify Object")]
    [SerializeField] private string correctWord;
    [SerializeField] private string[] alternativeWords;

    public string CorrectWord => correctWord;
    public bool IsIdentified => GetPersistentStateValue<bool>("is_identified", false);
    [SerializeField] private WordBankData wordBank;
    public WordBankData WordBank => wordBank;

    protected override void Start()
    {
        base.Start();
        displayName = "?";
        if (QuestManager.Instance.IsQuestCompleted(progessQuestId))
        {
            MarkAsCompleted();
        }
        QuestManager.Instance.OnQuestCompleted += HandleQuestCompleted;
    }

    private void HandleQuestCompleted(string questId)
    {
        if (questId == progessQuestId)
        {
            MarkAsCompleted();
            QuestManager.Instance.OnQuestCompleted -= HandleQuestCompleted;
        }
    }

    protected override void OnInteract()
    {
        // UI öffnen und mit den Daten dieses Objekts füttern
        IdentifyOverlay.Instance.Open(this);
    }

    public bool CheckAnswer(string input)
    {
        string cleanInput = input.Trim().ToLowerInvariant();

        if (cleanInput == correctWord.Trim().ToLowerInvariant())
            return true;

        if (alternativeWords != null)
        {
            foreach (var alt in alternativeWords)
            {
                if (cleanInput == alt.Trim().ToLowerInvariant())
                    return true;
            }
        }

        return false;
    }

    public void MarkAsCompleted()
    {
        SetPersistentStateValue("is_not_interactable", true);
        if (!string.IsNullOrWhiteSpace(progessQuestId))
        {
            QuestManager.Instance.AddProgress(progessQuestId);
        }
        isNotInteractable = true;
    }
}
