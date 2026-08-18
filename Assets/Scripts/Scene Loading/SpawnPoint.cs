using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    [SerializeField] private string spawnPointID = "Default";
    public string ID => spawnPointID;

    private void OnEnable()
    {
        // Registriert sich beim Szenenstart selbst
        if (SpawnManager.Instance != null)
        {
            SpawnManager.Instance.RegisterSpawnPoint(this);
        }
    }

    private void OnDisable()
    {
        // Meldet sich beim Entladen der Szene sauber ab
        if (SpawnManager.Instance != null)
        {
            SpawnManager.Instance.UnregisterSpawnPoint(this);
        }
    }

    private void OnDrawGizmos()
    {
        // Visuelle Hilfe im Editor (Blickrichtung des Spawns)
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, transform.forward * 1f);
        Gizmos.DrawWireSphere(transform.position, 0.1f);
    }
}
