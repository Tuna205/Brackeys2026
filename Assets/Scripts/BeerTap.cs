using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class BeerTap : MonoBehaviour
{
    private const float PourSoundDuration = 3f;

    [SerializeField] private InputActionAsset inputActions = null;
    [SerializeField] private Player.BeerTypes beerType = Player.BeerTypes.White;
    [SerializeField] private AudioSource pourAudioSource = null;
    [SerializeField] private AudioClip pourClip = null;

    private InputAction interactAction;
    private Collider playerColliderInside;
    private Coroutine stopPourSoundRoutine;

    private void Awake()
    {
        if (inputActions == null)
        {
            Debug.LogError("BeerTap needs an Input Action Asset.", this);
            enabled = false;
            return;
        }

        interactAction = inputActions.FindAction("Player/Jump", true).Clone();
    }

    private void OnEnable()
    {
        interactAction.performed += OnInteract;
        interactAction.Enable();
    }

    private void OnDisable()
    {
        if (interactAction != null)
        {
            interactAction.performed -= OnInteract;
            interactAction.Disable();
        }

        StopPourSound();
        playerColliderInside = null;
    }

    private void OnDestroy()
    {
        interactAction?.Dispose();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (playerColliderInside != null || !IsPlayerCollider(other))
        {
            return;
        }

        playerColliderInside = other;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other == playerColliderInside)
        {
            playerColliderInside = null;
        }
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        if (playerColliderInside != null && Player.instance.AddBeer(beerType))
        {
            PlayPourSound();
        }
    }

    private void PlayPourSound()
    {
        if (pourAudioSource == null || pourClip == null)
        {
            Debug.LogWarning("BeerTap is missing its pour AudioSource or SFX_Pour clip.", this);
            return;
        }

        StopPourSound();
        pourAudioSource.PlayOneShot(pourClip);
        stopPourSoundRoutine = StartCoroutine(StopPourSoundAfterDuration());
    }

    private IEnumerator StopPourSoundAfterDuration()
    {
        yield return new WaitForSeconds(PourSoundDuration);
        pourAudioSource.Stop();
        stopPourSoundRoutine = null;
    }

    private void StopPourSound()
    {
        if (stopPourSoundRoutine != null)
        {
            StopCoroutine(stopPourSoundRoutine);
            stopPourSoundRoutine = null;
        }

        if (pourAudioSource != null)
        {
            pourAudioSource.Stop();
        }
    }

    private static bool IsPlayerCollider(Collider other)
    {
        return Player.instance != null &&
            (other.gameObject == Player.instance.gameObject ||
             other.transform.IsChildOf(Player.instance.transform));
    }
}
