using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class DayTimer : MonoBehaviour
{
    private const float DayDurationSeconds = 5f * 60f;

    private TMP_Text timerText;
    private GameOverController gameOverController;
    private float remainingTime;
    private bool timerFinished;

    private void Awake()
    {
        timerText = GetComponent<TMP_Text>();
        remainingTime = DayDurationSeconds;
        DisplayTime();
    }

    private void Start()
    {
        gameOverController = FindAnyObjectByType<GameOverController>();
        if (gameOverController == null)
        {
            Debug.LogError("Day Timer could not find the Game Over Controller.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        if (timerFinished)
        {
            return;
        }

        remainingTime = Mathf.Max(0f, remainingTime - Time.deltaTime);
        DisplayTime();

        if (remainingTime <= 0f)
        {
            timerFinished = true;
            gameOverController.TriggerGameOver();
        }
    }

    private void DisplayTime()
    {
        int totalSeconds = Mathf.CeilToInt(remainingTime);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        timerText.text = $"{minutes}:{seconds:00}";
    }
}
