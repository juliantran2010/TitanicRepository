using DG.Tweening;
using Ink.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;
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
    public Action<Dialogue, Story> OnDialogueEnd;
    public Action<string> OnTriggerFound;

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

    public void StartDialogue(Dialogue dialogue, Dictionary<string, object> variables = null)
    {
        currentDialogue = dialogue;
        currentStory = new Story(dialogue.inkJSON.text);
        string startPath = currentStory.state.currentPathString;
        if (dialogue.dialogueState != "")
        { 
            currentStory.state.LoadJson(dialogue.dialogueState);
            currentStory.ChoosePathString(startPath); // go to beginning
        }

        if (variables != null)
        {
            foreach (var variable in variables)
            {
                SetInkVariable(variable.Key, variable.Value);
            }
        }

        GameStateManager.Instance.SetState(GameState.Dialogue);
        DialogueBox.SetActive(true);
        ContinueDialogue();
    }

    private void SetInkVariable(string varName, object value)
    {
        if (currentStory.variablesState.GlobalVariableExistsWithName(varName))
        {
            currentStory.variablesState[varName] = value;
        }
        else
        {
            Debug.LogWarning($"Variable '{varName}' existiert nicht in der Ink-Story!");
        }
    }

    private void ContinueDialogue()
    {
        if (currentStory.canContinue)
        {
            string line = currentStory.Continue();
            CheckForTags();
            string[] parts = line.Split(new char[] { ':' });

            if (parts.Length == 2)
            {
                nameText.text = parts[0].Trim();
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
            if (tag.StartsWith("trigger:"))
            {
                string triggerName = tag.Replace("trigger:", "").Trim();
                OnTriggerFound?.Invoke(triggerName);
            }
        }
    }

    private void EndDialogue()
    {
        DialogueBox.SetActive(false);
        GameStateManager.Instance.SetState(GameState.Gameplay);
        OnDialogueEnd?.Invoke(currentDialogue, currentStory);
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
            choicesText[i].text = currentChoices[i].text;

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
}
