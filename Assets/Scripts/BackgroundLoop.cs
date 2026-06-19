using UnityEngine;

public class BackgroundLoop : MonoBehaviour
{
    [Header("배경 설정")]
    public float minSpeed = 1f;
    public float maxSpeed = 10f;

    [Header("테스트 설정")]
    public bool isTestMode = true;
    public KeyCode speedUpKey = KeyCode.RightArrow;
    public KeyCode speedDownKey = KeyCode.LeftArrow;

    public float currentSpeed = 0f;
    private float testNormalizedScore = 0f;
    private Transform[] backgrounds;
    private float bgWidth;

    void Start()
    {
        backgrounds = new Transform[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
            backgrounds[i] = transform.GetChild(i);

        bgWidth = backgrounds[0].GetComponent<Renderer>().bounds.size.x;
    }

    void Update()
    {
        if (isTestMode) HandleTestInput();

        transform.Translate(Vector3.left * currentSpeed * Time.deltaTime);

        foreach (Transform bg in backgrounds)
        {
            if (bg.position.x < -bgWidth)
            {
                float rightmostX = GetRightmostX();
                bg.position = new Vector3(rightmostX + bgWidth, bg.position.y, bg.position.z);
            }
        }
    }

    void HandleTestInput()
    {
        if (Input.GetKey(speedUpKey))
            testNormalizedScore = Mathf.Clamp01(testNormalizedScore + Time.deltaTime * 0.5f);
        if (Input.GetKey(speedDownKey))
            testNormalizedScore = Mathf.Clamp01(testNormalizedScore - Time.deltaTime * 0.5f);

        SetSpeed(testNormalizedScore);
    }

    // 판정 담당이 호출
    public void SetSpeed(float normalizedScore)
    {
        float target = Mathf.Lerp(minSpeed, maxSpeed, normalizedScore);
        currentSpeed = Mathf.Lerp(currentSpeed, target, Time.deltaTime * 3f);
    }

    float GetRightmostX()
    {
        float max = float.MinValue;
        foreach (Transform bg in backgrounds)
            if (bg.position.x > max) max = bg.position.x;
        return max;
    }
}