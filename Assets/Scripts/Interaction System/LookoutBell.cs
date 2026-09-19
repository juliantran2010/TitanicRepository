using UnityEngine;

public class LookoutBell : InteractableObject
{
    public override InteractionType Type => InteractionType.Use;

    private int bellRungCount = 0;
    protected override void OnInteract()
    {
        Debug.Log("Lookout Bell rung!");
        bellRungCount++;
        if (bellRungCount >= 3)
        {
            QuestManager.Instance.CompleteQuest("ring_bell");
            QuestManager.Instance.SetVariable("rung_bell", true);
            QuestManager.Instance.AddQuest("talk_to_hogg", "Talk to Hogg about the bell ringing.");
            GameSceneManager.Instance.ChangeScene("TitanicScene", "titanic_deck");
        }
    }
}
