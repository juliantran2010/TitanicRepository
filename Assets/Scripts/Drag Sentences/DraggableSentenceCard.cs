using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class DraggableSentenceCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private TextMeshProUGUI letterLabel; // z. B. "A."
    [SerializeField] private TextMeshProUGUI sentenceLabel;

    public SentenceOption Data { get; private set; }
    public Transform OriginalParent { get; set; }

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Canvas rootCanvas;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        rootCanvas = GetComponentInParent<Canvas>();
    }

    public void Setup(string letter, SentenceOption optionData)
    {
        Data = optionData;
        if (letterLabel != null) letterLabel.text = letter;
        if (sentenceLabel != null) sentenceLabel.text = optionData.sentenceText;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        OriginalParent = transform.parent;

        // Nach ganz oben in die Canvas-Hierarchie hängen, damit sie über allen Elementen schwebt
        transform.SetParent(rootCanvas.transform, true);

        // BlockRaycasts auf false, damit der Drop-Slot darunter das Event empfangen kann
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.75f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (rootCanvas == null) return;
        rectTransform.anchoredPosition += eventData.delta / rootCanvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1.0f;

        // Falls die Karte im Nichts fallen gelassen wurde -> zurück zum Ursprungs-Parent
        if (transform.parent == rootCanvas.transform)
        {
            ResetToOriginalParent();
        }
    }

    public void ResetToOriginalParent()
    {
        transform.SetParent(OriginalParent, false);
    }
}