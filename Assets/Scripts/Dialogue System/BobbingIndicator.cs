using UnityEngine;
using DG.Tweening;

public class BobbingIndicator : MonoBehaviour
{
    [Tooltip("Richtung und Auslenkung (z. B. (0, 0.1, 0) für 10 cm nach oben)")]
    [SerializeField] private Vector3 moveOffset = new Vector3(0f, 0.1f, 0f);

    [Tooltip("Dauer für einen halben Zyklus (Hinweg)")]
    [SerializeField] private float cycleTime = 0.6f;

    [Tooltip("Soll sich die Verschiebung an der Ausrichtung des Objekts (lokal) oder an der Welt ausrichten?")]
    [SerializeField] private bool useWorldSpace = false;

    private Tween bobTween;

    private void Start()
    {
        // Prüfen, ob wir auf einem Screen-Space Canvas liegen (klassisches 2D Screen-UI)
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        bool isScreenSpaceUI = parentCanvas != null && parentCanvas.renderMode != RenderMode.WorldSpace;

        if (isScreenSpaceUI && TryGetComponent<RectTransform>(out var rt))
        {
            // Reines Screen-UI (anchoredPosition)
            Vector2 targetPos = rt.anchoredPosition + (Vector2)moveOffset;
            bobTween = rt.DOAnchorPos(targetPos, cycleTime)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true);
        }
        else
        {
            // 3D-Objekte, 3D TextMeshPro, World-Space Canvas:
            // Hier greifen wir immer auf die echten 3D-Koordinaten zu.
            if (useWorldSpace)
            {
                Vector3 targetPos = transform.position + moveOffset;
                bobTween = transform.DOMove(targetPos, cycleTime)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetUpdate(true);
            }
            else
            {
                Vector3 targetPos = transform.localPosition + moveOffset;
                bobTween = transform.DOLocalMove(targetPos, cycleTime)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetUpdate(true);
            }
        }
    }

    private void OnDestroy()
    {
        bobTween?.Kill();
    }
}