using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class StoryAction : ScriptableObject
{
    public abstract void Execute();
}

[Serializable]
public class StoryBeat
{
    [Header("Uebersicht")]
    public string beatName;
    public bool isDisabled = false;

    [Header("Timing")]
    [Tooltip("Wartet nach Erfuellung aller Bedingungen und Beendigung von Dialogen noch X Sekunden, bevor dieser Beat startet.")]
    public float delayBeforeStart = 0f; // z. B. 2 oder 3 Sekunden Verschnaufpause

    [Header("Bedingungen / Trigger (alle ausgefuellten muessen zutreffen)")]
    public string waitForSceneName;
    public string waitForQuestId;
    public string customEventName;

    [Header("Aktionen nach Erfuellung")]
    public Dialogue dialogueToPlay;
    public GameState stateDuringDialogue = GameState.Dialogue;
    public Quest questToAssign;
    public string questToComplete;
    [Header("Spezielle Logik / Szenen-Aktionen")]
    [Tooltip("Feuert ein Event in der Szene, z. B. 'give_binoculars' oder 'lookout_look_ahead'")]
    public string triggerToFire;

    [Tooltip("Modulare Aktionen, die keine Szenenreferenz brauchen (z. B. GiveItem, PlaySound)")]
    public List<StoryAction> customActions = new List<StoryAction>();
}

[CreateAssetMenu(fileName = "NewStoryScript", menuName = "Story/Story Script")]
public class StoryScript : ScriptableObject
{
    public List<StoryBeat> beats = new List<StoryBeat>();
}