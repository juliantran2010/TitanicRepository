using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSceneManager : MonoBehaviour
{
    public static GameSceneManager Instance { get; private set; }

    [SerializeField] private CanvasGroup fadeCanvasGroup; // Optional: für Fade-In/Out Effekte
    [SerializeField] private float fadeDuration = 0.5f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void ChangeScene(string sceneName, string spawnPointID)
    {
        StartCoroutine(TransitionRoutine(sceneName, spawnPointID));
    }

    private IEnumerator TransitionRoutine(string sceneName, string spawnPointID)
    {
        // 1. Optional: Bild langsam schwarz werden lassen (Fade Out)
        yield return StartCoroutine(Fade(1f));

        // 2. Spawn-ID für den Spieler merken
        SpawnManager.Instance.SetNextSpawnPoint(spawnPointID);

        // 3. Neue Szene laden
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // 4. Optional: Bild wieder sichtbar machen (Fade In)
        yield return StartCoroutine(Fade(0f));
    }

    private IEnumerator Fade(float targetAlpha)
    {
        if (fadeCanvasGroup == null) yield break;

        float startAlpha = fadeCanvasGroup.alpha;
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
            yield return null;
        }

        fadeCanvasGroup.alpha = targetAlpha;
    }
}
