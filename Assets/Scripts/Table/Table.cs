using System.Collections.Generic;
using UnityEngine;

public class Table : MonoBehaviour
{
    private float perimeterRadius = 2f;
    private const float AngrySuspitionPerTick = 2f;
    private const float DrinkingSuspitionPerTick = -1f;
    private const float SuspitionTickInterval = 1f;

    private Transform player;
    private Drink drink;
    private float suspitionTickTime;
    private readonly List<Soldier> arrivedSoldiers = new();

    public bool IsPlayerInside { get; private set; }
    public IReadOnlyList<Soldier> ArrivedSoldiers => arrivedSoldiers;

    public void RegisterArrivedSoldier(Soldier soldier)
    {
        if (soldier != null && !arrivedSoldiers.Contains(soldier))
        {
            arrivedSoldiers.Add(soldier);
        }
    }

    public void ClearArrivedSoldiers()
    {
        arrivedSoldiers.Clear();
    }

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

        drink = GetComponent<Drink>();
        if (drink == null)
        {
            Debug.LogError("Table needs a Drink component for Suspition changes.", this);
            enabled = false;
            return;
        }

        player = playerObject.transform;
    }

    private void Update()
    {
        Vector3 offset = player.position - transform.position;
        IsPlayerInside = offset.sqrMagnitude <= perimeterRadius * perimeterRadius;

        float suspitionPerTick = GetSuspitionPerTick();
        if (Mathf.Approximately(suspitionPerTick, 0f))
        {
            suspitionTickTime = 0f;
            return;
        }

        suspitionTickTime += Time.deltaTime;
        while (suspitionTickTime >= SuspitionTickInterval)
        {
            Suspition.instance.Add(suspitionPerTick);
            suspitionTickTime -= SuspitionTickInterval;
        }
    }

    private float GetSuspitionPerTick()
    {
        return drink.State switch
        {
            Drink.DrinkState.Angry => AngrySuspitionPerTick,
            Drink.DrinkState.WaitingForDrinksAngry => AngrySuspitionPerTick,
            Drink.DrinkState.DrinkingAndGivingInfo => DrinkingSuspitionPerTick,
            _ => 0f
        };
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, perimeterRadius);
    }
}
