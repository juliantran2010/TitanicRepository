using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StoryDirector : MonoBehaviour
{
    public static StoryDirector Instance { get; private set; }

    public Action<string> OnStoryEventTriggered;

    [Header("Drehbuch")]
    [SerializeField] private StoryScript activeScript;
    [SerializeField] private int startAtIndex = 0;

    [Header("Testing & Debug")]
    [Tooltip("Wenn aktiv, werden fuer den 'startAtIndex'-Beat alle Warte-Bedingungen (Szene/Quest/Event) ignoriert und er startet sofort.")]
    [SerializeField] private bool ignoreConditionsOnStart = false;

    private int currentIndex = 0;
    private bool isInitialized = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private IEnumerator Start()
    {
        if (Instance != this) yield break;

        yield return null; // Warten auf alle Initialisierungen der Startszene

        if (!isInitialized)
        {
            isInitialized = true;
            currentIndex = startAtIndex;
            ExecuteCurrentBeat(ignoreConditionsOnStart);
        }
    }

    public void TriggerEvent(string eventName)
    {
        Debug.Log($"[StoryDirector] TriggerEvent: '{eventName}'");
        OnStoryEventTriggered?.Invoke(eventName);
    }

    private void ExecuteCurrentBeat(bool bypassConditions = false)
    {
        if (activeScript == null || currentIndex >= activeScript.beats.Count)
        {
            Debug.Log($"[StoryDirector] Drehbuch beendet (Index: {currentIndex}).");
            GameStateManager.Instance?.SetState(GameState.Gameplay);
            return;
        }

        StoryBeat beat = activeScript.beats[currentIndex];

        // --- Neu: Pruefen, ob der Beat temporaer deaktiviert ist ---
        if (beat.isDisabled)
        {
            Debug.Log($"[StoryDirector] Beat [{currentIndex}]: '{beat.beatName}' ist DEAKTIVIERT -> wird uebersprungen.");
            AdvanceBeat();
            return;
        }

        Debug.Log($"[StoryDirector] Starte Beat [{currentIndex}]: '{beat.beatName}' (Bypass: {bypassConditions})");

        // Wenn Bedingungen erzwungenermassen uebersprungen werden sollen (z.B. beim Test-Start)
        if (bypassConditions)
        {
            StartCoroutine(FireBeatRoutine(beat));
            return;
        }

        // Zaehlen, welche Bedingungen ausgefuellt sind
        bool hasSceneReq = !string.IsNullOrEmpty(beat.waitForSceneName);
        bool hasQuestReq = !string.IsNullOrEmpty(beat.waitForQuestId);
        bool hasCustomReq = !string.IsNullOrEmpty(beat.customEventName);

        // Status der einzelnen Bedingungen
        bool sceneDone = !hasSceneReq;
        bool questDone = !hasQuestReq;
        bool customDone = !hasCustomReq;

        if (hasSceneReq && SceneManager.GetActiveScene().name == beat.waitForSceneName)
        {
            sceneDone = true;
        }

        void CheckAllConditions()
        {
            if (sceneDone && questDone && customDone)
            {
                StartCoroutine(FireBeatRoutine(beat));
            }
        }

        // Falls von vornherein erfuellt
        if (sceneDone && questDone && customDone)
        {
            StartCoroutine(FireBeatRoutine(beat));
            return;
        }

        // --- Listener fuer Szene registrieren ---
        if (!sceneDone)
        {
            void SceneHandler(string sceneName, string spawnPointId)
            {
                if (sceneName == beat.waitForSceneName)
                {
                    GameSceneManager.Instance.OnSceneChanged -= SceneHandler;
                    StartCoroutine(WaitSceneAndMarkDone(() =>
                    {
                        sceneDone = true;
                        CheckAllConditions();
                    }));
                }
            }
            GameSceneManager.Instance.OnSceneChanged += SceneHandler;
        }

        // --- Listener fuer Quest registrieren ---
        if (!questDone)
        {
            void QuestHandler(string finishedId)
            {
                if (finishedId == beat.waitForQuestId)
                {
                    QuestManager.Instance.OnQuestCompleted -= QuestHandler;
                    questDone = true;
                    CheckAllConditions();
                }
            }
            QuestManager.Instance.OnQuestCompleted += QuestHandler;
        }

        // --- Listener fuer Custom Event registrieren ---
        if (!customDone)
        {
            void CustomHandler(string eventName)
            {
                if (eventName == beat.customEventName)
                {
                    OnStoryEventTriggered -= CustomHandler;
                    customDone = true;
                    CheckAllConditions();
                }
            }
            OnStoryEventTriggered += CustomHandler;
        }
    }

    private IEnumerator WaitSceneAndMarkDone(Action onComplete)
    {
        yield return null;
        yield return new WaitForEndOfFrame();
        onComplete?.Invoke();
    }

    private IEnumerator FireBeatRoutine(StoryBeat beat)
    {
        // 1. Warten, falls der Spieler noch im Inspect ODER noch im laufenden Dialog ist!
        while (GameStateManager.Instance != null &&
              (GameStateManager.Instance.CurrentState == GameState.Inspect ||
               GameStateManager.Instance.CurrentState == GameState.Dialogue))
        {
            yield return null;
        }

        // 2. Die eingestellte Wartezeit (z.B. 2-3 Sekunden Pause nach Dialogende)
        if (beat.delayBeforeStart > 0f)
        {
            yield return new WaitForSeconds(beat.delayBeforeStart);

            // Zur Sicherheit: Hat der Spieler in den 3 Sekunden Pause erneut einen Dialog/Inspect geoeffnet?
            while (GameStateManager.Instance != null &&
                  (GameStateManager.Instance.CurrentState == GameState.Inspect ||
                   GameStateManager.Instance.CurrentState == GameState.Dialogue))
            {
                yield return null;
            }
        }

        yield return new WaitForEndOfFrame();

        Debug.Log($"[StoryDirector] Alle Bedingungen erfuellt. Starte Aktionen fuer Beat: '{beat.beatName}'");

        // Trigger Event
        if (!string.IsNullOrEmpty(beat.triggerToFire))
        {
            TriggerEvent(beat.triggerToFire);
        }

        //Run Custom StoryAction
        foreach (StoryAction action in beat.customActions)
        {
            action.Execute();
        }

        //Complete quest
        if (!string.IsNullOrEmpty(beat.questToComplete))
        {
            QuestManager.Instance.CompleteQuest(beat.questToComplete);
        }

        //Play Dialogue or Add Quest
        if (!beat.dialogueToPlay.IsEmpty())
        {
            GameStateManager.Instance?.SetState(beat.stateDuringDialogue);

            if (DialogueManager.Instance == null)
            {
                Debug.LogError("[StoryDirector] DialogueManager.Instance ist NULL!");
                AssignQuestIfAny(beat);
                AdvanceBeat();
                yield break;
            }

            DialogueManager.Instance.StartDialogue(beat.dialogueToPlay, (story) =>
            {
                Debug.Log($"[StoryDirector] Dialog beendet fuer Beat: '{beat.beatName}'. Schalte zurueck auf Gameplay.");
                GameStateManager.Instance?.SetState(GameState.Gameplay);
                AssignQuestIfAny(beat);
                AdvanceBeat();
            });
        }
        else
        {
            AssignQuestIfAny(beat);
            AdvanceBeat();
        }
    }

    private void AssignQuestIfAny(StoryBeat beat)
    {
        if (beat.questToAssign != null && !string.IsNullOrEmpty(beat.questToAssign.id))
        {
            Debug.Log($"[StoryDirector] Vergebe neue Quest: '{beat.questToAssign.id}'");
            QuestManager.Instance?.AddQuest(beat.questToAssign);
        }
    }

    private void AdvanceBeat()
    {
        currentIndex++;
        ExecuteCurrentBeat(false);
    }

    // --- Kontext-Menue Buttons im Editor Inspector ---

    [ContextMenu("Debug: Force Trigger Current Beat")]
    public void DebugForceTriggerCurrentBeat()
    {
        if (!Application.isPlaying || activeScript == null || currentIndex >= activeScript.beats.Count)
        {
            Debug.LogWarning("[StoryDirector] Erzwingen nur im PlayMode mit gueltigem Beat moeglich.");
            return;
        }

        Debug.Log($"[StoryDirector] Manuelles Ausloesen erzwungen fuer Beat Index: {currentIndex}");
        StopAllCoroutines();
        StartCoroutine(FireBeatRoutine(activeScript.beats[currentIndex]));
    }

    [ContextMenu("Debug: Skip To Next Beat")]
    public void DebugSkipToNextBeat()
    {
        if (!Application.isPlaying) return;
        Debug.Log("[StoryDirector] Ueberspringe zum naechsten Beat.");
        AdvanceBeat();
    }
}