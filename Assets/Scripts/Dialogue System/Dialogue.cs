using System;
using UnityEngine;

[Serializable]
public class Dialogue
{
    public TextAsset inkJSON;
    public string dialogueState = "";
    public string startPath = "";

    public bool IsEmpty()
    {
        return inkJSON == null;
    }
}
