using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class StoryBeat
{
    [Header("Uebersicht")]
    public string beatName;

    [Header("Bedingungen / Trigger (alle ausgefuellten muessen zutreffen)")]
    [Tooltip("Wenn ausgefuellt: Spieler muss sich in dieser Szene befinden/sie betreten.")]
    public string waitForSceneName;

    [Tooltip("Wenn ausgefuellt: Wartet, bis diese Quest-ID abgeschlossen wurde.")]
    public string waitForQuestId;

    [Tooltip("Wenn ausgefuellt: Wartet auf diesen StoryDirector.TriggerEvent(\"...\") Code.")]
    public string customEventName;

    [Header("Aktionen nach Erfuellung")]
    [Tooltip("Dialog, der abgespielt wird, sobald alle Bedingungen erfuellt sind.")]
    public Dialogue dialogueToPlay;
    public GameState stateDuringDialogue = GameState.Dialogue;

    [Tooltip("Neue Quest, die nach dem Dialog (oder direkt) aktiv wird.")]
    public Quest questToAssign;
}

[CreateAssetMenu(fileName = "NewStoryScript", menuName = "Story/Story Script")]
public class StoryScript : ScriptableObject
{
    public List<StoryBeat> beats = new List<StoryBeat>();
}