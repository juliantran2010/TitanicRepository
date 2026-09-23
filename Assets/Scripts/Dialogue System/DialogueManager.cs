using DG.Tweening;
using Ink.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    [Header("Dialogue UI")]
    [SerializeField] private GameObject DialogueBox;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private Button continueButton;
    [SerializeField] private TextMeshProUGUI continueButtonText;
    [SerializeField] private GameObject CompanionHologram;

    [Header("Choices UI")]
    [SerializeField] private Transform choicesContainer;
    [SerializeField] private GameObject choiceButtonPrefab;    // Dein Button-Prefab
    [SerializeField] private ScrollRect choicesScrollRect;       // Die ScrollView selbst
    private readonly List<GameObject> activeChoiceButtons = new List<GameObject>();

    [Header("Typewriter Settings")]
    [SerializeField] private float typingSpeed = 0.025f;
    [SerializeField] private float commaPause = 0.12f;
    [SerializeField] private float punctuationPause = 0.35f;
    private Coroutine typewriterCoroutine;
    private bool isTyping = false;

    [Header("State")]
    public static DialogueManager Instance { get; private set; }
    private Story currentStory;
    private Dialogue currentDialogue;
    private GameState previousGameState;
    private Action<Story> callbackOnDialogueEnd;
    private Action<string> callbackOnInkTrigger;
    public Action<string> OnInkTriggerFound;
    private bool isPaused = false;
    public bool IsPaused => isPaused;
    private string pendingLine = null;
    private readonly Queue<string> pendingTags = new Queue<string>();
    public string waitingForTriggerToFinish;

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

    void Start()
    {
        DialogueBox.SetActive(false);
        continueButton.onClick.AddListener(ContinueDialogue);
        CompanionHologram.SetActive(false);
    }

    public void StartDialogue(Dialogue dialogue, Action<Story> callbackDialogueEnd = null, Action<string> callbackInkTrigger = null)
    {
        if (dialogue.IsEmpty()) return;
        previousGameState = GameStateManager.Instance.CurrentState;
        currentDialogue = dialogue;
        currentStory = new Story(dialogue.inkJSON.text);
        callbackOnDialogueEnd = callbackDialogueEnd;
        callbackOnInkTrigger = callbackInkTrigger;

        //Start path setzen, falls angegeben, sonst den Anfang
        string startPath = dialogue.startPath != "" ? dialogue.startPath : currentStory.state.currentPathString;
        if (dialogue.dialogueState != "")
        {
            currentStory.state.LoadJson(dialogue.dialogueState);
        }
        currentStory.ChoosePathString(startPath);

        SyncVariablesWithInk(currentStory);
        GameStateManager.Instance.SetState(GameState.Dialogue);
        DialogueBox.SetActive(true);
        ContinueDialogue();
    }

    public void ShowText(string text, string speakerName = "")
    {
        previousGameState = GameStateManager.Instance.CurrentState;
        GameStateManager.Instance.SetState(GameState.Dialogue);
        DialogueBox.SetActive(true);
        nameText.text = speakerName;
        continueButtonText.gameObject.SetActive(true);
        ClearChoices();
        PlayTypewriterText(text.Trim());
    }

    private void ContinueDialogue()
    {
        if (isPaused) return;
        if (isTyping)
        {
            CompleteTypewriterText();
            return;
        }

        if (!DialogueBox.activeSelf) return;
        if (currentStory == null)
        {
            EndDialogue();
            return;
        }

        if (currentStory.canContinue)
        {
            string line = currentStory.Continue();
            CheckForTags();

            if (isPaused)
            {
                pendingLine = line;
                return;
            }

            if (!DialogueBox.activeSelf) return;

            ProcessAndDisplayLine(line);

        }
        else if (currentStory.currentChoices.Count > 0)
        {
            DisplayChoices();
        }
        else
        {
            EndDialogue();
        }
    }

    private void ProcessAndDisplayLine(string line)
    {
        int colonIndex = line.IndexOf(':');
        string textToDisplay = "";

        if (colonIndex > 0)
        {
            string potentialSpeaker = line.Substring(0, colonIndex).Trim();
            bool showHologram = potentialSpeaker.ToLower() == "companion" && SceneManager.GetActiveScene().name != "ComputerScene";
            CompanionHologram.SetActive(showHologram);

            // Sprecher-Namen haben normalerweise keine Leerzeichen (z. B. "Companion", "Old_Man")
            if (!potentialSpeaker.Contains(" "))
            {
                nameText.text = potentialSpeaker.Replace('_', ' ');
                nameText.color = nameText.text.ToLower() == "you" || nameText.text.ToLower() == "player"
                    ? new Color32(92, 245, 155, 255)
                    : new Color32(80, 120, 225, 255);

                textToDisplay = line.Substring(colonIndex + 1).Trim();    
            }
            else
            {
                nameText.text = "";
                textToDisplay = line.Trim();
            }
        }
        else
        {
            nameText.text = "";
            textToDisplay = line.Trim();
            CompanionHologram.SetActive(false);
        }

        //Leere Zeilen überspringen (z.B. wenn in einer Zeile nur ein # trigger steht)
        if (string.IsNullOrWhiteSpace(textToDisplay))
        {
            // Wenn noch Text oder Choices kommen -> weiter, sonst sofort sauber beenden!
            if (currentStory.canContinue || currentStory.currentChoices.Count > 0)
            {
                ContinueDialogue();
            }
            else
            {
                EndDialogue();
            }
            return;
        }

        PlayTypewriterText(textToDisplay);
    }

    private void CheckForTags()
    {
        var tags = currentStory.currentTags;

        for (int i = 0; i < tags.Count; i++)
        {
            ProcessSingleTag(tags[i]);

            // Sobald DIESER Tag pausiert hat:
            if (isPaused)
            {
                // Alle NACHFOLGENDEN Tags für nach dem Minigame merken
                pendingTags.Clear();
                for (int j = i + 1; j < tags.Count; j++)
                {
                    pendingTags.Enqueue(tags[j]);
                }
                break; // Schleife SOFORT beenden!
            }
        }
    }

    private void ProcessSingleTag(string tag)
    {
        string[] parts = tag.Split(':', 3);
        string command = parts[0].Trim().ToLower();

        switch (command)
        {
            case "set" when parts.Length >= 3:
                string variableName = parts[1].Trim();
                string value = parts[2].Trim().ToLower();
                QuestManager.Instance.HandleVariableTag(variableName, value);
                break;

            case "trigger" when parts.Length >= 2:
                string triggerName = parts[1].Trim();
                Debug.Log($"[DialogueManager] Trigger erkannt: {triggerName}");
                callbackOnInkTrigger?.Invoke(triggerName);
                StoryDirector.Instance?.TriggerEvent(triggerName);

                if (parts.Length >= 3 && parts[2].Trim().ToLower() == "pause")
                {
                    Debug.Log("[DialogueManager] Sofortige Pause angefordert.");
                    PauseDialogue(triggerName);
                }
                break;

            case "pause":
                PauseDialogue();
                break;

            case "add_quest" when parts.Length >= 3:
                string[] allQuests = tag.Split('>');
                Quest rootQuest = ParseQuestSegment(allQuests[0].Trim(), isFirstQuest: true);

                if (allQuests.Length > 1 && rootQuest != null)
                {
                    Quest currentPointer = rootQuest;
                    for (int i = 1; i < allQuests.Length; i++)
                    {
                        Quest nextQuest = ParseQuestSegment(allQuests[i].Trim(), isFirstQuest: false);
                        if (nextQuest != null)
                        {
                            currentPointer.nextQuest = nextQuest;
                            currentPointer = nextQuest;
                        }
                    }
                }

                if (rootQuest != null)
                {
                    QuestManager.Instance.AddQuest(rootQuest);
                }
                break;

            case "complete_quest" when parts.Length >= 2:
                string completeId = parts[1].Trim();
                QuestManager.Instance.CompleteQuest(completeId);
                break;

            case "progress_quest" when parts.Length >= 2:
                string targetQuestId = parts[1].Trim();
                int amount = 1;
                if (parts.Length >= 3)
                {
                    int.TryParse(parts[2].Trim(), out amount);
                }
                QuestManager.Instance.AddProgress(targetQuestId, amount);
                break;

            case "teleport" when parts.Length >= 3:
                string sceneName = parts[1].Trim();
                string spawnPointID = parts[2].Trim();
                GameSceneManager.Instance.ChangeScene(sceneName, spawnPointID);
                break;

            default:
                Debug.LogWarning($"Unbekannter Tag-Befehl: {command}");
                break;
        }
    }

    private Quest ParseQuestSegment(string segmentString, bool isFirstQuest)
    {
        // Erstes Segment: "add_quest:id:desc[:target]" -> bis zu 4 Teile
        // Folge-Segmente: "id:desc[:target]"           -> bis zu 3 Teile
        int maxSplits = isFirstQuest ? 4 : 3;
        string[] segments = segmentString.Split(':', maxSplits);

        int idIndex = isFirstQuest ? 1 : 0;
        int descIndex = isFirstQuest ? 2 : 1;
        int targetIndex = isFirstQuest ? 3 : 2;

        if (segments.Length <= descIndex) return null;

        string id = segments[idIndex].Trim();
        string desc = segments[descIndex].Trim();
        int target = -1;

        // Wurde ein Zähler mitgegeben?
        if (segments.Length > targetIndex)
        {
            int.TryParse(segments[targetIndex].Trim(), out target);
        }

        return new Quest
        {
            id = id,
            description = desc,
            currentProgress = 0,
            targetProgress = target
        };
    }

    private void EndDialogue()
    {
        if (typewriterCoroutine != null) StopCoroutine(typewriterCoroutine);
        isTyping = false;
        ClearChoices();
        DialogueBox.SetActive(false);
        if (GameStateManager.Instance.CurrentState == GameState.Dialogue)
        {
            GameStateManager.Instance.SetState(previousGameState);
        }
        callbackOnDialogueEnd?.Invoke(currentStory);
    }

    public void PauseDialogue(string triggerToFinish = "")
    {
        Debug.Log("[DialogueManager] PauseDialogue aufgerufen -> Box wird deaktiviert.");
        isPaused = true;
        waitingForTriggerToFinish = triggerToFinish;
        if (typewriterCoroutine != null) StopCoroutine(typewriterCoroutine);
        isTyping = false;
        DialogueBox.SetActive(false);
    }

    public void ResumeDialogue()
    {
        Debug.Log("[DialogueManager] ResumeDialogue aufgerufen -> Box wird reaktiviert.");
        isPaused = false;
        DialogueBox.SetActive(true);
        GameStateManager.Instance?.SetState(GameState.Dialogue);

        // 1. Zuerst die aufgeschobenen Tags ausführen (z. B. complete_quest)
        while (pendingTags.Count > 0)
        {
            string nextTag = pendingTags.Dequeue();
            ProcessSingleTag(nextTag);
            if (isPaused) return; // Falls direkt wieder pausiert wird
        }

        // 2. Jetzt die wartende Textzeile ohne Extraklick anzeigen
        if (!string.IsNullOrEmpty(pendingLine))
        {
            string lineToPlay = pendingLine;
            pendingLine = null;
            ProcessAndDisplayLine(lineToPlay);
        }
        else
        {
            ContinueDialogue();
        }
    }

    private void DisplayChoices()
    {
        ClearChoices();

        List<Choice> currentChoices = currentStory.currentChoices;
        dialogueText.text = "";
        continueButtonText.gameObject.SetActive(false);

        for (int i = 0; i < currentChoices.Count; i++)
        {
            int choiceIndex = i;
            Choice choice = currentChoices[i];

            GameObject btnObj = Instantiate(choiceButtonPrefab, choicesContainer);
            activeChoiceButtons.Add(btnObj);

            Button btn = btnObj.GetComponent<Button>();
            TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();

            if (btnText != null)
            {
                string prefix = $"<color=#D4AF37>{i + 1}. </color> ";
                btnText.text = prefix + choice.text;
                string pathString = choice.pathStringOnChoice;
                int visits = currentStory.state.VisitCountAtPathString(pathString);
                btnText.color = visits > 0 ? new Color(0.6f, 0.6f, 0.6f, 0.7f) : Color.white;
            }

            btn.onClick.AddListener(() => MakeChoice(choiceIndex));
        }
        if (choicesScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            choicesScrollRect.verticalNormalizedPosition = 1f;
        }
    }

    private void MakeChoice(int choiceIndex)
    {
        currentStory.ChooseChoiceIndex(choiceIndex);
        ClearChoices();
        continueButtonText.gameObject.SetActive(true);
        ContinueDialogue();
    }

    private void ClearChoices()
    {
        for (int i = 0; i < activeChoiceButtons.Count; i++)
        {
            if (activeChoiceButtons[i] != null)
                Destroy(activeChoiceButtons[i]);
        }
        activeChoiceButtons.Clear();
    }

    private void SyncVariablesWithInk(Story story)
    {
        if (QuestManager.Instance == null) return;
        var variableNames = story.variablesState.ToList();
        foreach (string varName in variableNames)
        {
            if (QuestManager.Instance.TryGetVariable(varName, out object value))
            {
                try
                {
                    story.variablesState[varName] = value;
                    Debug.Log(varName + " = " + value);
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"Konnte Variable '{varName}' nicht an Ink übergeben: {ex.Message}");
                }
            }
        }
    }

    private void PlayTypewriterText(string text)
    {
        if (typewriterCoroutine != null) StopCoroutine(typewriterCoroutine);

        dialogueText.text = text;
        dialogueText.maxVisibleCharacters = 0;
        dialogueText.ForceMeshUpdate();

        typewriterCoroutine = StartCoroutine(TypewriterRoutine());
    }

    private IEnumerator TypewriterRoutine()
    {
        isTyping = true;
        int totalChars = dialogueText.textInfo.characterCount;

        for (int i = 0; i < totalChars; i++)
        {
            dialogueText.maxVisibleCharacters = i + 1;
            char c = dialogueText.textInfo.characterInfo[i].character;
            bool hasNextChar = (i + 1 < totalChars);
            char nextC = hasNextChar ? dialogueText.textInfo.characterInfo[i + 1].character : '\0';

            if ((c == '.' || c == '!' || c == '?') && nextC != c)
            {
                yield return new WaitForSeconds(punctuationPause);
            }
            else if ((c == ',' || c == ';' || c == ':' || c == '—') && nextC != c)
            {
                yield return new WaitForSeconds(commaPause);
            }
            else
            {
                yield return new WaitForSeconds(typingSpeed);
            }
        }

        isTyping = false;
    }

    private void CompleteTypewriterText()
    {
        if (typewriterCoroutine != null) StopCoroutine(typewriterCoroutine);
        dialogueText.maxVisibleCharacters = dialogueText.textInfo.characterCount;
        isTyping = false;
    }
}
