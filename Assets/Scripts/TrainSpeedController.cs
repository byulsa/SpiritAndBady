using UnityEngine;

public class TrainSpeedController : MonoBehaviour
{
    [Header("노트 타입별 속도 (km)")]
    public float easySpeed = 10f;
    public float normalSpeed = 20f;
    public float hardSpeed = 30f;

    [Header("판정 설정")]
    // Miss만 미완수로 처리, Perfect/Good은 완수

    private float[] selectedNoteSpeeds = new float[4]; // 선택한 노트 4개 속도
    private float maxSpeed = 0f;      // 선택한 노트 합산 최대 속도
    private float currentSpeed = 0f;  // 현재 실제 속도

    private int currentSectionIndex = 0; // 현재 진행 중인 구간 (0~3)
    private int totalNotes = 0;          // 현재 구간 전체 노트 수
    private int hitNotes = 0;            // 현재 구간 완수 노트 수 (Perfect + Good)

    private BackgroundLoop backgroundLoop;
    private Judgement judgement;

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

    // 2번 담당이 노트 선택 완료 후 호출
    // 예: SetSelectedNotes(new NoteType[] { NoteType.Normal, NoteType.Normal, NoteType.Easy, NoteType.Easy })
    public void SetSelectedNotes(float[] noteSpeeds)
    {
        selectedNoteSpeeds = noteSpeeds;
        maxSpeed = 0f;
        foreach (float speed in noteSpeeds)
            maxSpeed += speed;

        currentSpeed = maxSpeed; // 시작은 최대 속도로
        currentSectionIndex = 0;
        totalNotes = 0;
        hitNotes = 0;

        backgroundLoop.SetSpeed(currentSpeed);
        Debug.Log($"최대 속도 설정: {maxSpeed}km");
    }

    // 구간 시작 시 호출 - 1번 담당이 구간 시작할 때 전체 노트 수 넘겨줘야 함
    public void OnSectionStart(int noteCount)
    {
        totalNotes = noteCount;
        hitNotes = 0;
        Debug.Log($"구간 {currentSectionIndex + 1} 시작 / 전체 노트: {totalNotes}");
    }

    // Judgement 이벤트로 자동 수신
    void HandleJudge(JudgeType result)
    {
        if (result == JudgeType.Perfect || result == JudgeType.Good)
            hitNotes++;

        Debug.Log($"판정: {result} / 완수: {hitNotes}/{totalNotes}");
    }

    // 구간 완료 시 호출 - 1번 담당 또는 NoteGenerator.OnWaveFinished 에서 호출
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

        Debug.Log($"구간 {currentSectionIndex + 1} 완료 / 완수율: {completionRate * 100f}% / 획득: {actualSpeed}km / 현재 속도: {currentSpeed}km");

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
        Debug.Log($"노트 속도 설정 - Easy: {easy} / Normal: {normal} / Hard: {hard}");
    }
}