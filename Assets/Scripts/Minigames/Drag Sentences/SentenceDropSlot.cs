using UnityEngine;
using UnityEngine.EventSystems;

public class SentenceDropSlot : MonoBehaviour, IDropHandler
{
    [SerializeField] private int slotIndex; // 0 für FIRST, 1 für NEXT, 2 für LAST
    [SerializeField] private GameObject placeholderHint; // Text "Drag a sentence here"

    public DraggableSentenceCard CurrentCard { get; private set; }
    public int SlotIndex => slotIndex;

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return;

        if (eventData.pointerDrag.TryGetComponent<DraggableSentenceCard>(out var draggedCard))
        {
            // Falls schon eine Karte drin liegt -> die alte Karte zurücklegen
            if (CurrentCard != null && CurrentCard != draggedCard)
            {
                CurrentCard.ResetToOriginalParent();
            }

            CurrentCard = draggedCard;
            draggedCard.transform.SetParent(transform, false);
            draggedCard.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

            UpdatePlaceholder();
            SentenceMinigameController.Instance?.NotifySelectionChanged();
        }
    }

    public void ClearSlot()
    {
        CurrentCard = null;
        UpdatePlaceholder();
    }

    private void UpdatePlaceholder()
    {
        if (placeholderHint != null)
        {
            placeholderHint.SetActive(transform.childCount == 0 || (transform.childCount == 1 && transform.GetChild(0).gameObject == placeholderHint));
        }
    }

    private void Update()
    {
        // Kontinuierliche Prüfung, falls eine Karte wieder herausgezogen wurde
        bool hasCard = false;
        foreach (Transform child in transform)
        {
            if (child.GetComponent<DraggableSentenceCard>() != null)
            {
                hasCard = true;
                break;
            }
        }

        if (!hasCard)
        {
            CurrentCard = null;
        }
        UpdatePlaceholder();
    }
}