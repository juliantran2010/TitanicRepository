using TMPro;
using UnityEngine;

public class Note : InspectableObject
{
    [SerializeField] private TextMeshProUGUI textBox;
    private string initialText;

    protected override void Start()
    {
        base.Start();
        initialText = textBox.text;
    }

    protected override void OnGlobalVariableSet(string varName, object value)
    {
        base.OnGlobalVariableSet(varName, value);
        textBox.text = CanInteract() ? initialText : "";
    }
}
