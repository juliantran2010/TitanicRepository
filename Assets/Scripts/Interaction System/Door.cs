using GLTFast.Schema;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Door : InteractableObject
{
    [SerializeField] private string targetSceneName;
    [SerializeField] private string targetSpawnPointID;
    public override InteractionType Type => InteractionType.Teleport;

    [SerializeField] private string neccessaryQuestID;

    protected override void OnInteract()
    {
        QuestManager questManager = QuestManager.Instance;
        if (!string.IsNullOrWhiteSpace(neccessaryQuestID) && !questManager.IsQuestCompleted(neccessaryQuestID.Trim()))
        {
            Debug.Log("Quest not completed: '" + neccessaryQuestID + "'");
            Quest oldest = questManager.GetOldestActiveQuest();
            string taskText = "finish my current task.";

            if (oldest != null && !string.IsNullOrEmpty(oldest.description))
            {
                string desc = oldest.description;
                taskText = char.ToLower(desc[0]) + desc.Substring(1) + ".";
            }
            DialogueManager.Instance.ShowText("I shouldn't go there yet. I first need to " + taskText);
            return;
        }
        if (!string.IsNullOrWhiteSpace(targetSpawnPointID))
            SpawnManager.Instance.SetNextSpawnPoint(targetSpawnPointID);
        if (!string.IsNullOrWhiteSpace(targetSceneName))
            GameSceneManager.Instance.ChangeScene(targetSceneName, targetSpawnPointID);
        if (!string.IsNullOrWhiteSpace(targetSpawnPointID))
            questManager.CompleteQuest("reach_" + targetSpawnPointID);
    }
}
