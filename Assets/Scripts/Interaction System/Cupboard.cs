using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class Cupboard : DialogueObject
{
    private InteractionType type = InteractionType.Use;
    public override InteractionType Type => type;

    public override string ObjectName => "Cupboard";

    protected override Dictionary<string, object> SetDialogueVariables()
    {
        bool hasKey = Inventory.Instance.ContainsItem("Cupboard Key");
        return new Dictionary<string, object>
        {
            { "hasKey", hasKey }
        };
    }

    protected override void OnInkTrigger(string triggerName)
    {
        if (triggerName != "open_cupboard") return;
        //Open Door
        Inventory.Instance.RemoveItem("Cupboard Key");
        gameObject.transform.DORotate(new Vector3(0, -155, 0), 1f);
        GetComponent<BoxCollider>().enabled = false; // Disable further interaction after opening the cupboard
        SetPersistentStateValue("isOpened", true);
    }

    protected override void OnStateRestored()
    {
        base.OnStateRestored();
        if (GetPersistentStateValue<bool>("isOpened"))
        {
            gameObject.transform.Rotate(new Vector3(0, -155, 0));
            GetComponent<BoxCollider>().enabled = false;
        }
    }
}
