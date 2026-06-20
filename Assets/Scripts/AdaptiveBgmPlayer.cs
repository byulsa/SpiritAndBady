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
    [Tooltip("Assign tracks in ascending order, for example 60, 90, 120, 160 BPM.")]
    [SerializeField] private BpmTrack[] tracks;
    [SerializeField, Min(1)] private int initialBpm = 60;
    [SerializeField, Min(1)] private int bpmStep = 10;

    [Header("Game References")]
    [SerializeField] private RythmManager rythmManager;
    [SerializeField] private TrainSpeedController speedController;

    [Header("Audio Sources")]
    [Tooltip("Use two sources so changing to another base track can crossfade cleanly.")]
    [SerializeField] private AudioSource primarySource;
    [SerializeField] private AudioSource secondarySource;
    [SerializeField, Range(0f, 1f)] private float volume = 0.75f;
    [SerializeField, Range(0f, 0.5f)] private float trackCrossfadeSeconds = 0.08f;

    [Header("Pitch Preservation")]
    [Tooltip("Enable this after routing both AudioSources through a Music AudioMixer group with a Pitch Shifter effect.")]
    [SerializeField] private bool preservePitch = true;
    [Tooltip("Music group that contains the Pitch Shifter effect. Both AudioSources are routed here.")]
    [SerializeField] private AudioMixerGroup musicMixerGroup;
    [SerializeField] private AudioMixer pitchCompensationMixer;
    [Tooltip("Exposed Pitch parameter of the AudioMixer Pitch Shifter effect.")]
    [SerializeField] private string pitchCompensationParameter = "BgmPitchCompensation";

    public int RequestedBpm { get; private set; }
    public int CurrentBpm { get; private set; }
    public int CurrentSourceBpm => activeTrack != null ? activeTrack.bpm : 0;
    public float CurrentPlaybackRate { get; private set; } = 1f;

    public event Action<int> OnBpmApplied;

    private AudioSource activeSource;
    private AudioSource inactiveSource;
    private BpmTrack activeTrack;
    private Coroutine crossfadeRoutine;
    private bool warnedAboutPitchMixer;

    private void Awake()
    {
        FindReferences();
        FindOrCreateAudioSources();
        ConfigureAudioSource(primarySource);
        ConfigureAudioSource(secondarySource);

        activeSource = primarySource;
        inactiveSource = secondarySource;
        RequestedBpm = QuantizeBpm(initialBpm);
    }

    private void OnEnable()
    {
        if (speedController != null)
        {
            speedController.OnSpeedChanged += HandleSpeedChanged;
        }

        if (rythmManager != null)
        {
            rythmManager.OnMeasureStart += HandleMeasureStart;
        }
    }

    private void Start()
    {
        float speed = speedController != null ? speedController.GetCurrentSpeed() : 0f;
        RequestBpm(speed > 0f ? speed : initialBpm);
    }

    private void OnDisable()
    {
        if (speedController != null)
        {
            speedController.OnSpeedChanged -= HandleSpeedChanged;
        }

        if (rythmManager != null)
        {
            rythmManager.OnMeasureStart -= HandleMeasureStart;
        }
    }

    public void RequestBpm(float bpm)
    {
        int quantizedBpm = QuantizeBpm(bpm);
        if (!HasUsableTracks())
        {
            Debug.LogError("AdaptiveBgmPlayer needs at least one BPM track with an AudioClip.", this);
            return;
        }

        RequestedBpm = quantizedBpm;

        if (rythmManager != null && rythmManager.IsRunning)
        {
            rythmManager.ChangeBpmOnNextMeasure(RequestedBpm);
            return;
        }

        ApplyBpm(RequestedBpm, 0);
    }

    public void Stop()
    {
        if (crossfadeRoutine != null)
        {
            StopCoroutine(crossfadeRoutine);
            crossfadeRoutine = null;
        }

        primarySource.Stop();
        secondarySource.Stop();
        activeTrack = null;
        CurrentBpm = 0;
        CurrentPlaybackRate = 1f;
        ApplyPitchCompensation(1f);
    }

    private void HandleSpeedChanged(float speed)
    {
        RequestBpm(speed);
    }

    private void HandleMeasureStart(int measureIndex)
    {
        if (activeTrack == null || !activeSource.isPlaying || CurrentBpm != RequestedBpm)
        {
            ApplyBpm(RequestedBpm, measureIndex);
        }
    }

    private void ApplyBpm(int targetBpm, int measureIndex)
    {
        BpmTrack selectedTrack = FindTrackForBpm(targetBpm);
        if (selectedTrack == null)
        {
            Debug.LogError($"No BGM track is available for {targetBpm} BPM.", this);
            return;
        }

        float playbackRate = (float)targetBpm / selectedTrack.bpm;
        if (playbackRate < 0.5f || playbackRate > 2f)
        {
            Debug.LogError(
                $"{selectedTrack.bpm} BPM track cannot safely cover {targetBpm} BPM. " +
                "Add another base track so the playback-rate ratio stays between 0.5 and 2.0.",
                this);
            return;
        }

        CurrentBpm = targetBpm;
        CurrentPlaybackRate = playbackRate;
        ApplyPitchCompensation(playbackRate);

        if (activeTrack == selectedTrack && activeSource.isPlaying)
        {
            activeSource.pitch = playbackRate;
            OnBpmApplied?.Invoke(CurrentBpm);
            return;
        }

        StartSelectedTrack(selectedTrack, measureIndex, playbackRate);
        OnBpmApplied?.Invoke(CurrentBpm);
    }

    private void StartSelectedTrack(BpmTrack selectedTrack, int measureIndex, float playbackRate)
    {
        if (crossfadeRoutine != null)
        {
            StopCoroutine(crossfadeRoutine);
            crossfadeRoutine = null;
        }

        AudioSource oldSource = activeSource;
        AudioSource newSource = activeTrack == null ? activeSource : inactiveSource;

        newSource.Stop();
        newSource.clip = selectedTrack.clip;
        newSource.pitch = playbackRate;
        newSource.loop = true;
        newSource.timeSamples = GetMeasureStartSample(selectedTrack, measureIndex);

        bool shouldCrossfade = activeTrack != null && oldSource.isPlaying && trackCrossfadeSeconds > 0f;
        newSource.volume = shouldCrossfade ? 0f : volume;
        newSource.Play();

        activeSource = newSource;
        inactiveSource = oldSource == newSource ? secondarySource : oldSource;
        activeTrack = selectedTrack;

        if (shouldCrossfade)
        {
            crossfadeRoutine = StartCoroutine(Crossfade(oldSource, newSource));
        }
        else if (oldSource != newSource)
        {
            oldSource.Stop();
        }
    }

    private IEnumerator Crossfade(AudioSource oldSource, AudioSource newSource)
    {
        float elapsed = 0f;
        float oldStartVolume = oldSource.volume;

        while (elapsed < trackCrossfadeSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / trackCrossfadeSeconds);
            oldSource.volume = oldStartVolume * Mathf.Cos(progress * Mathf.PI * 0.5f);
            newSource.volume = volume * Mathf.Sin(progress * Mathf.PI * 0.5f);
            yield return null;
        }

        oldSource.Stop();
        oldSource.volume = volume;
        newSource.volume = volume;
        crossfadeRoutine = null;
    }

    private BpmTrack FindTrackForBpm(int targetBpm)
    {
        BpmTrack lowestTrack = null;
        BpmTrack bestTrack = null;

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

    private int QuantizeBpm(float bpm)
    {
        int step = Mathf.Max(1, bpmStep);
        return Mathf.Max(step, Mathf.RoundToInt(bpm / step) * step);
    }

    private bool HasUsableTracks()
    {
        if (tracks == null)
        {
            return false;
        }

        foreach (BpmTrack track in tracks)
        {
            if (track != null && track.clip != null && track.bpm > 0)
            {
                return true;
            }
        }

        return false;
    }

    private void FindReferences()
    {
        if (rythmManager == null)
        {
            rythmManager = FindAnyObjectByType<RythmManager>();
        }

        if (speedController == null)
        {
            speedController = FindAnyObjectByType<TrainSpeedController>();
        }
    }

    private void FindOrCreateAudioSources()
    {
        AudioSource[] sources = GetComponents<AudioSource>();

        if (primarySource == null)
        {
            primarySource = sources.Length > 0 ? sources[0] : gameObject.AddComponent<AudioSource>();
        }

        if (secondarySource == null || secondarySource == primarySource)
        {
            for (int i = 0; i < sources.Length; i++)
            {
                if (sources[i] != primarySource)
                {
                    secondarySource = sources[i];
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
        initialBpm = Mathf.Max(1, initialBpm);
        bpmStep = Mathf.Max(1, bpmStep);

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
