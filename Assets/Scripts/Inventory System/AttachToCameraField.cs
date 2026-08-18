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


    private void Start()
    {
        mainCam = Camera.main;

        // Collider ausschalten, damit man nicht mehr am Item hängen bleibt
        if (TryGetComponent<Collider>(out var col)) col.enabled = false;

        StartPickupAnimation();
    }

    private void StartPickupAnimation()
    {
        if (mainCam == null) mainCam = Camera.main;

        isFollowing = false; // Deaktiviert das sprunghafte LateUpdate während des Tweens

        // 1. Zielposition und -rotation berechnen
        Vector3 targetWorldPos = mainCam.ViewportToWorldPoint(viewportPosition);
        Quaternion targetRotation = mainCam.transform.rotation;

        // 2. DOTween-Animationen starten
        transform.DOMove(targetWorldPos, animDuration).SetEase(animEase);
        transform.DORotateQuaternion(targetRotation, animDuration).SetEase(animEase);

        // 3. Skalieren und am Ende LateUpdate aktivieren
        transform.DOScale(targetScale, animDuration)
            .SetEase(animEase)
            .OnComplete(() =>
            {
                // Sobald DOTween fertig ist, übernimmt LateUpdate!
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
