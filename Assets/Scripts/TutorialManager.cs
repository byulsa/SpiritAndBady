using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    [SerializeField] private GameObject TutorialFirstTimePanel;

    [SerializeField] private RythmManager RythmManager;

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
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            
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

    public void ShowTutorial(bool bShow)
    {
        if (!bShow)
        {
            OnTutorialEnd?.Invoke();
            Destroy(gameObject);
            return;
        }
        TutorialFirstTimePanel.SetActive(false);

    }
}
