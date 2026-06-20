using System.Collections;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    [SerializeField] private GameObject TutorialFirstTimePanel;

    [SerializeField] private RythmManager RythmManager;

    [SerializeField] private GameObject[] TutorialPanels;
    private int Index = 0;
    [SerializeField] private AudioSource Audio;
    [SerializeField] private AudioClip Clip;
    public void OnButtonCliked()
    {
        if (Audio == null || Clip == null)
        {
            return;
        }
        Audio.PlayOneShot(Clip);
    }
    public event System.Action OnTutorialEnd;

    void Awake()
    {
        if (RythmManager == null)
        {
            RythmManager = FindAnyObjectByType<RythmManager>();
        }
    }
    void Start()
    {
        SetCursorInput(true);
    }
    public void SetCursorInput(bool isEnabled)
    {
        Cursor.visible = isEnabled;
        Cursor.lockState = isEnabled ? CursorLockMode.None : CursorLockMode.Locked;
    }
    private void TutorialEnd()
    {
        OnTutorialEnd?.Invoke();
        if (RythmManager)
        {
            RythmManager.TurnOffMetronome();
        }
        SetCursorInput(false);
        Destroy(gameObject);
        return;
    }
    public void Show(bool bShow)
    {
        OnButtonCliked();
        SetCursorInput(false);
        if (!bShow)
        {
            TutorialEnd();
            return;
        }
        TutorialFirstTimePanel.SetActive(false);
        ShowTutorial();
    }

    public void ShowTutorial()
    {
        if (Index != 0)
        {
            Destroy(TutorialPanels[Index - 1]);
        }
        if (Index >= TutorialPanels.Length)
        {
            TutorialEnd();
            return;
        }
        TutorialPanels[Index++].SetActive(true);
        RythmManager.RunOnNextMeasure(ShowTutorial);
    }
}
