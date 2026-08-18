using GLTFast.Schema;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Door : MonoBehaviour, IInteractable
{
    [SerializeField] private string targetSceneName;
    [SerializeField] private string targetSpawnPointID;

    public InteractionType Type => InteractionType.Teleport;

    [SerializeField] private string destinationName;
    public string ObjectName => destinationName;

    public void Interact()
    {
        if (targetSpawnPointID != "")
            SpawnManager.Instance.SetNextSpawnPoint(targetSpawnPointID);
        if (targetSceneName != "")
            GameSceneManager.Instance.ChangeScene(targetSceneName, targetSpawnPointID);
    }
}
