using System;
using UnityEngine;
using UnityEngine.AI;

public class SoldierMovement : MonoBehaviour
{
    private const float ArrivalDistance = 0.1f;
    private const float NavMeshSampleDistance = 2f;

    private NavMeshAgent agent;
    private Transform destination;
    private Action<SoldierMovement> arrivedCallback;
    private bool agentWasDetached;
    private bool isMoving;
    private float movementY;

    private void Awake()
    {
        agent = GetComponentInChildren<NavMeshAgent>(true);
        if (agent == null)
        {
            Debug.LogError("SoldierMovement needs a child NavMeshAgent.", this);
            enabled = false;
            return;
        }

    }

    public bool MoveFromTo(
        Vector3 spawnPosition,
        Transform target,
        Action<SoldierMovement> onArrived)
    {
        if (agent == null || target == null)
        {
            return false;
        }

        if (!agentWasDetached)
        {
            agent.transform.SetParent(null, true);
            agentWasDetached = true;
        }

        if (!NavMesh.SamplePosition(
                spawnPosition,
                out NavMeshHit spawnHit,
                NavMeshSampleDistance,
                agent.areaMask)
            || !NavMesh.SamplePosition(
                target.position,
                out NavMeshHit destinationHit,
                NavMeshSampleDistance,
                agent.areaMask))
        {
            Debug.LogError("Soldier could not find the NavMesh near its door or table position.", this);
            return false;
        }

        movementY = spawnPosition.y;
        transform.position = new Vector3(spawnHit.position.x, movementY, spawnHit.position.z);
        agent.Warp(spawnHit.position);
        destination = target;
        arrivedCallback = onArrived;
        isMoving = agent.SetDestination(destinationHit.position);
        return isMoving;
    }

    private void Update()
    {
        if (!isMoving)
        {
            return;
        }

        Vector3 soldierPosition = agent.transform.position;
        soldierPosition.y = movementY;
        transform.SetPositionAndRotation(soldierPosition, agent.transform.rotation);

        if (agent.pathPending
            || agent.remainingDistance > agent.stoppingDistance + ArrivalDistance)
        {
            return;
        }

        isMoving = false;
        agent.ResetPath();
        transform.rotation = destination.rotation;

        Action<SoldierMovement> callback = arrivedCallback;
        arrivedCallback = null;
        callback?.Invoke(this);
    }

    private void OnDestroy()
    {
        if (agentWasDetached && agent != null)
        {
            Destroy(agent.gameObject);
        }
    }
}
