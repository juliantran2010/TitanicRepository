using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance { get; private set; }

    // ID des Spawn-Punkts, an dem der Spieler herauskommen soll
    [SerializeField] private string TargetSpawnID;
    [SerializeField] private readonly Dictionary<string, SpawnPoint> registry = new Dictionary<string, SpawnPoint>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void RegisterSpawnPoint(SpawnPoint point)
    {
        if (!registry.ContainsKey(point.ID))
        {
            registry.Add(point.ID, point);
            //Debug.Log($"[SpawnManager] SpawnPoint '{point.ID}' registriert.");
        }

        // Sobald der gesuchte SpawnPoint geladen ist -> Spieler versetzen!
        if (point.ID == TargetSpawnID)
        {
            TeleportPlayerToPoint(point);
        }
    }

    public void UnregisterSpawnPoint(SpawnPoint point)
    {
        if (registry.ContainsKey(point.ID))
        {
            registry.Remove(point.ID);
        }
    }

    public void SetNextSpawnPoint(string spawnID)
    {
        TargetSpawnID = spawnID;
    }

    private void TeleportPlayerToPoint(SpawnPoint point)
    {
        // Greift auf die persistent existierende Player-Instanz zu
        if (PersistentPlayer.Instance == null) return;

        GameObject player = PersistentPlayer.Instance.gameObject;

        // WICHTIG: CharacterController vor dem Teleport deaktivieren,
        // da er manuelle Transform-Änderungen sonst blockieren kann!
        if (player.TryGetComponent<CharacterController>(out var cc))
        {
            cc.enabled = false;
        }

        player.transform.position = point.transform.position;
        player.transform.rotation = point.transform.rotation;

        if (cc != null)
        {
            cc.enabled = true;
        }

        //Debug.Log($"[SpawnManager] Spieler erfolgreich zu SpawnPoint '{point.ID}' versetzt.");
    }
}
