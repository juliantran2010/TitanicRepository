using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class MesabaGap : MonoBehaviour
{
    [Header("Puzzle Data")]
    [Tooltip("Das richtige Wort für diesen Fleck")]
    [SerializeField] private string correctWord = "pack";

    [Tooltip("Falsche Ablenker-Optionen (z. B. 3 Stück)")]
    [SerializeField] private string[] distractors = new string[] { "open", "deep", "cold" };

    [Tooltip("Hinweistext für den Hint-Button")]
    [TextArea(2, 4)]
    [SerializeField] private string hintText = "Think about floating, dense ice masses.";

    [Header("UI Reference")]
    [Tooltip("Das Textfeld AUF dem Fleck")]
    [SerializeField] private TextMeshProUGUI gapLabel;

    public string CorrectWord => correctWord;
    public string[] Distractors => distractors;
    public string HintText => hintText;
    public string CurrentChoice { get; private set; } = "";

    public bool IsCorrect => string.Equals(CurrentChoice.Trim(), correctWord.Trim(), System.StringComparison.OrdinalIgnoreCase);

    private Button button;
    private string defaultText = "";

    private void Awake()
    {
        button = GetComponent<Button>();
        if (gapLabel == null) gapLabel = GetComponentInChildren<TextMeshProUGUI>();

        // Ursprünglichen Text sichern, der im Editor eingetragen war
        if (gapLabel != null)
        {
            defaultText = gapLabel.text;
        }
    }

    public void Setup(System.Action<MesabaGap> onClickCallback)
    {
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClickCallback?.Invoke(this));
    }

    public void SetChoice(string word)
    {
        CurrentChoice = word;
        SetDisplayText(string.IsNullOrEmpty(word) ? defaultText : word);
    }

    public void ResetGap()
    {
        CurrentChoice = "";
        SetDisplayText(defaultText);
    }

    private void SetDisplayText(string text)
    {
        if (gapLabel != null) gapLabel.text = text;
    }
}