using DG.Tweening;
using UnityEngine;

public class AttachToCameraField : MonoBehaviour
{
    [Header("Position im Sichtfeld (0 bis 1)")]
    [Tooltip("X: 0 = ganz links, 1 = ganz rechts | Y: 0 = unten, 1 = oben | Z = Abstand in Metern vor der Kamera")]
    [SerializeField] private Vector3 viewportPosition = new Vector3(0.9f, 0.1f, 0.5f); // Rechter Rand, unten, 50cm vor Linse

    [Header("Animation")]
    [SerializeField] private float animDuration = 0.5f; //Dauer des Flugs ins UI
    [SerializeField] private Ease animEase = Ease.InOutCubic;
    [SerializeField] private float rotationSpeed = 25f;
    [SerializeField] private Vector3 targetScale = new Vector3(0.75f, 0.75f, 0.75f); // Macht das Objekt passend klein
    
    private Camera mainCam;
    private float currentYaw = 0f; // Speichert den aktuellen Drehwinkel
    private bool isFollowing = false;
    private Sequence pickupSequence;


    private void OnEnable()
    {
        mainCam = Camera.main;

        // Collider ausschalten, damit man nicht mehr am Item hängen bleibt
        if (TryGetComponent<Collider>(out var col)) col.enabled = false;

        StartPickupAnimation();
    }

    private void OnDisable()
    {
        pickupSequence?.Kill();
        transform.DOKill();
        isFollowing = false;
    }

    private void StartPickupAnimation()
    {
        // Vorherige Tweens sicher abbrechen
        pickupSequence?.Kill();
        transform.DOKill();
        isFollowing = false;

        // Lokale Zielposition relativ zur Kamera berechnen
        Vector3 targetWorldPos = mainCam.ViewportToWorldPoint(viewportPosition);
        Vector3 targetLocalPos = mainCam.transform.InverseTransformPoint(targetWorldPos);

        pickupSequence = DOTween.Sequence();

        // Über DOTween.To jeden Frame die Zielkoordinate an die aktuelle Kameraposition anpassen
        Vector3 startPos = transform.position;
        pickupSequence.Join(DOTween.To(() => 0f, t =>
        {
            // Verhindert das Verfehlen bei Kamerabewegung: Interpoliert flüssig zur *aktuellen* Kameraposition
            Vector3 currentTarget = mainCam.ViewportToWorldPoint(viewportPosition);
            transform.position = Vector3.Lerp(startPos, currentTarget, t);
        }, 1f, animDuration).SetEase(animEase));

        pickupSequence.Join(transform.DORotateQuaternion(mainCam.transform.rotation, animDuration).SetEase(animEase));
        pickupSequence.Join(transform.DOScale(targetScale, animDuration).SetEase(animEase));

        pickupSequence.OnComplete(() =>
        {
            // Initialer Winkel für nahtlosen Übergang ins LateUpdate
            currentYaw = 0f;
            isFollowing = true;
        });
    }

    private void LateUpdate()
    {
        if (mainCam == null || !isFollowing) return;

        // 1. Position im Sichtfeld berechnen
        Vector3 targetWorldPos = mainCam.ViewportToWorldPoint(viewportPosition);
        transform.position = targetWorldPos;

        // 2. Drehwinkel jeden Frame kontinuierlich erhöhen
        currentYaw += rotationSpeed * Time.deltaTime;

        // 3. Kamera-Rotation nehmen UND die eigene Drehung auf der Y-Achse hinzufügen
        transform.rotation = mainCam.transform.rotation * Quaternion.Euler(0f, currentYaw, 0f);
    }

    // Damit kannst du die Position für verschiedene Items anpassen (z. B. Versatz im Slot)
    public void SetViewportPosition(Vector3 newViewportPos)
    {
        viewportPosition = newViewportPos;
    }
}
