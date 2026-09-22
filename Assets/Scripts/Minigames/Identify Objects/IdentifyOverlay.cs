using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class IdentifyOverlay : MonoBehaviour
{
    public static IdentifyOverlay Instance { get; private set; }

    [Header("Input Panel")]
    [SerializeField] private GameObject inputPanel;
    [SerializeField] private CanvasGroup inputPanelCanvasGroup;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button submitButton;
    [SerializeField] private Button overlayCloseButton;
    [SerializeField] private GameObject incorrectBadge;
    [SerializeField] private GameObject correctBadge;

    [Header("Hint Trigger Button")]
    [SerializeField] private Button needHintButton;

    [Header("Hint Window / Modal")]
    [SerializeField] private GameObject hintModal;
    [SerializeField] private CanvasGroup hintModalCanvasGroup;
    [SerializeField] private Button hintCloseButton;
    [SerializeField] private TextMeshProUGUI hintTitle;
    [SerializeField] private Transform wordGridParent;
    [SerializeField] private GridLayoutGroup wordGridLayout;
    [SerializeField] private GameObject wordButtonPrefab;

    [Header("Animation Settings")]
    [SerializeField] private float animDuration = 0.25f;
    [SerializeField] private float inputPanelFadedAlpha = 0.15f; // Transparenz, wenn Hint offen ist

    private CanvasGroup mainCanvasGroup;
    private RectTransform overlayRect;
    private RectTransform hintModalRect;

    private IdentifiableObject currentTarget;
    private int currentHintStage = 0;
    private Tween delayedCloseTween;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        mainCanvasGroup = GetComponent<CanvasGroup>();
        overlayRect = GetComponent<RectTransform>();

        if (inputPanel != null && inputPanelCanvasGroup == null)
        {
            inputPanelCanvasGroup = inputPanel.GetComponent<CanvasGroup>();
            if (inputPanelCanvasGroup == null) inputPanelCanvasGroup = inputPanel.AddComponent<CanvasGroup>();
        }

        if (hintModal != null)
        {
            hintModalRect = hintModal.GetComponent<RectTransform>();
            if (hintModalCanvasGroup == null)
            {
                hintModalCanvasGroup = hintModal.GetComponent<CanvasGroup>();
                if (hintModalCanvasGroup == null) hintModalCanvasGroup = hintModal.AddComponent<CanvasGroup>();
            }
        }

        if (wordGridLayout == null && wordGridParent != null)
        {
            wordGridLayout = wordGridParent.GetComponent<GridLayoutGroup>();
        }

        // Listener
        inputField.onSubmit.AddListener(SubmitAnswer);
        if (submitButton != null)
        {
            submitButton.onClick.AddListener(() => SubmitAnswer(inputField.text));
        }

        if (overlayCloseButton != null)
        {
            overlayCloseButton.onClick.AddListener(Close);
        }

        if (hintCloseButton != null)
        {
            hintCloseButton.onClick.AddListener(CloseHintModal);
        }

        needHintButton.onClick.AddListener(OnNeedHintClicked);

        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        KillAllTweens();
    }

    private void Update()
    {
        if (!gameObject.activeSelf) return;

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
        KillAllTweens();

        GameStateManager.Instance.SetState(GameState.Inspect);
        currentTarget = target;
        currentHintStage = 0;

        gameObject.SetActive(true);
        inputPanel.SetActive(true);

        // Input Panel voll sichtbar & interaktiv schalten
        SetInputPanelDimmed(false, instant: true);

        if (hintModal != null) hintModal.SetActive(false);

        incorrectBadge.SetActive(false);
        correctBadge.SetActive(false);
        needHintButton.gameObject.SetActive(false);

        inputField.text = "";
        inputField.interactable = true;
        if (submitButton != null) submitButton.interactable = true;

        inputField.ActivateInputField();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Einblend-Animation
        mainCanvasGroup.alpha = 0f;
        mainCanvasGroup.DOFade(1f, animDuration).SetEase(Ease.OutQuad);

        overlayRect.localScale = Vector3.one * 0.9f;
        overlayRect.DOScale(1f, animDuration).SetEase(Ease.OutBack);
    }

    public void SubmitAnswer(string input)
    {
        if (currentTarget == null || string.IsNullOrWhiteSpace(input)) return;

        if (currentTarget.CheckAnswer(input))
        {
            incorrectBadge.SetActive(false);
            correctBadge.SetActive(true);
            correctBadge.transform.DOPunchScale(Vector3.one * 0.3f, 0.4f, 10, 1);

            needHintButton.gameObject.SetActive(false);
            inputField.interactable = false;
            if (submitButton != null) submitButton.interactable = false;

            currentTarget.MarkAsCompleted();

            delayedCloseTween?.Kill();
            delayedCloseTween = DOVirtual.DelayedCall(1.2f, Close);
        }
        else
        {
            incorrectBadge.SetActive(true);
            incorrectBadge.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f);

            // Shakt das Eingabefeld horizontal hin und her
            inputField.transform.DOComplete();
            inputField.transform.DOShakePosition(0.35f, strength: new Vector3(12f, 0f, 0f), vibrato: 18);

            needHintButton.gameObject.SetActive(true);
            needHintButton.transform.DOComplete();
            needHintButton.transform.DOPunchScale(Vector3.one * 0.15f, 0.3f);
        }
    }

    private void OnNeedHintClicked()
    {
        currentHintStage++;

        // Input-Panel im Hintergrund ausblenden / transparent machen
        SetInputPanelDimmed(true);

        hintModal.SetActive(true);
        if (hintModalCanvasGroup != null)
        {
            hintModalCanvasGroup.alpha = 0f;
            hintModalCanvasGroup.DOFade(1f, animDuration);
        }
        if (hintModalRect != null)
        {
            hintModalRect.localScale = Vector3.one * 0.85f;
            hintModalRect.DOScale(1f, animDuration).SetEase(Ease.OutBack);
        }

        // Alte Buttons entfernen
        for (int i = wordGridParent.childCount - 1; i >= 0; i--)
        {
            Transform child = wordGridParent.GetChild(i);
            child.SetParent(null);
            Destroy(child.gameObject);
        }

        List<string> options = new List<string>();

        if (currentHintStage == 1)
        {
            hintTitle.text = "Choose a word from the list:";
            if (currentTarget.WordBank != null && currentTarget.WordBank.Words != null)
            {
                options.AddRange(currentTarget.WordBank.Words);
            }
        }
        else if (currentHintStage == 2)
        {
            hintTitle.text = "Choose a word:";
            options.Add(currentTarget.CorrectWord);
            options.AddRange(GetRandomDistractors(5));
        }
        else
        {
            hintTitle.text = "Which word is correct?";
            options.Add(currentTarget.CorrectWord);
            options.AddRange(GetRandomDistractors(2));
        }

        // Shuffle
        for (int i = 0; i < options.Count; i++)
        {
            string temp = options[i];
            int rnd = Random.Range(i, options.Count);
            options[i] = options[rnd];
            options[rnd] = temp;
        }

        // Dynamisches Grid-Layout
        if (wordGridLayout != null)
        {
            Canvas.ForceUpdateCanvases();
            RectTransform gridRect = wordGridParent as RectTransform;
            float totalWidth = gridRect != null ? gridRect.rect.width : 500f;

            if (options.Count <= 3)
            {
                wordGridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                wordGridLayout.constraintCount = 1;

                float singleColWidth = totalWidth - wordGridLayout.padding.horizontal;
                wordGridLayout.cellSize = new Vector2(singleColWidth, 50f);
                wordGridLayout.spacing = new Vector2(0f, 10f);
            }
            else
            {
                int columns = options.Count > 6 ? 3 : 2;

                wordGridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                wordGridLayout.constraintCount = columns;

                float availableWidth = totalWidth - wordGridLayout.padding.horizontal - (wordGridLayout.spacing.x * (columns - 1));
                float cellWidth = availableWidth / columns;

                wordGridLayout.cellSize = new Vector2(cellWidth, 45f);
                wordGridLayout.spacing = new Vector2(10f, 10f);
            }
        }

        // Buttons generieren
        foreach (string word in options)
        {
            GameObject btn = Instantiate(wordButtonPrefab, wordGridParent);
            TextMeshProUGUI btnText = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null) btnText.text = word;

            string chosen = word;
            btn.GetComponent<Button>().onClick.AddListener(() =>
            {
                inputField.text = chosen;
                CloseHintModal();
                SubmitAnswer(chosen);
            });
        }
    }

    private void SetInputPanelDimmed(bool dimmed, bool instant = false)
    {
        if (inputPanelCanvasGroup == null) return;

        float targetAlpha = dimmed ? inputPanelFadedAlpha : 1f;
        inputPanelCanvasGroup.blocksRaycasts = !dimmed;
        inputPanelCanvasGroup.interactable = !dimmed;

        inputPanelCanvasGroup.DOKill();
        if (instant)
        {
            inputPanelCanvasGroup.alpha = targetAlpha;
        }
        else
        {
            inputPanelCanvasGroup.DOFade(targetAlpha, animDuration).SetEase(Ease.InOutQuad);
        }
    }

    private List<string> GetRandomDistractors(int count)
    {
        List<string> distractors = new List<string>();

        if (currentTarget.WordBank == null || currentTarget.WordBank.Words == null)
            return distractors;

        List<string> pool = new List<string>();
        foreach (string w in currentTarget.WordBank.Words)
        {
            if (!string.Equals(w.Trim(), currentTarget.CorrectWord.Trim(), System.StringComparison.OrdinalIgnoreCase))
            {
                pool.Add(w);
            }
        }

        for (int i = 0; i < pool.Count; i++)
        {
            string temp = pool[i];
            int rnd = Random.Range(i, pool.Count);
            pool[i] = pool[rnd];
            pool[rnd] = temp;
        }

        for (int i = 0; i < count && i < pool.Count; i++)
        {
            distractors.Add(pool[i]);
        }

        return distractors;
    }

    private void CloseHintModal()
    {
        // Hintergrundfeld wieder voll einblenden
        SetInputPanelDimmed(false);

        if (hintModalCanvasGroup != null)
        {
            hintModalCanvasGroup.DOKill();
            hintModalRect.DOKill();

            hintModalCanvasGroup.DOFade(0f, animDuration * 0.8f);
            hintModalRect.DOScale(0.9f, animDuration * 0.8f).OnComplete(() =>
            {
                if (hintModal != null) hintModal.SetActive(false);
                inputField.ActivateInputField();
            });
        }
        else
        {
            if (hintModal != null) hintModal.SetActive(false);
            inputField.ActivateInputField();
        }
    }

    public void Close()
    {
        KillAllTweens();

        // Sanft ausfaden und skalieren, dann deaktivieren
        mainCanvasGroup.DOFade(0f, animDuration * 0.8f).SetEase(Ease.InQuad);
        overlayRect.DOScale(0.9f, animDuration * 0.8f).SetEase(Ease.InQuad).OnComplete(() =>
        {
            gameObject.SetActive(false);
            currentTarget = null;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            GameStateManager.Instance.SetState(GameState.Gameplay);
        });
    }

    private void KillAllTweens()
    {
        delayedCloseTween?.Kill();
        if (mainCanvasGroup != null) mainCanvasGroup.DOKill();
        if (overlayRect != null) overlayRect.DOKill();
        if (inputPanelCanvasGroup != null) inputPanelCanvasGroup.DOKill();
        if (hintModalCanvasGroup != null) hintModalCanvasGroup.DOKill();
        if (hintModalRect != null) hintModalRect.DOKill();
    }
}