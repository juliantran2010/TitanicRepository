using UnityEngine;

public class CompanionHologramSpawner : MonoBehaviour
{
    private static CompanionHologramSpawner instance;

    [Header("Setup")]
    [Tooltip("Ziehe hier dein HologramRig-Prefab aus dem Projektfenster rein")]
    [SerializeField] private GameObject hologramRigPrefab;

    private GameObject activeHologramRig;

    private void Awake()
    {
        // Verhindert Duplikate, falls du die Startszene später erneut lädst
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        SpawnRig();
    }

    private void SpawnRig()
    {
        if (hologramRigPrefab == null)
        {
            Debug.LogError("[CompanionHologram] Kein Prefab im Inspector zugewiesen!");
            return;
        }

        activeHologramRig = Instantiate(hologramRigPrefab);
        activeHologramRig.name = "HologramRig";

        // Das instanziierte Rig bleibt ebenfalls über alle Szenenwechsel hinweg erhalten
        DontDestroyOnLoad(activeHologramRig);
    }
}