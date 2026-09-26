using System.Collections.Generic;
using UnityEngine;

public class MesabaNote : InspectableObject
{
    public override string DisplayName => "Mesaba Message";

    [Header("Minigame Elements")]
    [SerializeField] private Canvas noteWorldCanvas;
    [SerializeField] private List<MesabaGap> gaps = new List<MesabaGap>();
    [SerializeField] private string TriggerOnCompletion = "mesaba_solved";


    protected override void OnOpenComplete()
    {
        base.OnOpenComplete();

        // Maus freigeben
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Minigame UI starten und lokale Gaps dynamisch an den globalen Manager übergeben
        if (MesabaMinigameManager.Instance != null)
        {
            MesabaMinigameManager.Instance.OpenMinigame(gaps, this, OnMinigameCompleted);
        }
    }

    protected override void OnCloseStarted()
    {
        base.OnCloseStarted();

        // Maus wieder sperren
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Globales Minigame-UI schließen
        if (MesabaMinigameManager.Instance != null)
        {
            MesabaMinigameManager.Instance.CloseMinigame();
        }
    }

    private void OnMinigameCompleted()
    {
        // Spezifische Logik beim Lösen (z.B. Quest-Event, Sound)
        StoryDirector.Instance.TriggerEvent(TriggerOnCompletion);
        IsSetAsInteractable = false;
    }
}