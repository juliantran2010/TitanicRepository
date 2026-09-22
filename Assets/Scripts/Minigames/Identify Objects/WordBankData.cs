using System;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "NewWordBank", menuName = "Titanic/Word Bank")]
public class WordBankData : ScriptableObject
{
    [Header("Schnelleingabe (Kopieren & Einfügen)")]
    [Tooltip("Füge hier Wörter mit Kommas oder neuen Zeilen getrennt ein.")]
    [TextArea(4, 10)]
    [SerializeField] private string quickInput = "bunk, pillow, blanket, towel, washbasin, tap, mirror, suitcase, bed, coat, door, lamp";

    [Header("Generierte Wörterliste")]
    [SerializeField] private string[] words;

    public string[] Words => words;

    // Erzeugt einen klickbaren Menüpunkt im Inspector (Rechtsklick auf die Komponente / ScriptableObject Header)
    [ContextMenu("Wörter aus Textfeld parsen")]
    public void ParseQuickInput()
    {
        if (string.IsNullOrWhiteSpace(quickInput))
        {
            words = Array.Empty<string>();
            return;
        }

        // Trennt bei Komma, Semikolon oder Zeilenumbruch
        char[] delimiters = new char[] { ',', ';', '\n', '\r' };

        words = quickInput
            .Split(delimiters, StringSplitOptions.RemoveEmptyEntries)
            .Select(w => w.Trim().ToLowerInvariant())
            .Where(w => !string.IsNullOrEmpty(w))
            .Distinct() // Entfernt eventuelle Duplikate automatisch
            .ToArray();

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
        Debug.Log($"[WordBank] {words.Length} Wörter erfolgreich übernommen!");
    }

    // Automatisch beim Ändern des Inspektors aktualisieren
    private void OnValidate()
    {
        ParseQuickInput();
    }
}