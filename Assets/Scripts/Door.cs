using UnityEngine;

[DisallowMultipleComponent]
public class Door : MonoBehaviour
{
    private void Awake()
    {
        Rigidbody doorRigidbody = GetComponent<Rigidbody>();
        if (doorRigidbody == null)
        {
            doorRigidbody = gameObject.AddComponent<Rigidbody>();
        }

        doorRigidbody.isKinematic = true;
        doorRigidbody.useGravity = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        Soldier soldier = other.GetComponentInParent<Soldier>();
        if (soldier != null && soldier.IsLeaving)
        {
            soldier.ExitThroughDoor();
        }
    }
}
