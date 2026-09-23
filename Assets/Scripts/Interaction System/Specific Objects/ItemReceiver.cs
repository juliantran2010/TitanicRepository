using System;
using System.Collections.Generic;
using UnityEngine;

public class ItemReceiver : MonoBehaviour
{
    [Serializable]
    public class ItemTransferRule
    {
        public string triggerId = "";
        public string itemName = "";
        public Transform targetSocket;

        [Header("Offset")]
        public Vector3 positionOffset = Vector3.zero;
        public Vector3 rotationOffset = Vector3.zero;

        [Header("Collider & Physik")]
        public bool enableCollider = false;
        public bool notKinematic = false;
    }

    [SerializeField] private List<ItemTransferRule> transferRules = new List<ItemTransferRule>();

    private void Start()
    {
        StoryDirector.Instance.OnStoryEventTriggered += HandleStoryTrigger;
    }

    private void OnDestroy()
    {
        StoryDirector.Instance.OnStoryEventTriggered -= HandleStoryTrigger;
    }

    private void HandleStoryTrigger(string triggerName)
    {
        foreach (var rule in transferRules)
        {
            if (rule.triggerId != triggerName) continue;

            TransferItem(rule);
        }
    }

    private void TransferItem(ItemTransferRule rule)
    {
        if (Inventory.Instance == null) return;

        PickupObject item = Inventory.Instance.GetItem(rule.itemName);
        if (item == null)
        {
            Debug.LogWarning($"[ItemReceiver] Item '{rule.itemName}' nicht im Inventar gefunden!");
            return;
        }

        // Ziel-Socket bestimmen (Fallback: dieses Transform)
        Transform target = rule.targetSocket != null ? rule.targetSocket : transform;
        item.PutDown(target, rule.positionOffset, rule.rotationOffset, () =>
        {
            //falls Dialog pausiert wurde
            if (DialogueManager.Instance.waitingForTriggerToFinish == "put_down_" + rule.itemName)
                DialogueManager.Instance.ResumeDialogue();
        });

        // 3. Optionaler Rotations-Offset
        if (rule.rotationOffset != Vector3.zero)
        {
            item.transform.localRotation = Quaternion.Euler(rule.rotationOffset);
        }

        // 4. Physik/Collider absichern
        if (!rule.enableCollider && item.TryGetComponent<Collider>(out var col))
        {
            col.enabled = false;
        }

        if (!rule.notKinematic && item.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = true;
        }
    }
}