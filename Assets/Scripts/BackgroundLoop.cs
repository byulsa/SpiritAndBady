using UnityEngine;

public class BackgroundLoop : MonoBehaviour
{
    [Header("최대 속도")]
    public float maxSpeed = 100f;

    [Header("테스트용 이동")]
    public bool isTestMode = true;
    public KeyCode speedUpKey = KeyCode.RightArrow;
    public KeyCode speedDownKey = KeyCode.LeftArrow;

    public float currentSpeed = 0f;
    private float testSpeed = 0f;
    private Transform[] backgrounds;
    private float bgWidth;

    void Start()
    {
        backgrounds = new Transform[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
            backgrounds[i] = transform.GetChild(i);

        bgWidth = backgrounds[0].GetComponent<Renderer>().bounds.size.x;
    }

    private float targetSpeed = 0f;

    public void SetSpeed(float speed)
    {
        targetSpeed = speed;
    }

    void Update()
    {
        if (isTestMode) HandleTestInput();

        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 3f);

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
            testSpeed = Mathf.Clamp(testSpeed + Time.deltaTime * 10f, 0f, maxSpeed);
        if (Input.GetKey(speedDownKey))
            testSpeed = Mathf.Clamp(testSpeed - Time.deltaTime * 10f, 0f, maxSpeed);

        SetSpeed(testSpeed);
    }


    float GetRightmostX()
    {
        float max = float.MinValue;
        foreach (Transform bg in backgrounds)
            if (bg.position.x > max) max = bg.position.x;
        return max;
    }
}