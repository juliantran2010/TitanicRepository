using DG.Tweening;
using NUnit.Framework.Interfaces;
using System;
using UnityEngine;

public class PickupObject : DialogueObject
{
    public override InteractionType Type => InteractionType.Pickup;

    protected override void OnInteract()
    {
        base.OnInteract();
        bool wasAdded = Inventory.Instance.AddItem(this);
        if (wasAdded)
        {
            transform.SetParent(Inventory.Instance.transform);
            AttachToCameraField attachScript = gameObject.AddComponent<AttachToCameraField>();
            SetPersistentStateValue("is_picked_up", true);
        }
    }

    protected override void OnStateRestored()
    {
        base.OnStateRestored();
        if (GetPersistentStateValue<bool>("is_picked_up"))
        {
            Destroy(gameObject);
        }
    }
    
    public void PutDown(Transform targetTransform, Vector3? positionOffset, Vector3? rotationOffset, Action callback = null)
    {
        Vector3 pos = targetTransform.position;
        if (positionOffset != null)
            pos = pos + (Vector3) positionOffset;

        float animDuration = 0.5f;
        AttachToCameraField attachScript = GetComponent<AttachToCameraField>();
        if (attachScript != null) attachScript.enabled = false;
        transform.SetParent(targetTransform, true);
        transform.DOMove(pos, animDuration).OnComplete(() =>
        {
            Inventory.Instance.RemoveItem(this, false);
            callback?.Invoke();
        });
        if (rotationOffset != null)
            transform.DOBlendableRotateBy((Vector3)rotationOffset, animDuration);
    }
}
