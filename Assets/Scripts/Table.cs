using UnityEngine;

public class Table : MonoBehaviour
{
    [SerializeField, Min(0f)] private float perimeterRadius = 1f;
    [SerializeField, Min(0f)] private float suspicionPerInterval = 11f;
    [SerializeField, Min(0.01f)] private float intervalSeconds = 1f;

    private Transform player;
    private float timeInside;

    private void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null)
        {
            Debug.LogError("Table could not find a GameObject tagged Player.", this);
            enabled = false;
            return;
        }

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
        bool isInside = offset.sqrMagnitude <= perimeterRadius * perimeterRadius;

        if (!isInside)
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
