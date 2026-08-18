using UnityEngine;

public class AttachToCameraField : MonoBehaviour
{
    [Header("Position im Sichtfeld (0 bis 1)")]
    [Tooltip("X: 0 = ganz links, 1 = ganz rechts | Y: 0 = unten, 1 = oben | Z = Abstand in Metern vor der Kamera")]
    [SerializeField] private Vector3 viewportPosition = new Vector3(0.9f, 0.1f, 0.5f); // Rechter Rand, unten, 50cm vor Linse

    [Header("Animation")]
    [SerializeField] private float rotationSpeed = 25f;
    [SerializeField] private Vector3 targetScale = new Vector3(0.75f, 0.75f, 0.75f); // Macht das Objekt passend klein
    private float currentYaw = 0f; // Speichert den aktuellen Drehwinkel

    private Camera mainCam;

    private void Start()
    {
        mainCam = Camera.main;

        // Collider ausschalten, damit man nicht mehr am Item hängen bleibt
        if (TryGetComponent<Collider>(out var col)) col.enabled = false;

        // Objekt auf UI-Größe verkleinern
        transform.localScale = targetScale;
    }

    private void LateUpdate()
    {
        if (mainCam == null) return;

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
