using GLTFast.Schema;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TeleportationObject : InteractableObject
{
    [Header("Teleportation")]
    [SerializeField] private string targetSceneName;
    [SerializeField] private string targetSpawnPointID;
    public override InteractionType Type => InteractionType.Teleport;

    protected override void OnInteract()
    {
        if (!string.IsNullOrWhiteSpace(targetSpawnPointID))
            SpawnManager.Instance.SetNextSpawnPoint(targetSpawnPointID);
        if (!string.IsNullOrWhiteSpace(targetSceneName))
            GameSceneManager.Instance.ChangeScene(targetSceneName, targetSpawnPointID);
        if (!string.IsNullOrWhiteSpace(targetSpawnPointID))
            QuestManager.Instance.CompleteQuest("reach_" + targetSpawnPointID);
    }

    protected override void OnCannotInteract()
    {
        QuestManager questManager = QuestManager.Instance;
        Debug.Log("Quest not completed: '" + neccessaryQuestIdFinished + "'");
        Quest oldest = questManager.GetOldestActiveQuest();
        string taskText = "finish my current task.";

        if (oldest != null && !string.IsNullOrEmpty(oldest.description))
        {
            string desc = oldest.description;
            taskText = char.ToLower(desc[0]) + desc.Substring(1) + ".";
        }
        DialogueManager.Instance.ShowText("I shouldn't go there yet. I first need to " + taskText);
    }

    private void OnEnable()
    {
        if (PathGuideManager.Instance != null)
        {
            PathGuideManager.Instance.RegisterTarget(targetSpawnPointID, transform);
        }
    }

    private void OnDisable()
    {
        if (PathGuideManager.Instance != null)
        {
            PathGuideManager.Instance.UnregisterTarget(targetSpawnPointID);
        }
    }
}
