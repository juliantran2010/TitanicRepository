using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class Cupboard : DialogueObject
{
    public override string DisplayName => "Cupboard";
    private InteractionType type = InteractionType.Use;
    public override InteractionType Type => type;

    protected override void OnInkTrigger(string triggerName)
    {
        if (triggerName != "open_cupboard") return;
        //Open Door
        Inventory.Instance.RemoveItem("cupboard_key");
        gameObject.transform.DORotate(new Vector3(0, -155, 0), 1f);
        GetComponent<BoxCollider>().enabled = false; // Disable further interaction after opening the cupboard
        SetPersistentStateValue("is_opened", true);
    }

    protected override void OnStateRestored()
    {
        base.OnStateRestored();
        if (GetPersistentStateValue<bool>("is_opened"))
        {
            gameObject.transform.Rotate(new Vector3(0, -155, 0));
            GetComponent<BoxCollider>().enabled = false;
        }
    }
}
