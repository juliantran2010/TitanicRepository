using System;
using UnityEngine;

[System.Serializable]
public struct DialogueLine
{
    public string name;
    [TextArea(3, 10)]
    public string sentence;
}

[System.Serializable]
public class Dialogue
{
    public DialogueLine[] dialogueLines;
}
