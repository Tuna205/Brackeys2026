using UnityEngine;

public class GameOverController : MonoBehaviour
{
    [SerializeField] private GameObject gameOverUI = null;

    private Suspition suspition;
    private bool gameOverTriggered;

    private void Awake()
    {
        if (gameOverUI == null)
        {
            Debug.LogError("GameOverController needs a GameOver UI object.", this);
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
        gameOverUI.SetActive(true);
        Time.timeScale = 0f;
    }
}
