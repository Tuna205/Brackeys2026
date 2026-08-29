using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public sealed class FootstepAudio : MonoBehaviour
{
    [SerializeField] private AudioClip[] stepClips = null;
    [SerializeField, Min(0.05f)] private float stepInterval = 0.42f;
    [SerializeField, Range(0f, 1f)] private float volume = 0.6f;
    [SerializeField, Range(0f, 0.25f)] private float pitchVariation = 0.06f;

    private AudioSource audioSource;
    private bool isWalking;
    private float nextStepTime;
    private int lastClipIndex = -1;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;

        if (stepClips == null || stepClips.Length == 0)
        {
            Debug.LogWarning("FootstepAudio needs at least one step clip.", this);
        }
    }

    private void Update()
    {
        if (!isWalking || stepClips == null || stepClips.Length == 0 || Time.time < nextStepTime)
        {
            return;
        }

        PlayStep();
        nextStepTime = Time.time + stepInterval;
    }

    private void OnDisable()
    {
        isWalking = false;
        audioSource?.Stop();
    }

    public void SetWalking(bool walking)
    {
        if (isWalking == walking)
        {
            return;
        }

        isWalking = walking;
        if (isWalking)
        {
            nextStepTime = Time.time;
        }
    }

    private void PlayStep()
    {
        int clipIndex;
        if (stepClips.Length == 1)
        {
            clipIndex = 0;
        }
        else if (lastClipIndex < 0)
        {
            clipIndex = Random.Range(0, stepClips.Length);
        }
        else
        {
            clipIndex = (lastClipIndex + 1) % stepClips.Length;
        }

        AudioClip clip = stepClips[clipIndex];
        if (clip == null)
        {
            return;
        }

        lastClipIndex = clipIndex;
        audioSource.pitch = Random.Range(1f - pitchVariation, 1f + pitchVariation);
        audioSource.PlayOneShot(clip, volume);
    }
}
