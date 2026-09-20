using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SentenceOption
{
    public string sentenceText;
    public bool isCorrect;
}

[Serializable]
public class SentenceSlotData
{
    [Tooltip("z. B. 'FIRST: What is the danger?'")]
    public string headerTitle;
    [Tooltip("Alle Auswahloptionen für diese Spalte (A, B, C, D)")]
    public List<SentenceOption> options = new List<SentenceOption>();
}

[CreateAssetMenu(fileName = "NewSentenceMinigame", menuName = "Minigames/Sentence Ordering Data")]
public class SentenceOrderingData : ScriptableObject
{
    [Header("Quest / Event")]
    public string completedQuestId = "bridge_argument_finished";

    [Header("Spalten-Konfiguration")]
    public List<SentenceSlotData> slots = new List<SentenceSlotData>();
}