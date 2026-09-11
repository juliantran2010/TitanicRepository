using GLTFast.Schema;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Door : InteractableObject
{
    [SerializeField] private string targetSceneName;
    [SerializeField] private string targetSpawnPointID;
    public override InteractionType Type => InteractionType.Teleport;

    protected override void OnInteract()
    {
        if (targetSpawnPointID != "")
            SpawnManager.Instance.SetNextSpawnPoint(targetSpawnPointID);
        if (targetSceneName != "")
            GameSceneManager.Instance.ChangeScene(targetSceneName, targetSpawnPointID);
    }
}
