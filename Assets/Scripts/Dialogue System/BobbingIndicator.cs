using UnityEngine;
using DG.Tweening;

public class BobbingIndicator : MonoBehaviour
{
    [SerializeField] private float moveDistance = 5f;
    [SerializeField] private float cycleTime = 0.6f;

    private void Start()
    {
        RectTransform rt = GetComponent<RectTransform>();

        // Bewegt sich unendlich um moveDistance Einheiten nach unten und wieder zurück
        rt.DOAnchorPosY(rt.anchoredPosition.y - moveDistance, cycleTime)
          .SetEase(Ease.InOutSine)
          .SetLoops(-1, LoopType.Yoyo)
          .SetUpdate(true); // Läuft auch weiter, wenn Time.timeScale pausiert ist
    }
}