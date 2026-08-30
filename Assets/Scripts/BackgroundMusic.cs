using System.Collections;
using UnityEngine;

public sealed class BackgroundMusic : MonoBehaviour
{
    [Header("Sources")]
    [SerializeField] private AudioSource mellowSource = null;
    [SerializeField] private AudioSource bigBandSource = null;

    [Header("Tracks")]
    [SerializeField] private AudioClip mellowTrack = null;
    [SerializeField] private AudioClip bigBandTrack = null;

    [Header("Transition")]
    [SerializeField, Range(0f, 100f)] private float bigBandThreshold = 50f;
    [SerializeField, Min(0f)] private float crossfadeDuration = 2f;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.55f;

    private Suspition suspition;
    private Coroutine crossfadeRoutine;
    private bool bigBandIsActive;
    private bool configurationIsValid;

    private void Awake()
    {
        configurationIsValid = mellowSource != null
            && bigBandSource != null
            && mellowSource != bigBandSource
            && mellowTrack != null
            && bigBandTrack != null;

        if (!configurationIsValid)
        {
            Debug.LogError(
                "BackgroundMusic needs two different AudioSources and both music tracks.",
                this);
            return;
        }

        ConfigureSource(mellowSource, mellowTrack);
        ConfigureSource(bigBandSource, bigBandTrack);
    }

    private void OnEnable()
    {
        StartCoroutine(BeginPlayback());
    }

    private void OnDisable()
    {
        if (suspition != null)
        {
            suspition.Changed -= OnSuspitionChanged;
            suspition = null;
        }

        if (crossfadeRoutine != null)
        {
            StopCoroutine(crossfadeRoutine);
            crossfadeRoutine = null;
        }

        if (mellowSource != null)
        {
            mellowSource.Stop();
        }

        if (bigBandSource != null)
        {
            bigBandSource.Stop();
        }
    }

    private IEnumerator BeginPlayback()
    {
        yield return null;

        if (!configurationIsValid)
        {
            yield break;
        }

        suspition = Suspition.instance;
        float initialSuspition = suspition != null ? suspition.Value : 0f;
        bigBandIsActive = initialSuspition > bigBandThreshold;

        mellowSource.volume = bigBandIsActive ? 0f : musicVolume;
        bigBandSource.volume = bigBandIsActive ? musicVolume : 0f;

        double startTime = AudioSettings.dspTime + 0.05d;
        mellowSource.PlayScheduled(startTime);
        bigBandSource.PlayScheduled(startTime);

        if (suspition == null)
        {
            Debug.LogWarning(
                "BackgroundMusic could not find Suspition, so it will remain mellow.",
                this);
            yield break;
        }

        suspition.Changed += OnSuspitionChanged;
    }

    private void OnSuspitionChanged(float value)
    {
        bool shouldUseBigBand = value > bigBandThreshold;
        if (shouldUseBigBand == bigBandIsActive)
        {
            return;
        }

        AudioSource outgoingSource = bigBandIsActive ? bigBandSource : mellowSource;
        AudioSource incomingSource = shouldUseBigBand ? bigBandSource : mellowSource;
        SynchronizeTimestamp(incomingSource, outgoingSource);
        bigBandIsActive = shouldUseBigBand;

        if (crossfadeRoutine != null)
        {
            StopCoroutine(crossfadeRoutine);
        }

        crossfadeRoutine = StartCoroutine(CrossfadeToCurrentTrack());
    }

    private IEnumerator CrossfadeToCurrentTrack()
    {
        float mellowStartVolume = mellowSource.volume;
        float bigBandStartVolume = bigBandSource.volume;
        float mellowTargetVolume = bigBandIsActive ? 0f : musicVolume;
        float bigBandTargetVolume = bigBandIsActive ? musicVolume : 0f;

        if (crossfadeDuration <= 0f)
        {
            mellowSource.volume = mellowTargetVolume;
            bigBandSource.volume = bigBandTargetVolume;
            crossfadeRoutine = null;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < crossfadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float blend = Mathf.SmoothStep(0f, 1f, elapsed / crossfadeDuration);
            mellowSource.volume = Mathf.Lerp(mellowStartVolume, mellowTargetVolume, blend);
            bigBandSource.volume = Mathf.Lerp(bigBandStartVolume, bigBandTargetVolume, blend);
            yield return null;
        }

        mellowSource.volume = mellowTargetVolume;
        bigBandSource.volume = bigBandTargetVolume;
        crossfadeRoutine = null;
    }

    private static void ConfigureSource(AudioSource source, AudioClip track)
    {
        source.clip = track;
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
    }

    private static void SynchronizeTimestamp(AudioSource incoming, AudioSource outgoing)
    {
        if (incoming.clip == null || outgoing.clip == null)
        {
            return;
        }

        double outgoingSeconds = outgoing.timeSamples / (double)outgoing.clip.frequency;
        double incomingSeconds = outgoingSeconds % incoming.clip.length;
        int incomingSample = (int)(incomingSeconds * incoming.clip.frequency);
        incoming.timeSamples = Mathf.Clamp(incomingSample, 0, incoming.clip.samples - 1);
    }
}
