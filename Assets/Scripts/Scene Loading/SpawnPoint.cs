using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    [SerializeField] private string spawnPointID = "Default";
    public string ID => spawnPointID;

    [Header("Offset Settings")]
    [Tooltip("Lokaler Positionsversatz relativ zum SpawnPoint (Z = Vorne).")]
    [SerializeField] private Vector3 localOffset = new Vector3(0f, 0f, 5f);

    [Tooltip("Zusätzlicher lokaler Rotationsversatz in Grad (z. B. Y = 180 für Blickrichtung nach hinten).")]
    [SerializeField] private Vector3 localRotationOffset = Vector3.zero;

    // Nur diese berechneten Werte sind nach außen hin sichtbar
    public Vector3 Position => transform.TransformPoint(localOffset);
    public Quaternion Rotation => transform.rotation * Quaternion.Euler(localRotationOffset);

    /// <summary>
    /// Setzt ein Transform (z. B. den Spieler) direkt an den versetzten SpawnPoint.
    /// </summary>
    public void Place(Transform target)
    {
        target.SetPositionAndRotation(Position, Rotation);
    }

    private void OnEnable()
    {
        if (SpawnManager.Instance != null)
        {
            SpawnManager.Instance.RegisterSpawnPoint(this);
        }
    }

    private void OnDisable()
    {
        if (SpawnManager.Instance != null)
        {
            SpawnManager.Instance.UnregisterSpawnPoint(this);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, 0.1f);

        Vector3 finalSpawnPos = Position;
        Quaternion finalSpawnRot = Rotation;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, finalSpawnPos);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(finalSpawnPos, 0.1f);

        Vector3 forwardDirection = finalSpawnRot * Vector3.forward;
        Gizmos.DrawRay(finalSpawnPos, forwardDirection * 0.5f);
    }
}