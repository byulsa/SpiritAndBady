using UnityEngine;

public class TrainSpeedController : MonoBehaviour
{
    [Header("��Ʈ Ÿ�Ժ� �ӵ� (km)")]
    public float easySpeed = 10f;
    public float normalSpeed = 20f;
    public float hardSpeed = 30f;

    [Header("���� ����")]
    // Miss�� �̿ϼ��� ó��, Perfect/Good�� �ϼ�

    private float[] selectedNoteSpeeds = new float[4]; // ������ ��Ʈ 4�� �ӵ�
    private float maxSpeed = 0f;      // ������ ��Ʈ �ջ� �ִ� �ӵ�
    private float currentSpeed = 0f;  // ���� ���� �ӵ�

    private int currentSectionIndex = 0; // ���� ���� ���� ���� (0~3)
    private int totalNotes = 0;          // ���� ���� ��ü ��Ʈ ��
    private int hitNotes = 0;            // ���� ���� �ϼ� ��Ʈ �� (Perfect + Good)

    [Header("Components Reference")]
    [SerializeField] private NoteGenerator NoteGenerator;
    [SerializeField] private BackgroundLoop backgroundLoop;
    [SerializeField] private Judgement judgement;

    private void Awake()
    {
        if(backgroundLoop == null)
        {
            // ndLoop = Object.FindAnyObjectByType<BackgroundLoop>();
        }
    }
    void Start()
    {
        backgroundLoop = Object.FindAnyObjectByType<BackgroundLoop>();
        judgement = Object.FindAnyObjectByType<Judgement>();
    }

    private JudgeType lastCheckedResult;

    void Update()
    {
        if (judgement == null) return;

        if (judgement.judgeType != lastCheckedResult)
        {
            lastCheckedResult = judgement.judgeType;
            HandleJudge(judgement.judgeType);
        }
    }

    void OnDestroy()
    {
        // if (judgement != null)
        //     judgement.OnJudged -= HandleJudge;
    }

    // 2�� ����� ��Ʈ ���� �Ϸ� �� ȣ��
    // ��: SetSelectedNotes(new NoteType[] { NoteType.Normal, NoteType.Normal, NoteType.Easy, NoteType.Easy })
    public void SetSelectedNotes(float[] noteSpeeds)
    {
        selectedNoteSpeeds = noteSpeeds;
        maxSpeed = 0f;
        foreach (float speed in noteSpeeds)
            maxSpeed += speed;

        currentSpeed = maxSpeed; // ������ �ִ� �ӵ���
        currentSectionIndex = 0;
        totalNotes = 0;
        hitNotes = 0;

        backgroundLoop.SetSpeed(currentSpeed);
        Debug.Log($"�ִ� �ӵ� ����: {maxSpeed}km");
    }

    // ���� ���� �� ȣ�� - 1�� ����� ���� ������ �� ��ü ��Ʈ �� �Ѱ���� ��
    public void OnSectionStart(int noteCount)
    {
        totalNotes = noteCount;
        hitNotes = 0;
        Debug.Log($"���� {currentSectionIndex + 1} ���� / ��ü ��Ʈ: {totalNotes}");
    }

    // Judgement �̺�Ʈ�� �ڵ� ����
    void HandleJudge(JudgeType result)
    {
        if (result == JudgeType.Perfect || result == JudgeType.Good)
            hitNotes++;

        Debug.Log($"����: {result} / �ϼ�: {hitNotes}/{totalNotes}");
    }

    // ���� �Ϸ� �� ȣ�� - 1�� ��� �Ǵ� NoteGenerator.OnWaveFinished ���� ȣ��
    public void OnSectionComplete()
    {
        if (totalNotes == 0) return;

        float completionRate = (float)hitNotes / totalNotes;
        float sectionSpeed = selectedNoteSpeeds[currentSectionIndex];
        float actualSpeed = sectionSpeed * completionRate;
        float penalty = sectionSpeed - actualSpeed;

        currentSpeed -= penalty;
        currentSpeed = Mathf.Max(currentSpeed, 0f);

        backgroundLoop.SetSpeed(currentSpeed);

        Debug.Log($"���� {currentSectionIndex + 1} �Ϸ� / �ϼ���: {completionRate * 100f}% / ȹ��: {actualSpeed}km / ���� �ӵ�: {currentSpeed}km");

        currentSectionIndex++;
    }

    public float GetCurrentSpeed() => currentSpeed;
    public float GetMaxSpeed() => maxSpeed;

    public void ResetSpeed()
    {
        currentSpeed = 0f;
        maxSpeed = 0f;
        currentSectionIndex = 0;
        backgroundLoop.SetSpeed(currentSpeed);
    }

    public void SetNoteTypeSpeeds(float easy, float normal, float hard)
    {
        easySpeed = easy;
        normalSpeed = normal;
        hardSpeed = hard;
        Debug.Log($"��Ʈ �ӵ� ���� - Easy: {easy} / Normal: {normal} / Hard: {hard}");
    }
}