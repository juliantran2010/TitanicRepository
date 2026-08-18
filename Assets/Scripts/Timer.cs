using TMPro;
using UnityEngine;

public class Timer : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private float timeRemaining = 10* 60f; // 10 minutes
    [SerializeField] private bool isTimerRunning = true;
    public bool IsTimerRunning => isTimerRunning;


    private void Update()
    {
        if (!isTimerRunning) return;
        if (timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
            UpdateTimerUI(timeRemaining);
        }
        else
        {
            timeRemaining = 0;
            isTimerRunning = false;
            UpdateTimerUI(0);
        }
    }

    private void UpdateTimerUI(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60);
        int seconds = Mathf.FloorToInt(time % 60);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds) + " min. remaining";
    }

    public void StartTimer() => isTimerRunning = true;
    public void PauseTimer() => isTimerRunning = false;
    public void ResetTimer(float newTime)
    {
        timeRemaining = newTime;
        isTimerRunning = false;
        UpdateTimerUI(timeRemaining);
    }
}
