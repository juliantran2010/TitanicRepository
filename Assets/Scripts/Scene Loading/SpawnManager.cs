using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance { get; private set; }

    [SerializeField] private string TargetSpawnID;
    [SerializeField] private readonly Dictionary<string, SpawnPoint> registry = new Dictionary<string, SpawnPoint>();

    [Header("Ground Snapping")]
    [Tooltip("Layer, die als Boden erkannt werden sollen")]
    [SerializeField] private LayerMask groundLayer = ~0; // Standard: Alles
    [Tooltip("Wie weit über dem SpawnPoint nach oben geschaut wird")]
    [SerializeField] private float raycastUpOffset = 0f;
    [Tooltip("Wie weit nach unten der Strahl sucht")]
    [SerializeField] private float raycastDistance = 5.0f;

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
        }

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
        if (PersistentPlayer.Instance == null) return;

        GameObject player = PersistentPlayer.Instance.gameObject;

        if (player.TryGetComponent<CharacterController>(out var cc))
        {
            cc.enabled = false;
        }

        Vector3 spawnPos = point.Position;

        // Strahl startet etwas über dem gesetzten Punkt und feuert nach unten
        Vector3 rayStart = spawnPos + Vector3.up * raycastUpOffset;
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, raycastDistance, groundLayer))
        {
            if (cc != null)
            {
                // Setzt die Füße der Kapsel exakt auf den Treffpunkt des Bodens
                float feetOffset = (cc.height * 0.5f) - cc.center.y;
                spawnPos = hit.point + Vector3.up * (feetOffset + 0.02f);
            }
            else
            {
                spawnPos = hit.point + Vector3.up * 0.02f;
            }
        }

        player.transform.position = spawnPos;
        player.transform.rotation = point.Rotation;

        if (cc != null)
        {
            cc.enabled = true;
        }
    }
}