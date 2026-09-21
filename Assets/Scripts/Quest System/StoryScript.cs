using System;
using System.Collections.Generic;
using UnityEngine;

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
}

[CreateAssetMenu(fileName = "NewStoryScript", menuName = "Story/Story Script")]
public class StoryScript : ScriptableObject
{
    public List<StoryBeat> beats = new List<StoryBeat>();
}