using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class IdentifyOverlay : MonoBehaviour
{
    public static IdentifyOverlay Instance { get; private set; }

    [Header("Input Panel")]
    [SerializeField] private GameObject inputPanel;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private GameObject incorrectBadge; // Rotes X
    [SerializeField] private GameObject correctBadge;   // Grüner Haken

    [Header("Hint Trigger Button")]
    [SerializeField] private Button needHintButton;     // Button "? Need a hint?"

    [Header("Hint Window / Modal")]
    [SerializeField] private GameObject hintModal;      // Das Pop-up-Fenster
    [SerializeField] private TextMeshProUGUI hintTitle; // Überschrift
    [SerializeField] private Transform wordGridParent;  // Container für die Buttons
    [SerializeField] private GridLayoutGroup wordGridLayout; // Das Grid-Layout auf wordGridParent
    [SerializeField] private GameObject wordButtonPrefab;

    private IdentifiableObject currentTarget;
    private int currentHintStage = 0;

    private void Awake()
    {
        Instance = this;

        if (wordGridLayout == null && wordGridParent != null)
        {
            wordGridLayout = wordGridParent.GetComponent<GridLayoutGroup>();
        }

        inputField.onSubmit.AddListener(SubmitAnswer);
        needHintButton.onClick.AddListener(OnNeedHintClicked);

        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!gameObject.activeSelf) return;

        // Neues Input System: Tastaturabfrage für Escape
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (hintModal != null && hintModal.activeSelf)
            {
                CloseHintModal();
            }
            else
            {
                Close();
            }
        }
    }

    public void Open(IdentifiableObject target)
    {
        GameStateManager.Instance.SetState(GameState.Inspect);
        currentTarget = target;
        currentHintStage = 0;

        gameObject.SetActive(true);
        inputPanel.SetActive(true);
        if (hintModal != null) hintModal.SetActive(false);

        incorrectBadge.SetActive(false);
        correctBadge.SetActive(false);
        needHintButton.gameObject.SetActive(false);

        inputField.text = "";
        inputField.interactable = true;
        inputField.ActivateInputField();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void SubmitAnswer(string input)
    {
        if (currentTarget == null || string.IsNullOrWhiteSpace(input)) return;

        if (currentTarget.CheckAnswer(input))
        {
            incorrectBadge.SetActive(false);
            correctBadge.SetActive(true);
            needHintButton.gameObject.SetActive(false);
            inputField.interactable = false;

            currentTarget.MarkAsCompleted();
            DOVirtual.DelayedCall(1.2f, Close);
        }
        else
        {
            incorrectBadge.SetActive(true);
            needHintButton.gameObject.SetActive(true);
            needHintButton.transform.DOPunchScale(Vector3.one * 0.15f, 0.3f);
        }
    }

    private void OnNeedHintClicked()
    {
        currentHintStage++;
        hintModal.SetActive(true);

        // Vorherige Buttons löschen
        foreach (Transform child in wordGridParent)
        {
            Destroy(child.gameObject);
        }

        List<string> options = new List<string>();

        if (currentHintStage == 1)
        {
            // Hint 1: Der komplette Wort-Pool
            hintTitle.text = "Choose a word from the list:";
            if (currentTarget.WordBank != null)
            {
                options.AddRange(currentTarget.WordBank.Words);
            }
        }
        else if (currentHintStage == 2)
        {
            // Hint 2: Weniger Optionen (Richtige Antwort + 5 zufällige Ablenker = 6 Optionen)
            hintTitle.text = "Choose a word:";
            options.Add(currentTarget.CorrectWord);
            options.AddRange(GetRandomDistractors(5));
        }
        else
        {
            // Hint 3: Multiple Choice (Richtige Antwort + 2 zufällige Ablenker = 3 Optionen)
            hintTitle.text = "Which word is correct?";
            options.Add(currentTarget.CorrectWord);
            options.AddRange(GetRandomDistractors(2));
        }

        // Liste durchmischen (Fisher-Yates Shuffle)
        for (int i = 0; i < options.Count; i++)
        {
            string temp = options[i];
            int rnd = Random.Range(i, options.Count);
            options[i] = options[rnd];
            options[rnd] = temp;
        }

        // --- DYNAMISCHES GRID-LAYOUT ---
        if (wordGridLayout != null)
        {
            Canvas.ForceUpdateCanvases();
            RectTransform gridRect = wordGridParent as RectTransform;
            float totalWidth = gridRect.rect.width;

            if (options.Count <= 3)
            {
                // Bei 3 oder weniger Elementen: 1 Spalte (rein vertikal)
                wordGridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                wordGridLayout.constraintCount = 1;

                float singleColWidth = totalWidth - wordGridLayout.padding.horizontal;
                wordGridLayout.cellSize = new Vector2(singleColWidth, 50f);
                wordGridLayout.spacing = new Vector2(0f, 10f);
            }
            else
            {
                // Bei mehr Elementen: 2 Spalten (oder 3 bei > 6 Optionen)
                int columns = options.Count > 6 ? 3 : 2;

                wordGridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                wordGridLayout.constraintCount = columns;

                float availableWidth = totalWidth - wordGridLayout.padding.horizontal - (wordGridLayout.spacing.x * (columns - 1));
                float cellWidth = availableWidth / columns;

                wordGridLayout.cellSize = new Vector2(cellWidth, 45f);
                wordGridLayout.spacing = new Vector2(10f, 10f);
            }
        }

        // Buttons instanziieren
        foreach (string word in options)
        {
            GameObject btn = Instantiate(wordButtonPrefab, wordGridParent);
            btn.GetComponentInChildren<TextMeshProUGUI>().text = word;

            string chosen = word;
            btn.GetComponent<Button>().onClick.AddListener(() =>
            {
                inputField.text = chosen;
                CloseHintModal();
                SubmitAnswer(chosen);
            });
        }
    }

    // Zieht 'count' zufällige Begriffe aus der WordBank, die NICHT das Lösungswort sind
    private List<string> GetRandomDistractors(int count)
    {
        List<string> distractors = new List<string>();

        if (currentTarget.WordBank == null || currentTarget.WordBank.Words == null)
            return distractors;

        // Alle Wörter holen außer das korrekte Wort
        List<string> pool = new List<string>();
        foreach (string w in currentTarget.WordBank.Words)
        {
            if (w.Trim().ToLowerInvariant() != currentTarget.CorrectWord.Trim().ToLowerInvariant())
            {
                pool.Add(w);
            }
        }

        // Zufällig durchmischen
        for (int i = 0; i < pool.Count; i++)
        {
            string temp = pool[i];
            int rnd = Random.Range(i, pool.Count);
            pool[i] = pool[rnd];
            pool[rnd] = temp;
        }

        // Gewünschte Anzahl entnehmen
        for (int i = 0; i < count && i < pool.Count; i++)
        {
            distractors.Add(pool[i]);
        }

        return distractors;
    }

    private void CloseHintModal()
    {
        if (hintModal != null) hintModal.SetActive(false);
        inputField.ActivateInputField();
    }

    public void Close()
    {
        gameObject.SetActive(false);
        currentTarget = null;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        GameStateManager.Instance.SetState(GameState.Gameplay);
    }
}