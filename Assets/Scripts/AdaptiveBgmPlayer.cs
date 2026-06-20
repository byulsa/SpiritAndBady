using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class AdaptiveBgmPlayer : MonoBehaviour
{
    [Serializable]
    private class BpmTrack
    {
        [Min(1)] public int bpm = 60;
        [Min(1)] public int bars = 32;
        public AudioClip clip;
    }

    [Header("BPM Tracks")]
    [Tooltip("Assign base tracks in ascending order, for example 60, 100, 120, 160 BPM.")]
    [SerializeField] private BpmTrack[] tracks;

    [Header("Rhythm Source")]
    [Tooltip("The sole owner of the clock and BPM. This component only subscribes to its events.")]
    [SerializeField] private RythmManager rythmManager;

    [Header("Audio Sources")]
    [Tooltip("Two sources allow a new speed or base track to be DSP-scheduled on a measure boundary.")]
    [SerializeField] private AudioSource primarySource;
    [SerializeField] private AudioSource secondarySource;
    [SerializeField, Range(0f, 1f)] private float volume = 0.75f;
    [Tooltip("The old source fades out immediately before the scheduled measure boundary.")]
    [SerializeField, Range(0f, 0.5f)] private float trackCrossfadeSeconds = 0.08f;

    [Header("Pitch Preservation")]
    [Tooltip("Route both sources through a Music group with a Pitch Shifter effect.")]
    [SerializeField] private bool preservePitch = true;
    [SerializeField] private AudioMixerGroup musicMixerGroup;
    [SerializeField] private AudioMixer pitchCompensationMixer;
    [Tooltip("Exposed Pitch parameter of the AudioMixer Pitch Shifter effect.")]
    [SerializeField] private string pitchCompensationParameter = "BgmPitchCompensation";

    public int CurrentBpm { get; private set; }
    public int ScheduledBpm { get; private set; }
    public int CurrentSourceBpm => activeTrack != null ? activeTrack.bpm : 0;
    public float CurrentPlaybackRate { get; private set; } = 1f;

    public event Action<int> OnBpmApplied;

    private AudioSource activeSource;
    private AudioSource inactiveSource;
    private AudioSource pendingSource;
    private BpmTrack activeTrack;
    private Coroutine transitionRoutine;
    private double activeStartDspTime = double.NegativeInfinity;
    private bool warnedAboutPitchMixer;

    private void Awake()
    {
        FindRythmManager();
        FindOrCreateAudioSources();
        ConfigureAudioSource(primarySource);
        ConfigureAudioSource(secondarySource);

        activeSource = primarySource;
        inactiveSource = secondarySource;
    }

    private void OnEnable()
    {
        FindRythmManager();
        SubscribeToRythmManager();
    }

    private void Start()
    {
        if (rythmManager == null)
        {
            Debug.LogError("AdaptiveBgmPlayer requires RythmManager.", this);
            return;
        }

        // Handles a BGM object that was enabled after the clock had already started.
        if (rythmManager.IsRunning && activeTrack == null)
        {
            int targetMeasureIndex = rythmManager.CurrentMeasureIndex < 0
                ? 0
                : rythmManager.CurrentMeasureIndex + 1;
            ScheduleInitialPlayback(
                rythmManager.NextMeasureBPM,
                rythmManager.GetNextMeasureDspTime(0f),
                targetMeasureIndex);
        }
    }

    private void OnDisable()
    {
        UnsubscribeFromRythmManager();
        StopPlayback();
    }

    private void HandleClockScheduled(double startDspTime, float bpm)
    {
        ScheduleInitialPlayback(bpm, startDspTime, 0);
    }

    private void HandleClockStopped()
    {
        StopPlayback();
    }

    private void HandleBpmChangeScheduled(
        float bpm,
        double effectiveDspTime,
        int targetMeasureIndex)
    {
        int targetBpm = Mathf.RoundToInt(bpm);
        ScheduledBpm = targetBpm;

        if (activeTrack == null ||
            (!activeSource.isPlaying && AudioSettings.dspTime < activeStartDspTime))
        {
            ScheduleInitialPlayback(targetBpm, effectiveDspTime, targetMeasureIndex);
            return;
        }

        ScheduleTransition(targetBpm, effectiveDspTime, targetMeasureIndex);
    }

    private void ScheduleInitialPlayback(float bpm, double dspTime, int measureIndex)
    {
        if (!TryGetPlayback(bpm, out int targetBpm, out BpmTrack track, out float playbackRate))
        {
            return;
        }

        CancelTransition();
        primarySource.Stop();
        secondarySource.Stop();

        activeSource = primarySource;
        inactiveSource = secondarySource;
        pendingSource = null;
        activeTrack = track;
        activeStartDspTime = dspTime;
        ScheduledBpm = targetBpm;

        PrepareSource(
            activeSource,
            track,
            playbackRate,
            GetMeasureStartSample(track, measureIndex));
        ApplyPitchCompensation(playbackRate);
        activeSource.PlayScheduled(Math.Max(dspTime, AudioSettings.dspTime));

        transitionRoutine = StartCoroutine(
            CompleteInitialStartAtDsp(targetBpm, playbackRate, dspTime));
    }

    private void ScheduleTransition(int targetBpm, double dspTime, int measureIndex)
    {
        if (!TryGetPlayback(
                targetBpm,
                out int resolvedBpm,
                out BpmTrack nextTrack,
                out float playbackRate))
        {
            return;
        }

        targetBpm = resolvedBpm;

        CancelTransition();

        AudioSource oldSource = activeSource;
        AudioSource newSource = inactiveSource;
        int startSample = nextTrack == activeTrack
            ? GetMeasureStartSample(nextTrack, measureIndex)
            : 0;

        PrepareSource(newSource, nextTrack, playbackRate, startSample);
        newSource.PlayScheduled(Math.Max(dspTime, AudioSettings.dspTime));
        oldSource.SetScheduledEndTime(Math.Max(dspTime, AudioSettings.dspTime));

        pendingSource = newSource;
        ScheduledBpm = targetBpm;
        transitionRoutine = StartCoroutine(
            CompleteTransitionAtDsp(
                oldSource,
                newSource,
                nextTrack,
                targetBpm,
                playbackRate,
                dspTime));
    }

    private IEnumerator CompleteInitialStartAtDsp(
        int targetBpm,
        float playbackRate,
        double dspTime)
    {
        while (AudioSettings.dspTime < dspTime)
        {
            yield return null;
        }

        CurrentBpm = targetBpm;
        CurrentPlaybackRate = playbackRate;
        transitionRoutine = null;
        OnBpmApplied?.Invoke(CurrentBpm);
    }

    private IEnumerator CompleteTransitionAtDsp(
        AudioSource oldSource,
        AudioSource newSource,
        BpmTrack nextTrack,
        int targetBpm,
        float playbackRate,
        double dspTime)
    {
        double fadeStartDspTime = dspTime - trackCrossfadeSeconds;

        while (AudioSettings.dspTime < dspTime)
        {
            if (trackCrossfadeSeconds > 0f && AudioSettings.dspTime >= fadeStartDspTime)
            {
                float remaining = (float)(dspTime - AudioSettings.dspTime);
                oldSource.volume = volume * Mathf.Clamp01(remaining / trackCrossfadeSeconds);
            }

            yield return null;
        }

        ApplyPitchCompensation(playbackRate);
        oldSource.Stop();
        oldSource.volume = volume;

        activeSource = newSource;
        inactiveSource = oldSource;
        pendingSource = null;
        activeTrack = nextTrack;
        activeStartDspTime = dspTime;
        CurrentBpm = targetBpm;
        CurrentPlaybackRate = playbackRate;
        transitionRoutine = null;

        OnBpmApplied?.Invoke(CurrentBpm);
    }

    private bool TryGetPlayback(
        float bpm,
        out int targetBpm,
        out BpmTrack track,
        out float playbackRate)
    {
        targetBpm = Mathf.Max(1, Mathf.RoundToInt(bpm));
        track = FindTrackForBpm(targetBpm);
        playbackRate = 1f;

        if (track == null)
        {
            Debug.LogError($"No BGM track is available for {targetBpm} BPM.", this);
            return false;
        }

        playbackRate = (float)targetBpm / track.bpm;
        if (playbackRate < 0.5f || playbackRate > 2f)
        {
            Debug.LogError(
                $"{track.bpm} BPM track cannot safely cover {targetBpm} BPM. " +
                "Add another base track so the playback-rate ratio stays between 0.5 and 2.0.",
                this);
            return false;
        }

        return true;
    }

    private void PrepareSource(
        AudioSource source,
        BpmTrack track,
        float playbackRate,
        int startSample)
    {
        source.Stop();
        source.clip = track.clip;
        source.pitch = playbackRate;
        source.loop = true;
        source.volume = volume;
        source.timeSamples = startSample;
    }

    private BpmTrack FindTrackForBpm(int targetBpm)
    {
        BpmTrack lowestTrack = null;
        BpmTrack bestTrack = null;

        if (tracks == null)
        {
            return null;
        }

        foreach (BpmTrack track in tracks)
        {
            if (track == null || track.clip == null || track.bpm <= 0)
            {
                continue;
            }

            if (lowestTrack == null || track.bpm < lowestTrack.bpm)
            {
                lowestTrack = track;
            }

            if (track.bpm <= targetBpm && (bestTrack == null || track.bpm > bestTrack.bpm))
            {
                bestTrack = track;
            }
        }

        return bestTrack ?? lowestTrack;
    }

    private int GetMeasureStartSample(BpmTrack track, int measureIndex)
    {
        int barCount = Mathf.Max(1, track.bars);
        int loopMeasure = ((measureIndex % barCount) + barCount) % barCount;
        double normalizedPosition = (double)loopMeasure / barCount;
        int sample = (int)Math.Round(track.clip.samples * normalizedPosition);
        return Mathf.Clamp(sample, 0, track.clip.samples - 1);
    }

    private void ApplyPitchCompensation(float playbackRate)
    {
        float compensation = preservePitch ? 1f / playbackRate : 1f;

        if (pitchCompensationMixer != null &&
            !string.IsNullOrWhiteSpace(pitchCompensationParameter) &&
            pitchCompensationMixer.SetFloat(pitchCompensationParameter, compensation))
        {
            return;
        }

        if (preservePitch && !warnedAboutPitchMixer)
        {
            Debug.LogWarning(
                "Pitch preservation is enabled, but the AudioMixer Pitch Shifter parameter is not assigned. " +
                "Tempo will still change, but pitch will change with it until the mixer is configured.",
                this);
            warnedAboutPitchMixer = true;
        }
    }

    private void StopPlayback()
    {
        CancelTransition();
        primarySource.Stop();
        secondarySource.Stop();
        primarySource.volume = volume;
        secondarySource.volume = volume;
        activeSource = primarySource;
        inactiveSource = secondarySource;
        pendingSource = null;
        activeTrack = null;
        activeStartDspTime = double.NegativeInfinity;
        CurrentBpm = 0;
        ScheduledBpm = 0;
        CurrentPlaybackRate = 1f;
        ApplyPitchCompensation(1f);
    }

    private void CancelTransition()
    {
        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
        }

        if (pendingSource != null && pendingSource != activeSource)
        {
            pendingSource.Stop();
            pendingSource.volume = volume;
        }

        pendingSource = null;
        if (activeSource != null)
        {
            activeSource.volume = volume;
        }
    }

    private void FindRythmManager()
    {
        if (rythmManager == null)
        {
            rythmManager = FindAnyObjectByType<RythmManager>();
        }
    }

    private void SubscribeToRythmManager()
    {
        if (rythmManager == null)
        {
            return;
        }

        rythmManager.OnClockScheduled += HandleClockScheduled;
        rythmManager.OnClockStopped += HandleClockStopped;
        rythmManager.OnBpmChangeScheduled += HandleBpmChangeScheduled;
    }

    private void UnsubscribeFromRythmManager()
    {
        if (rythmManager == null)
        {
            return;
        }

        rythmManager.OnClockScheduled -= HandleClockScheduled;
        rythmManager.OnClockStopped -= HandleClockStopped;
        rythmManager.OnBpmChangeScheduled -= HandleBpmChangeScheduled;
    }

    private void FindOrCreateAudioSources()
    {
        AudioSource[] sources = GetComponents<AudioSource>();

        if (primarySource == null)
        {
            primarySource = sources.Length > 0
                ? sources[0]
                : gameObject.AddComponent<AudioSource>();
        }

        if (secondarySource == null || secondarySource == primarySource)
        {
            foreach (AudioSource source in sources)
            {
                if (source != primarySource)
                {
                    secondarySource = source;
                    break;
                }
            }

            if (secondarySource == null || secondarySource == primarySource)
            {
                secondarySource = gameObject.AddComponent<AudioSource>();
            }
        }
    }

    private void ConfigureAudioSource(AudioSource source)
    {
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        source.dopplerLevel = 0f;
        source.volume = volume;

        if (musicMixerGroup != null)
        {
            source.outputAudioMixerGroup = musicMixerGroup;
        }
        else if (source == secondarySource && primarySource.outputAudioMixerGroup != null)
        {
            source.outputAudioMixerGroup = primarySource.outputAudioMixerGroup;
        }
    }

    private void OnValidate()
    {
        if (tracks == null)
        {
            return;
        }

        foreach (BpmTrack track in tracks)
        {
            if (track == null)
            {
                continue;
            }

            track.bpm = Mathf.Max(1, track.bpm);
            track.bars = Mathf.Max(1, track.bars);
        }
    }
}
