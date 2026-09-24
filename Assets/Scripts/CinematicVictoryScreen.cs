using System.Collections;
using UnityEngine;
using TMPro;
using DG.Tweening;

public class CinematicVictoryScreen : MonoBehaviour
{
    [Header("Cinematic Balken (Optional)")]
    [SerializeField] private RectTransform topBar;
    [SerializeField] private RectTransform bottomBar;
    [SerializeField] private float barHeight = 80f;

    [Header("Textelemente")]
    [SerializeField] private CanvasGroup textContainerGroup;
    [SerializeField] private TextMeshProUGUI subtitleText;    // z. B. "K O L L I S I O N   V E R M I E D E N"
    [SerializeField] private TextMeshProUGUI mainTitleText;   // z. B. "G E W O N N E N"
    [SerializeField] private RectTransform separatorLine;     // Feine Trennlinie

    [Header("Buttons (Unten)")]
    [SerializeField] private CanvasGroup buttonsGroup;

    private void Awake()
    {
        // Zu Spielbeginn alles unsichtbar
        if (textContainerGroup != null) textContainerGroup.alpha = 0f;
        if (buttonsGroup != null)
        {
            buttonsGroup.alpha = 0f;
            buttonsGroup.interactable = false;
            buttonsGroup.blocksRaycasts = false;
        }

        if (topBar != null) topBar.sizeDelta = new Vector2(topBar.sizeDelta.x, 0f);
        if (bottomBar != null) bottomBar.sizeDelta = new Vector2(bottomBar.sizeDelta.x, 0f);
        if (separatorLine != null) separatorLine.localScale = new Vector3(0f, 1f, 1f);
    }

    private void Start()
    {
        StoryDirector.Instance.OnStoryEventTriggered += HandleStoryTrigger;
    }

    private void OnDestroy()
    {
        StoryDirector.Instance.OnStoryEventTriggered -= HandleStoryTrigger;
    }

    private void HandleStoryTrigger(string triggerName)
    {
        if (triggerName == "victory_screen")
        {
            TriggerCinematicVictory();
        }
    }

    [ContextMenu("Test Victory In-Game")]
    public void TriggerCinematicVictory()
    {
        StartCoroutine(ShowScreenSequence());
    }

    private IEnumerator ShowScreenSequence()
    {
        GetComponent<CanvasGroup>().alpha = 1f;
        // 1. Kinobalken fahren weich von oben und unten ins Bild
        if (topBar != null) topBar.DOSizeDelta(new Vector2(topBar.sizeDelta.x, barHeight), 1.2f).SetEase(Ease.OutCubic);
        if (bottomBar != null) bottomBar.DOSizeDelta(new Vector2(bottomBar.sizeDelta.x, barHeight), 1.2f).SetEase(Ease.OutCubic);

        yield return new WaitForSeconds(0.8f);

        // 2. Kleiner Untertitel fadet ganz dezent ein
        if (subtitleText != null)
        {
            subtitleText.alpha = 0f;
            subtitleText.DOFade(0.8f, 1.5f);
        }

        yield return new WaitForSeconds(0.6f);

        // 3. Haupttitel ("GEWONNEN") fadet ein
        if (mainTitleText != null)
        {
            mainTitleText.alpha = 0f;
            mainTitleText.DOFade(1f, 1.8f);
        }

        // 4. Feine Trennlinie zieht sich langsam in der Mitte auf
        if (separatorLine != null)
        {
            separatorLine.DOScaleX(1f, 1.4f).SetEase(Ease.OutSine);
        }

        if (textContainerGroup != null)
        {
            textContainerGroup.DOFade(1f, 1.0f);
        }

        // 5. Erst nach ein paar Sekunden Stille und Schiffsblick die Buttons anbieten
        yield return new WaitForSeconds(2.0f);

        if (buttonsGroup != null)
        {
            buttonsGroup.DOFade(1f, 1.2f).OnComplete(() =>
            {
                buttonsGroup.interactable = true;
                buttonsGroup.blocksRaycasts = true;
            });
        }
    }
}