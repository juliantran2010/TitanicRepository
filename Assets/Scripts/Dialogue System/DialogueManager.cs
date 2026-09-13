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
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    [Header("Dialogue UI")]
    [SerializeField] private GameObject DialogueBox;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private Button continueButton;
    [SerializeField] private TextMeshProUGUI continueButtonText;

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
    }

    public void StartDialogue(Dialogue dialogue, Action<Story> callbackDialogueEnd = null, Action<string> callbackInkTrigger = null)
    {
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
        if (isTyping)
        {
            CompleteTypewriterText();
            return;
        }
        if (!DialogueBox.activeSelf) return;

        if (currentStory.canContinue)
        {
            string line = currentStory.Continue();
            CheckForTags();
            if (!DialogueBox.activeSelf) return;

            string[] parts = line.Split(new char[] { ':' });
            string textToDisplay = "";
            if (parts.Length == 2 && !parts[0].Contains(" ")) // Check if no spaces in the name part, to avoid splitting on colons in dialogue text
            {
                nameText.text = parts[0].Replace('_', ' ').Trim();
                nameText.color = nameText.text.ToLower() == "you" ? new Color32(92, 245, 155, 255) : new Color32(80, 120, 225, 255);
                textToDisplay = parts[1].Trim();
            }
            else
            {
                nameText.text = "";
                textToDisplay = line.Trim();
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
        else if (currentStory.currentChoices.Count > 0)
        {
            DisplayChoices();
        }
        else
        {
            EndDialogue();
        }
    }

    private void CheckForTags()
    {
        foreach (string tag in currentStory.currentTags)
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
                    callbackOnInkTrigger?.Invoke(triggerName);
                    break;
                case "add_quest" when parts.Length >= 3:
                    //quests can be chained with '>' to indicate a sequence of quests
                    string[] allQuests = tag.Split('>');
                    string[] firstQuestParts = allQuests[0].Split(':', 3);
                    string rootId = firstQuestParts[1].Trim();
                    string rootDescription = firstQuestParts.Length >= 3 ? firstQuestParts[2].Trim() : "";
                    Quest rootQuest = new Quest
                    {
                        id = rootId,
                        description = rootDescription
                    };

                    if (allQuests.Length > 1)
                    {
                        Quest currentPointer = rootQuest;
                        for (int i = 1; i < allQuests.Length; i++)
                        {
                            string questString = allQuests[i].Trim();
                            string[] questParts = questString.Split(':', 2);
                            if (questParts.Length >= 2)
                            {
                                Quest nextQuest = new Quest
                                {
                                    id = questParts[0].Trim(),
                                    description = questParts[1].Trim()
                                };

                                // Kette anhängen und Pointer weiterbewegen
                                currentPointer.nextQuest = nextQuest;
                                currentPointer = nextQuest;
                            }
                        }
                    }
                    QuestManager.Instance.AddQuest(rootQuest);
                    break;
                case "complete_quest" when parts.Length >= 2:
                    string completeId = parts[1].Trim();
                    QuestManager.Instance.CompleteQuest(completeId);
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
