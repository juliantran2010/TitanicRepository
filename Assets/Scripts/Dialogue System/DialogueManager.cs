using Ink.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
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
    public bool dialogueIsPlaying = false;
    private Story currentStory;
    private Dialogue currentDialogue;
    public Action<Dialogue> OnDialogueEnd;

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
        currentStory = new Story(dialogue.inkJSON.text);
        currentDialogue = dialogue;

        if (variables != null)
        {
            foreach (var variable in variables)
            {
                SetInkVariable(variable.Key, variable.Value);
            }
        }

        dialogueIsPlaying = true;
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

    private void EndDialogue()
    {
        DialogueBox.SetActive(false);
        StartCoroutine(disableDialogueInNextFrame());
    }

    /// <summary>
    /// Disable Dialogue in next frame to avoid starting a new one directly after ending
    /// </summary>
    /// <returns></returns>
    private IEnumerator disableDialogueInNextFrame()
    {
        yield return null;
        dialogueIsPlaying = false;
        OnDialogueEnd?.Invoke(currentDialogue);
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
        for (int i = 0; i < currentChoices.Count; i++)
        {
            choices[i].gameObject.SetActive(true);
            choicesText[i].text = currentChoices[i].text;
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
