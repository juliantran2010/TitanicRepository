using GLTFast.Schema;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Door : InteractableObject
{
    [SerializeField] private string targetSceneName;
    [SerializeField] private string targetSpawnPointID;

    [SerializeField] private string destinationName;

    public override InteractionType Type => InteractionType.Teleport;

    public override string ObjectName => destinationName;

    protected override void OnInteract()
    {
        if (targetSpawnPointID != "")
            SpawnManager.Instance.SetNextSpawnPoint(targetSpawnPointID);
        if (targetSceneName != "")
            GameSceneManager.Instance.ChangeScene(targetSceneName, targetSpawnPointID);
    }
}
