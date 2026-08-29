using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverController : MonoBehaviour
{
    [SerializeField] private GameObject gameOverUI = null;
    [SerializeField] private TMP_Text scoreText = null;

    private Suspition suspition;
    private bool gameOverTriggered;

    private void Awake()
    {
        if (gameOverUI == null || scoreText == null)
        {
            Debug.LogError("GameOverController needs a GameOver UI object and score text.", this);
            enabled = false;
            return;
        }

        gameOverUI.SetActive(false);
    }

    private void Start()
    {
        suspition = Suspition.instance;
        if (suspition == null)
        {
            Debug.LogError("GameOverController could not find the Suspition singleton.", this);
            enabled = false;
            return;
        }

        suspition.Changed += OnSuspitionChanged;
        OnSuspitionChanged(suspition.Value);
    }

    private void OnDestroy()
    {
        if (suspition != null)
        {
            suspition.Changed -= OnSuspitionChanged;
        }

        if (gameOverTriggered)
        {
            Time.timeScale = 1f;
        }
    }

    private void OnSuspitionChanged(float value)
    {
        if (gameOverTriggered || value < 100f)
        {
            return;
        }

        TriggerGameOver();
    }

    public void TriggerGameOver()
    {
        if (gameOverTriggered)
        {
            return;
        }

        gameOverTriggered = true;
        float score = Info.instance != null ? Info.instance.Value : 0f;
        scoreText.text = $"{score:0}";
        gameOverUI.SetActive(true);
        Time.timeScale = 0f;
    }

    public void Retry()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
