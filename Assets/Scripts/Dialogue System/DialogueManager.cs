using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueManager : MonoBehaviour
{
    public GameObject DialogueBox;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dialogueText;

    public static DialogueManager Instance { get; private set; }

    public bool isInDialogue = false;

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

    private Queue<DialogueLine> sentences;

    void Start()
    {
        sentences = new Queue<DialogueLine>();
    }

    private void Update()
    {
        if (isInDialogue && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            DisplayNextSentence();
        }
    }

    public void StartDialogue(Dialogue dialogue)
    {
        sentences.Clear();
        DialogueBox.SetActive(true);
        foreach (DialogueLine line in dialogue.dialogueLines)
        {
            sentences.Enqueue(line);
        }
        DisplayNextSentence();
        isInDialogue = true;
    }

    public void DisplayNextSentence()
    {
        if (sentences.Count == 0)
        {
            EndDialogue();
            return;
        }

        DialogueLine line = sentences.Dequeue();
        nameText.text = line.name;
        dialogueText.text = line.sentence;
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
        isInDialogue = false;
    }
}
