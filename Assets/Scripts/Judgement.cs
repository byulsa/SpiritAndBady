using UnityEngine;
using System.Collections.Generic;
using System;
using TMPro;
using JetBrains.Annotations;
using UnityEngine.Timeline;

public enum JudgeType
{
    Perfect,
    Good,
    Miss
}

public class Judgement : MonoBehaviour
{
    public CameraMoving cameraMoving;
    public event Action<JudgeType> OnJudged;

    [Header("Judge Line")]
    public Transform judgeLine;

    [Header("Judge Range")]
    public float perfectRange = 0.1f;
    public float goodRange = 0.25f;
    public float missRange = 0.5f;

    public JudgeType judgeType = JudgeType.Perfect;

    private readonly List<Transform> notes = new();
    [Header("Judgement Particle")]
    public ParticleSystem ExFire;
    [Header("Judgement Text")]
    public TextMeshProUGUI judgeText;
    private Animator judgeTextAnim;

    [Header("Judgement Sound")]
    public AudioSource audioSource;
    [SerializeField] private AudioClip perfectSound;
    [SerializeField] private AudioClip goodSound;
    [SerializeField] private AudioClip failSound;
    [SerializeField, Range(0f, 1f)] private float judgementSoundVolume = 1f;

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }
    void Start()
    {
        judgeTextAnim = judgeText.GetComponent<Animator>();
    }

    private void Update()
    {
        CheckMiss();

        if (Input.GetKeyDown(KeyCode.Space))
        {
            JudgeCurrentNote();
        }
    }

    public void RegisterNote(Transform note)
    {
        if (note != null)
        {
            notes.Add(note);
        }
    }

    private void CheckMiss()
    {
        Transform note = GetCurrentNote();

        if (note == null)
        {
            return;
        }

        TimedNoteMover mover = note.GetComponent<TimedNoteMover>();

        if (mover != null && mover.HasPassedJudgementPoint(goodRange))
        {
            ResolveNote(note, JudgeType.Miss);
        }
    }

    public void JudgeCurrentNote()
    {
        Transform note = GetCurrentNote();

        if (note == null)
        {
            return;
        }

        if (!CanJudge(note))
        {
            return;
        }

        ResolveNote(note, Judge(note));
    }

    public JudgeType Judge(Transform note)
    {
        if (note == null || judgeLine == null)
        {
            return JudgeType.Miss;
        }

        float distance = Mathf.Abs(note.position.x - judgeLine.position.x);

        if (distance <= perfectRange)
        {
            return JudgeType.Perfect;
        }

        if (distance <= goodRange)
        {
            return JudgeType.Good;
        }

        return JudgeType.Miss;
    }

    public void ResolveNote(Transform note, JudgeType result)
    {
        judgeType = result;

        OnJudged?.Invoke(result);
        PlayJudgementSound(result);


        if (note == null)
        {
            return;
        }

        notes.RemoveAt(0);
        judgeText.text = result.ToString();
        judgeTextAnim.Play("OnPlay");
        if (result == JudgeType.Perfect)
        {
            cameraMoving.Zoom(13f, 0.25f);
            ExFire.Play();
        }
        Destroy(note.gameObject);
        Debug.Log($"Judgement : {result}");
    }

    private void PlayJudgementSound(JudgeType result)
    {
        if (audioSource == null)
        {
            return;
        }

        AudioClip clip = result switch
        {
            JudgeType.Perfect => perfectSound,
            JudgeType.Good => goodSound,
            JudgeType.Miss => failSound,
            _ => null
        };

        if (clip != null)
        {
            audioSource.PlayOneShot(clip, judgementSoundVolume);
        }
    }

    public bool CanJudge(Transform note)
    {
        if (note == null)
        {
            return false;
        }

        float distance = Mathf.Abs(note.position.x - judgeLine.position.x);

        return distance <= missRange;
    }

    private Transform GetCurrentNote()
    {
        while (notes.Count > 0 && notes[0] == null)
        {
            notes.RemoveAt(0);
        }

        if (notes.Count == 0)
        {
            return null;
        }

        return notes[0];
    }

    private void OnDrawGizmos()
    {
        if (judgeLine == null)
        {
            return;
        }

        Vector3 center = judgeLine.position;
        float height = 1f;

        // Perfect
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(
            center,
            new Vector3(perfectRange * 2f, height, 0.1f));

        // Good
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(
            center,
            new Vector3(goodRange * 2f, height, 0.1f));

        // Miss
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(
            center,
            new Vector3(missRange * 2f, height, 0.1f));
    }
}
