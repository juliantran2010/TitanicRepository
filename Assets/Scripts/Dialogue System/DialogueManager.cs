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
    [SerializeField] private Button[] choices;
    private TextMeshProUGUI[] choicesText;

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
        choicesText = new TextMeshProUGUI[choices.Length];
        for (int i = 0; i < choices.Length; i++)
        {
            choicesText[i] = choices[i].gameObject.GetComponentInChildren<TextMeshProUGUI>();
            int choiceIndex = i; // Capture the current value of i
            choices[i].onClick.AddListener(() => MakeChoice(choiceIndex));
            choices[i].gameObject.SetActive(false); // Hide choices initially
        }
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
        dialogueText.text = text;
        continueButtonText.gameObject.SetActive(true);
        foreach (Button choice in choices)
        {
            choice.gameObject.SetActive(false);
        }
    }

    private void ContinueDialogue()
    {
        if (currentStory.canContinue)
        {
            string line = currentStory.Continue();
            CheckForTags();
            string[] parts = line.Split(new char[] { ':' });

            if (parts.Length == 2 && !parts[0].Contains(" ")) // Check if no spaces in the name part, to avoid splitting on colons in dialogue text
            {
                nameText.text = parts[0].Replace('_', ' ').Trim();
                nameText.color = nameText.text.ToLower() == "you" ? new Color32(92, 245, 155, 255) : new Color32(80, 120, 225, 255);
                dialogueText.text = parts[1].Trim();
            }
            else
            {
                nameText.text = "";
                dialogueText.text = line.Trim();
            }

            //Leere Zeilen überspringen (z.B. wenn in einer Zeile nur ein # trigger steht)
            if (dialogueText.text == "")
            {
                ContinueDialogue();
            }

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
        DialogueBox.SetActive(false);
        if (GameStateManager.Instance.CurrentState == GameState.Dialogue)
        {
            GameStateManager.Instance.SetState(previousGameState);
        }
        callbackOnDialogueEnd?.Invoke(currentStory);
    }

    private void DisplayChoices()
    {
        List<Choice> currentChoices = currentStory.currentChoices;
        dialogueText.text = "";
        continueButtonText.gameObject.SetActive(false);
        if (currentChoices.Count > choices.Length)
        {
            Debug.LogError("More choices than UI can support. Number of choices given: " + currentChoices.Count);
        }

        for (int i = 0; i < Math.Min(currentChoices.Count, choices.Length); i++)
        {
            choices[i].gameObject.SetActive(true);
            choicesText[i].text = "[" + currentChoices[i].text + "]";

            // Prüfen, wie oft das Ziel dieser Wahl in Ink bereits besucht wurde
            string pathString = currentChoices[i].pathStringOnChoice;
            int visits = currentStory.state.VisitCountAtPathString(pathString);
            choicesText[i].color = visits > 0 ? new Color(0.6f, 0.6f, 0.6f, 0.7f) : Color.white;
        }
        for (int i = currentChoices.Count; i < choices.Length; i++)
        {
            choices[i].gameObject.SetActive(false);
        }
    }

    private void MakeChoice(int choiceIndex)
    {
        currentStory.ChooseChoiceIndex(choiceIndex);
        foreach (Button choice in choices)
        {
            choice.gameObject.SetActive(false);
        }
        continueButtonText.gameObject.SetActive(true);
        ContinueDialogue();
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
}
