using System.Collections;
using UnityEngine;

public sealed class SoldierVoiceAudio : MonoBehaviour
{
    [SerializeField] private AudioSource voiceSource = null;
    [SerializeField] private AudioClip[] talkingClips = null;
    [SerializeField] private Vector2 pauseRange = new(0.35f, 0.9f);
    [SerializeField, Range(0f, 0.25f)] private float pitchVariation = 0.04f;

    private Coroutine talkingRoutine;
    private int nextClipIndex;

    private void Awake()
    {
        if (voiceSource == null)
        {
            Debug.LogError($"{name} needs a dedicated voice AudioSource.", this);
            enabled = false;
            return;
        }

        voiceSource.playOnAwake = false;
        voiceSource.loop = false;
    }

    private void OnDisable()
    {
        StopTalking();
    }

    public void SetVolume(float volume)
    {
        if (!enabled || voiceSource == null)
        {
            return;
        }

        voiceSource.volume = Mathf.Clamp01(volume);

        if (voiceSource.volume <= 0f)
        {
            StopTalking();
        }
        else if (talkingRoutine == null && HasTalkingClips())
        {
            nextClipIndex = Random.Range(0, talkingClips.Length);
            talkingRoutine = StartCoroutine(CycleTalkingClips());
        }
    }

    public void StopTalking()
    {
        if (talkingRoutine != null)
        {
            StopCoroutine(talkingRoutine);
            talkingRoutine = null;
        }

        if (voiceSource != null)
        {
            voiceSource.Stop();
            voiceSource.pitch = 1f;
        }
    }

    private IEnumerator CycleTalkingClips()
    {
        yield return new WaitForSeconds(Random.Range(0f, pauseRange.y));

        while (true)
        {
            AudioClip clip = GetNextClip();
            if (clip == null)
            {
                talkingRoutine = null;
                yield break;
            }

            voiceSource.pitch = Random.Range(1f - pitchVariation, 1f + pitchVariation);
            voiceSource.clip = clip;
            voiceSource.Play();

            yield return new WaitForSeconds(clip.length / voiceSource.pitch);
            yield return new WaitForSeconds(Random.Range(pauseRange.x, pauseRange.y));
        }
    }

    private AudioClip GetNextClip()
    {
        if (!HasTalkingClips())
        {
            return null;
        }

        for (int i = 0; i < talkingClips.Length; i++)
        {
            AudioClip clip = talkingClips[nextClipIndex];
            nextClipIndex = (nextClipIndex + 1) % talkingClips.Length;

            if (clip != null)
            {
                return clip;
            }
        }

        return null;
    }

    private bool HasTalkingClips()
    {
        return talkingClips != null && talkingClips.Length > 0;
    }
}
