using UnityEngine;

public class Table : MonoBehaviour
{
    private float perimeterRadius = 2f;
    private float suspicionPerInterval = 6f;
    private float intervalSeconds = 1f;

    private Transform player;
    private float timeInside;

    public bool IsPlayerInside { get; private set; }

    private void Start()
    {
        if (Player.instance == null)
        {
            Debug.LogError("Table could not find the Player singleton.", this);
            enabled = false;
            return;
        }

        GameObject playerObject = Player.instance.gameObject;

        if (Suspition.instance == null)
        {
            Debug.LogError("Table could not find the Suspition singleton.", this);
            enabled = false;
            return;
        }

        player = playerObject.transform;
    }

    private void Update()
    {
        Vector3 offset = player.position - transform.position;
        IsPlayerInside = offset.sqrMagnitude <= perimeterRadius * perimeterRadius;

        if (!IsPlayerInside)
        {
            timeInside = 0f;
            return;
        }

        timeInside += Time.deltaTime;
        while (timeInside >= intervalSeconds)
        {
            Suspition.instance.Add(suspicionPerInterval);
            timeInside -= intervalSeconds;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, perimeterRadius);
    }
}
