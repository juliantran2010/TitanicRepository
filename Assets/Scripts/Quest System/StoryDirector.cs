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
            ExecuteCurrentBeat();
        }
    }

    private void ExecuteCurrentBeat()
    {
        if (activeScript == null || currentIndex >= activeScript.beats.Count)
        {
            Debug.Log($"[StoryDirector] Drehbuch beendet (Index: {currentIndex}).");
            GameStateManager.Instance?.SetState(GameState.Gameplay);
            return;
        }

        StoryBeat beat = activeScript.beats[currentIndex];
        Debug.Log($"[StoryDirector] Starte Beat [{currentIndex}]: '{beat.beatName}'");

        GameStateManager.Instance?.SetState(GameState.Gameplay);

        // Zaehlen, welche Bedingungen ausgefuellt sind
        bool hasSceneReq = !string.IsNullOrEmpty(beat.waitForSceneName);
        bool hasQuestReq = !string.IsNullOrEmpty(beat.waitForQuestId);
        bool hasCustomReq = !string.IsNullOrEmpty(beat.customEventName);

        // Status der einzelnen Bedingungen
        bool sceneDone = !hasSceneReq;
        bool questDone = !hasQuestReq;
        bool customDone = !hasCustomReq;

        // Falls bereits in der geforderten Szene
        if (hasSceneReq && SceneManager.GetActiveScene().name == beat.waitForSceneName)
        {
            sceneDone = true;
        }

        // Lokale Hilfsmethode: Prueft, ob jetzt alles erfuellt ist
        void CheckAllConditions()
        {
            if (sceneDone && questDone && customDone)
            {
                OnTriggerFired(beat);
            }
        }

        // Wenn von vornherein gar nichts eingetragen ist (oder Szene schon passt):
        if (sceneDone && questDone && customDone)
        {
            OnTriggerFired(beat);
            return;
        }

        // --- Listener fuer Szene registrieren ---
        if (!sceneDone)
        {
            void SceneHandler(string loadedScene)
            {
                if (loadedScene == beat.waitForSceneName)
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

    private void OnTriggerFired(StoryBeat beat)
    {
        Debug.Log($"[StoryDirector] Alle Bedingungen erfuellt fuer Beat: '{beat.beatName}'");

        if (beat.dialogueToPlay != null)
        {
            GameStateManager.Instance?.SetState(beat.stateDuringDialogue);

            if (DialogueManager.Instance == null)
            {
                Debug.LogError("[StoryDirector] DialogueManager.Instance ist NULL!");
                AssignQuestIfAny(beat);
                AdvanceBeat();
                return;
            }

            DialogueManager.Instance.StartDialogue(beat.dialogueToPlay, (story) =>
            {
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
        ExecuteCurrentBeat();
    }
}