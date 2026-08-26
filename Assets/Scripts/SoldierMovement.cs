using System;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class SoldierMovement : MonoBehaviour
{
    private const float ArrivalDistance = 0.1f;
    private const float NavMeshSampleDistance = 2f;

    private NavMeshAgent agent;
    private Transform destination;
    private Action<SoldierMovement> arrivedCallback;
    private bool isMoving;
    private float movementY;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updatePosition = false;
        agent.updateRotation = false;
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
        agent.Warp(spawnHit.position);
        transform.position = new Vector3(spawnHit.position.x, movementY, spawnHit.position.z);
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

        Vector3 soldierPosition = agent.nextPosition;
        soldierPosition.y = movementY;
        transform.position = soldierPosition;

        Vector3 movementDirection = agent.desiredVelocity;
        movementDirection.y = 0f;
        if (movementDirection.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(movementDirection, Vector3.up);
        }

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

}
