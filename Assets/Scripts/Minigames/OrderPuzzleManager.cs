using System;
using System.Collections.Generic;
using UnityEngine;

public class SnapPointOrderManager : MonoBehaviour
{
    [Serializable]
    public class SnapRequirement
    {
        [Tooltip("Der SnapPoint, der überwacht werden soll")]
        public SnapPoint snapPoint;

        [Tooltip("Der objectName des InteractableObject/MovableObject, das hier liegen muss")]
        public string expectedObjectName;

        public bool IsSatisfied()
        {
            if (snapPoint == null || snapPoint.CurrentOccupant == null) return false;

            string currentName = snapPoint.CurrentOccupant.ObjectName;
            return string.Equals(currentName?.Trim(), expectedObjectName?.Trim(), StringComparison.OrdinalIgnoreCase);
        }
    }

    [Header("Anforderungen")]
    [SerializeField] private List<SnapRequirement> requirements = new List<SnapRequirement>();

    [Header("Story Trigger")]
    [Tooltip("Der Event-Name, der an den StoryDirector gesendet wird")]
    [SerializeField] private string triggerEventName;

    private bool isSolved = false;

    private void OnEnable()
    {
        foreach (var req in requirements)
        {
            if (req.snapPoint != null)
            {
                req.snapPoint.OnObjectPlaced += HandleObjectChanged;
                req.snapPoint.OnObjectRemoved += HandleObjectChanged;
            }
        }
    }

    private void OnDisable()
    {
        foreach (var req in requirements)
        {
            if (req.snapPoint != null)
            {
                req.snapPoint.OnObjectPlaced -= HandleObjectChanged;
                req.snapPoint.OnObjectRemoved -= HandleObjectChanged;
            }
        }
    }

    private void HandleObjectChanged(MovableObject obj)
    {
        if (isSolved) return;
        CheckAllRequirements();
    }

    public void CheckAllRequirements()
    {
        if (requirements.Count == 0) return;

        foreach (var req in requirements)
        {
            if (!req.IsSatisfied())
            {
                return;
            }
        }

        Solve();
    }

    private void Solve()
    {
        isSolved = true;

        if (!string.IsNullOrEmpty(triggerEventName))
        {
            StoryDirector.Instance?.TriggerEvent(triggerEventName);
        }
    }
}